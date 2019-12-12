using System;
using System.Linq;
using NLog;
using WCFServer;
namespace VodovozOSMService
{
	public class ExtendedOsmService : OsmService, IOsmServiceMonitor
	{
		Logger logger = LogManager.GetCurrentClassLogger();

		public bool ServiceStatus()
		{
			try {
				//TODO Возможно стоит добавить проверку дополнительных методов службы, по необходимости

				var cities = GetCities();
				return cities.Any();
			}
			catch(Exception ex) {
				logger.Error(ex, "Ошибка при проверке работоспособности службы OSM");
				return false;
			}
		}
	}
}
