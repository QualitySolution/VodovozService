using System;
using Newtonsoft.Json;
using NLog;
using QS.DomainModel.UoW;
using Vodovoz.Domain.Client;

namespace DeliveryTermsAPI.Models
{
    public class DeliveryTerms
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        public virtual DeliveryPoint DeliveryPointObj { get; set; } = new DeliveryPoint();

        public string GetRulesByDistrict(decimal latitude, decimal longitude)
        {
            var rules = new DeliveryRulesDTO();
           // logger.Debug($"получены координаты {latitude} - {longitude}");

            try
            {
                var templat = (decimal)latitude;
                var templong = (decimal)longitude;
            }
            catch (Exception e)
            {
                logger.Fatal($"получены неправильные координаты");
                return "wrong data";
            }

            using (var uow = UnitOfWorkFactory.CreateWithoutRoot($"Получение правил доставки "))
            {
                //logger.Debug($"получаем район");
                try
                {
                    DeliveryPointObj.SetСoordinates(latitude, longitude, uow);
                    //logger.Debug($"получен DeliveryPoint");
                    var district = DeliveryPointObj.District;

                    if (district != null)
                    {
                        logger.Debug($"район получен {district.DistrictName}");
                        rules.minBottles = district?.MinBottles;
                        rules.deliveryPrice = district.ScheduleRestrictedDistrictRuleItems.Count > 0
                            ? district.ScheduleRestrictedDistrictRuleItems[0]?.DeliveryPrice
                            : -1;
                        rules.deliveryRule = district.ScheduleRestrictedDistrictRuleItems.Count > 0
                            ? district.ScheduleRestrictedDistrictRuleItems[0]?.DeliveryPriceRule.ToString()
                            : "-1";
                        return JsonConvert.SerializeObject(rules);
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            }
            logger.Debug($"район не обслуживается");
            return "the area is not serviced";
        }
    }

    class DeliveryRulesDTO
    {
        public int? minBottles;

        public string deliveryRule;

        public decimal? deliveryPrice;
    }
     
}