using System;
using NLog;
using QS.DomainModel.UoW;
using Vodovoz.Domain.Client;

namespace VodovozDeliveryRulesService
{
	public class DeliveryRulesService : IDeliveryRulesService
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		public DeliveryRuleDTO GetRulesByDistrict(decimal latitude, decimal longitude)
		{
			var rule = new DeliveryRuleDTO();

			using(var uow = UnitOfWorkFactory.CreateWithoutRoot($"Получение правил доставки ")) {
				try {
					//FIXME Сделать получение информации о районе нормально через репозиторий
					DeliveryPoint dp = new DeliveryPoint();
					dp.SetСoordinates(latitude, longitude, uow);
					var district = dp.District;

					if(district != null) {
						logger.Debug($"район получен {district.DistrictName}");
						rule.MinBottles = district?.MinBottles;
						rule.DeliveryPrice = district.ScheduleRestrictedDistrictRuleItems.Count > 0
							? district.ScheduleRestrictedDistrictRuleItems[0]?.DeliveryPrice
							: -1;
						rule.DeliveryRule = district.ScheduleRestrictedDistrictRuleItems.Count > 0
							? district.ScheduleRestrictedDistrictRuleItems[0]?.DeliveryPriceRule.ToString()
							: "-1";
						return rule;
					}
				}
				catch(Exception e) {
					logger.Error(e);
				}
			}

			logger.Debug($"район не обслуживается");
			return null;
		}
	}
}
