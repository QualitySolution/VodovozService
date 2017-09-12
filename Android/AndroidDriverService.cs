﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using QSOrmProject;
using Vodovoz.Domain;
using Vodovoz.Domain.Logistic;
using Vodovoz.Domain.Orders;
using Vodovoz.Repository;
using Vodovoz.Repository.Logistics;

namespace Android
{
	public class AndroidDriverService : IAndroidDriverService
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger ();

		/// <summary>
		/// Const value, equals to android code version on AndroidManifest.xml
		/// Needed for version checking. Increment this value on each API change.
		/// This is minimal version works with current API.
		/// </summary>
		private const int VERSION_CODE = 11;

		#region IAndroidDriverService implementation

		[Obsolete("Удалить после того как не останется клиентов на версии ниже 11.")]
		public bool CheckAppCodeVersion (int versionCode)
		{
			return false;
		}

		public CheckVersionResultDTO CheckApplicationVersion(int versionCode)
		{
			var uow = UnitOfWorkFactory.CreateWithoutRoot();
			var lastVersionParameter = uow.Session.Get<BaseParameter>("last_android_version_code");
			var lastVersionNameParameter = uow.Session.Get<BaseParameter>("last_android_version_name");

			var result = new CheckVersionResultDTO();
			result.DownloadUrl = "market://details?id=ru.qsolution.vodovoz.driver";
			result.NewVersion = lastVersionNameParameter?.StrValue;

			int lastVersionCode = 0;
			Int32.TryParse(lastVersionParameter?.StrValue, out lastVersionCode);

			if (lastVersionCode > versionCode)
				result.Result = CheckVersionResultDTO.ResultType.CanUpdate;

			if (VERSION_CODE > versionCode)
				result.Result = CheckVersionResultDTO.ResultType.NeedUpdate;

			return result;
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
				var routeLists = RouteListRepository.GetDriverRouteLists(uow, driver, RouteListStatus.EnRoute, DateTime.Today);

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

			if (!CheckAuth (authKey))
				return false;

			return TracksService.ReceivedCoordinates(trackId, TrackPointList);
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

				if(routeListItem.Status == RouteListItemStatus.Transfered)
				{
					logger.Error("Попытка переключить статус у переданного адреса. address_id = {0}", routeListItem.Id);
					return false;
				}

				switch (status) {
					case "EnRoute": routeListItem.UpdateStatus(orderUoW, RouteListItemStatus.EnRoute); break;
					case "Completed": routeListItem.UpdateStatus(orderUoW, RouteListItemStatus.Completed); break;
					case "Canceled": routeListItem.UpdateStatus(orderUoW, RouteListItemStatus.Canceled); break;
					case "Overdue": routeListItem.UpdateStatus(orderUoW, RouteListItemStatus.Overdue); break;
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

				if (routeListUoW.Root.Addresses.Any(r => r.Status == RouteListItemStatus.EnRoute)){
					logger.Error("Попытка закрыть маршрутный лист {0}", routeListUoW.Root.Id);
					return false;
				}
					

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

