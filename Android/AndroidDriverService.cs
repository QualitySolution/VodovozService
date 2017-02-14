﻿using System;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using QSOrmProject;
using Vodovoz.Domain.Employees;
using Vodovoz.Repository;
using Vodovoz.Repository.Logistics;
using Vodovoz.Domain.Logistic;
using Vodovoz.Domain.Orders;
using System.Globalization;
using System.ServiceModel.Channels;
using System.ServiceModel;

namespace Android
{
	public class AndroidDriverService : IAndroidDriverService
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger ();

		/// <summary>
		/// Const value, equals to android code version on AndroidManifest.xml
		/// Needed for version checking. Increment this value on each API change.
		/// </summary>
		private const int VERSION_CODE = 10;

		#region IAndroidDriverService implementation

		public bool CheckAppCodeVersion (int versionCode)
		{
			return versionCode == VERSION_CODE;
		}

		/// <summary>
		/// Authenticating driver by login and password.
		/// </summary>
		/// <returns>authentication string or <c>null</c></returns>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		public string Auth (string login, string password)
		{
			#if DEBUG
			logger.Debug("Auth called with args:\nlogin: {0}\npassword: {1}", login, password);
			#endif
			try
			{
				IUnitOfWork uow = UnitOfWorkFactory.CreateWithoutRoot();
				var employee = EmployeeRepository.GetDriverByAndroidLogin(uow, login);
			
				if (employee == null)
					return null;

				//Generating hash from driver password
				var hash = (new SHA1Managed()).ComputeHash(Encoding.UTF8.GetBytes(employee.AndroidPassword));
				var hashString = string.Join("", hash.Select(b => b.ToString("x2")).ToArray());
				if (password == hashString) {

					//Creating session auth key if needed
					if (String.IsNullOrEmpty (employee.AndroidSessionKey)) {
						employee.AndroidSessionKey = Guid.NewGuid ().ToString ();
						uow.Save (employee);
						uow.Commit();
					}
					return employee.AndroidSessionKey;
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			return null;
		}

		/// <summary>
		/// Checking authentication key
		/// </summary>
		/// <returns><c>true</c>, if auth was checked, <c>false</c> otherwise.</returns>
		/// <param name="authKey">Auth key.</param>
		public bool CheckAuth (string authKey)
		{
			#if DEBUG
			logger.Debug("CheckAuth called with args; authKey: {0}", authKey);
			#endif
			try {
				IUnitOfWork uow = UnitOfWorkFactory.CreateWithoutRoot();
				var driver = EmployeeRepository.GetDriverByAuthKey(uow, authKey);
				return driver != null;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			return false;
		}

		/// <summary>
		/// Gets the route lists for driver authenticated with the specified key.
		/// </summary>
		/// <returns>The route lists or <c>null</c>.</returns>
		/// <param name="authKey">Authentication key.</param>
		public List<RouteListDTO> GetRouteLists (string authKey)
		{
			#if DEBUG
			logger.Debug("GetRouteLists called with args:\nauthKey: {0}", authKey);
			#endif
			try
			{
				var result = new List<RouteListDTO>();
				IUnitOfWork uow = UnitOfWorkFactory.CreateWithoutRoot();
				if (!CheckAuth(authKey))
					return null;
				var driver = EmployeeRepository.GetDriverByAuthKey(uow, authKey);
				var routeLists = RouteListRepository.GetDriverRouteLists(uow, driver);

				foreach (RouteList rl in routeLists)
				{
					result.Add(new RouteListDTO(rl));
				}
				return result;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			return null;
		}

		public List<ShortOrderDTO> GetRouteListOrders (string authKey, int routeListId)
		{
			#if DEBUG
			logger.Debug("GetRouteListOrders called with args:\nauthKey: {0}\nrouteListId: {1}", authKey, routeListId);
			#endif

			try
			{
				if (!CheckAuth (authKey))
					return null;
			
				var routeListUoW = UnitOfWorkFactory.CreateForRoot<RouteList> (routeListId);

				if (routeListUoW == null || routeListUoW.Root == null)
					return null;

				var orders = new List<ShortOrderDTO> ();
				foreach (RouteListItem item in routeListUoW.Root.Addresses)
				{
					orders.Add(new ShortOrderDTO(item));
				}
				return orders;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			return null;
		}

		public OrderDTO GetOrderDetailed (string authKey, int orderId)
		{
			#if DEBUG
			logger.Debug("GetOrderDetailed called with args:\nauthKey: {0}\norderId: {1}", authKey, orderId);
			#endif

			try
			{
				if (!CheckAuth (authKey))
					return null;

				var orderUoW = UnitOfWorkFactory.CreateForRoot<Order> (orderId);
				if (orderUoW == null || orderUoW.Root == null)
					return null;
				var routeListItem = RouteListItemRepository.GetRouteListItemForOrder(orderUoW, orderUoW.Root);
				return new OrderDTO(routeListItem);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			return null;
		}
			
		public int? StartOrResumeTrack (string authKey, int routeListId)
		{
			try
			{
				if (!CheckAuth (authKey))
					return null;

				var routeListUoW = UnitOfWorkFactory.CreateForRoot<RouteList>(routeListId);
				var track = TrackRepository.GetTrackForRouteList(routeListUoW, routeListId);

				if (track != null)
					return track.Id;
				var trackUoW = UnitOfWorkFactory.CreateWithNewRoot<Track>();
				trackUoW.Root.RouteList = routeListUoW.Root;
				trackUoW.Root.Driver = EmployeeRepository.GetDriverByAuthKey(routeListUoW, authKey);
				trackUoW.Root.StartDate = DateTime.Now;
				trackUoW.Save();

				return trackUoW.Root.Id;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			return null;
		}

		public bool SendCoordinates (string authKey, int trackId, TrackPointList TrackPointList)
		{
			RemoteEndpointMessageProperty prop = (RemoteEndpointMessageProperty)OperationContext.Current.IncomingMessageProperties [RemoteEndpointMessageProperty.Name];
			if (prop != null)
				logger.Info("Получены координаты по треку {0} c ip{1}:{2}", trackId, prop.Address, prop.Port);

			DateTime startOp = DateTime.Now;

			var DecimalSeparatorFormat = new NumberFormatInfo { NumberDecimalSeparator = ".", NumberGroupSeparator = "," };
			var CommaSeparatorFormat = new NumberFormatInfo { NumberDecimalSeparator = ",", NumberGroupSeparator = "." };

			if (!CheckAuth (authKey))
				return false;

			var trackUoW = UnitOfWorkFactory.CreateForRoot<Track>(trackId);
			if (trackUoW == null || trackUoW.Root == null)
				return false;

			try
			{
				Vodovoz.Domain.Logistic.TrackPoint lastTp = null;
				foreach (TrackPoint tp in TrackPointList) {
					var trackPoint = new Vodovoz.Domain.Logistic.TrackPoint();
					Double Latitude, Longitude;
					if (!Double.TryParse(tp.Latitude, NumberStyles.Float, DecimalSeparatorFormat, out Latitude) 
						&& !Double.TryParse(tp.Latitude, NumberStyles.Float, CommaSeparatorFormat, out Latitude))
					{
						logger.Warn("Не получилось разобрать координату широты: {0}", tp.Latitude);
						return false;
					}
					if (!Double.TryParse(tp.Longitude, NumberStyles.Float, DecimalSeparatorFormat, out Longitude) 
						&& !Double.TryParse(tp.Longitude, NumberStyles.Float, CommaSeparatorFormat, out Longitude))
					{
						logger.Warn("Не получилось разобрать координату долготы: {0}", tp.Longitude);
						return false;
					}
					trackPoint.Latitude = Latitude;
					trackPoint.Longitude = Longitude;
					trackPoint.TimeStamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(long.Parse(tp.TimeStamp)).ToLocalTime();
					//Округляем время до секунд.
					trackPoint.TimeStamp = new DateTime(trackPoint.TimeStamp.Year, trackPoint.TimeStamp.Month, trackPoint.TimeStamp.Day, trackPoint.TimeStamp.Hour, trackPoint.TimeStamp.Minute, trackPoint.TimeStamp.Second);
					if(lastTp != null && lastTp.TimeStamp == trackPoint.TimeStamp)
					{
						logger.Warn("Для секунды {0} трека {1}, присутствует вторая пара координат, пропускаем что бы округлить трек с точностью не чаще раза в секунду.", trackPoint.TimeStamp, trackId);
						continue;
					}
					lastTp = trackPoint;
					var existPoint = trackUoW.Root.TrackPoints.FirstOrDefault(x => x.TimeStamp == trackPoint.TimeStamp);
					if(existPoint != null)
					{
						if(Math.Abs(existPoint.Latitude - trackPoint.Latitude) < 0.00000001 && Math.Abs(existPoint.Longitude - trackPoint.Longitude) < 0.00000001)
						{
							logger.Warn("Координаты на время {0} для трека {1}, были получены повторно поэтому пропущены.", trackPoint.TimeStamp, existPoint.Track.Id);
						}
						else
						{
							logger.Warn($"Координаты на время {trackPoint.TimeStamp} для трека {existPoint.Track.Id}, были получены повторно и изменены " +
								$"lat: {existPoint.Latitude} -> {trackPoint.Latitude} log: {existPoint.Longitude} -> {trackPoint.Longitude}");
							existPoint.Latitude = trackPoint.Latitude ;
							existPoint.Longitude = trackPoint.Longitude ;
						}
					}
					else
					{
						trackPoint.Track = trackUoW.Root;
						trackUoW.Root.TrackPoints.Add(trackPoint);
					}
				}
				trackUoW.Save();
				logger.Info("Закончена обработка координат для трека {0} за {1} сек.", trackId, (DateTime.Now - startOp).TotalSeconds);
				return true;
			}
			catch (NHibernate.NonUniqueObjectException ex)
			{
				logger.Error(ex, "На обоработке трека {0}", trackId);
				logger.Info("Содержание списка координат:\n" 
					+ String.Join("\n", trackUoW.Root.TrackPoints.Select(x => x.TimeStamp.ToLongTimeString())));
			}
			catch (Exception e)
			{
				logger.Error(e, "На обоработке трека {0}", trackId);
			}
			return false;
		}

		public bool ChangeOrderStatus (string authKey, int orderId, string status, int? bottlesReturned) {
			try
			{
				if (!CheckAuth (authKey))
					return false;

				var orderUoW = UnitOfWorkFactory.CreateForRoot<Order> (orderId);
				if (orderUoW == null || orderUoW.Root == null)
					return false;

				var routeListItem = RouteListItemRepository.GetRouteListItemForOrder(orderUoW, orderUoW.Root);
				if (routeListItem == null)
					return false;

				switch (status) {
				case "EnRoute": routeListItem.UpdateStatus(RouteListItemStatus.EnRoute); break;
				case "Completed": routeListItem.UpdateStatus(RouteListItemStatus.Completed); break;
				case "Canceled": routeListItem.UpdateStatus(RouteListItemStatus.Canceled); break;
				case "Overdue": routeListItem.UpdateStatus(RouteListItemStatus.Overdue); break;
				default: return false;
				}
				routeListItem.DriverBottlesReturned = bottlesReturned;

				orderUoW.Save(routeListItem);
				orderUoW.Commit();
				return true;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			return false;
		}

		public bool EnablePushNotifications (string authKey, string token)
		{
			try
			{
				var uow = UnitOfWorkFactory.CreateWithoutRoot();
				if (!CheckAuth(authKey))
					return false;
				var driver = EmployeeRepository.GetDriverByAuthKey(uow, authKey);
				if (driver == null)
					return false;
				driver.AndroidToken = token;
				uow.Save(driver);
				uow.Commit();
				return true;
			} 
			catch (Exception e) 
			{
				logger.Error (e);
				return false;
			}
		}

		public bool DisablePushNotifications (string authKey)
		{
			try
			{
				var uow = UnitOfWorkFactory.CreateWithoutRoot();
				if (!CheckAuth(authKey))
					return false;
				var driver = EmployeeRepository.GetDriverByAuthKey(uow, authKey);
				if (driver == null)
					return false;
				driver.AndroidToken = null;
				uow.Save(driver);
				uow.Commit();
				return true;
			} 
			catch (Exception e) 
			{
				logger.Error (e);
				return false;
			}
		}

		public bool FinishRouteList (string authKey, int routeListId) {
			try
			{
				if (!CheckAuth (authKey))
					return false;

				var routeListUoW = UnitOfWorkFactory.CreateForRoot<RouteList> (routeListId);

				if (routeListUoW == null || routeListUoW.Root == null)
					return false;

				routeListUoW.Root.CompleteRoute();
				routeListUoW.Save();
				return true;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			return false;
		}
		#endregion
	}
}

