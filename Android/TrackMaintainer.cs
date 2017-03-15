using System;
using QSOrmProject;
using Vodovoz.Domain.Logistic;
using System.Globalization;
using System.Linq;

namespace Android
{
	public  class TrackMaintainer
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger ();

		IUnitOfWorkGeneric<Track> uow;

		public bool IsBusy { get; private set;}

		public DateTime LastActive { get; private set;}

		public Track Track {get{ return uow.Root;}}

		public TrackMaintainer(int trackId)
		{
			uow = UnitOfWorkFactory.CreateForRoot<Track>(trackId);
			LastActive = DateTime.Now;
		}

		public bool SaveNewCoordinates(TrackPointList trackPointList)
		{
			IsBusy = true;
			LastActive = DateTime.Now;
			DateTime startOp = DateTime.Now;
			var DecimalSeparatorFormat = new NumberFormatInfo { NumberDecimalSeparator = ".", NumberGroupSeparator = "," };
			var CommaSeparatorFormat = new NumberFormatInfo { NumberDecimalSeparator = ",", NumberGroupSeparator = "." };

			Vodovoz.Domain.Logistic.TrackPoint lastTp = null;
			foreach (TrackPoint tp in trackPointList) {
				var trackPoint = new Vodovoz.Domain.Logistic.TrackPoint();
				Double Latitude, Longitude;
				if (!Double.TryParse(tp.Latitude, NumberStyles.Float, DecimalSeparatorFormat, out Latitude) 
					&& !Double.TryParse(tp.Latitude, NumberStyles.Float, CommaSeparatorFormat, out Latitude))
				{
					logger.Warn("Не получилось разобрать координату широты: {0}", tp.Latitude);
					IsBusy = false;
					return false;
				}
				if (!Double.TryParse(tp.Longitude, NumberStyles.Float, DecimalSeparatorFormat, out Longitude) 
					&& !Double.TryParse(tp.Longitude, NumberStyles.Float, CommaSeparatorFormat, out Longitude))
				{
					logger.Warn("Не получилось разобрать координату долготы: {0}", tp.Longitude);
					IsBusy = false;
					return false;
				}
				trackPoint.Latitude = Latitude;
				trackPoint.Longitude = Longitude;
				trackPoint.TimeStamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(long.Parse(tp.TimeStamp)).ToLocalTime();
				//Округляем время до секунд.
				trackPoint.TimeStamp = new DateTime(trackPoint.TimeStamp.Year, trackPoint.TimeStamp.Month, trackPoint.TimeStamp.Day, trackPoint.TimeStamp.Hour, trackPoint.TimeStamp.Minute, trackPoint.TimeStamp.Second);
				if(lastTp != null && lastTp.TimeStamp == trackPoint.TimeStamp)
				{
					logger.Warn("Для секунды {0} трека {1}, присутствует вторая пара координат, пропускаем что бы округлить трек с точностью не чаще раза в секунду.", trackPoint.TimeStamp, Track.Id);
					continue;
				}
				lastTp = trackPoint;
				var existPoint = Track.TrackPoints.FirstOrDefault(x => x.TimeStamp == trackPoint.TimeStamp);
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
					trackPoint.Track = Track;
					Track.TrackPoints.Add(trackPoint);
				}
			}
			uow.Save();
			logger.Info("Обработаны координаты для трека {0} за {1} сек.", Track.Id, (DateTime.Now - startOp).TotalSeconds);
			IsBusy = false;
			return true;
		}
	}
}

