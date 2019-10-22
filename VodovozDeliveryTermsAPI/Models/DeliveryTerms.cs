using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NLog;
using QS.DomainModel.UoW;
using Vodovoz.Domain.Client;

namespace VodovozDeliveryTermsAPI.Models
{
    public class DeliveryTerms
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        public virtual DeliveryPoint DeliveryPointObj { get; set; } = new DeliveryPoint();

        public string GetRulesByDistrict(decimal latitude, decimal longitude)
        {
            var rules = new DeliveryRulesDTO();
            logger.Debug($"получены координаты {latitude} - {longitude}");

            try
            {
                var templat = (decimal)latitude;
                var templong = (decimal)longitude;
            }
            catch (Exception e)
            {
                logger.Debug($"получены неправильные координаты");
                return "not found";
            }

            using (var uow = UnitOfWorkFactory.CreateWithoutRoot($"Получение правил доставки "))
            {
                logger.Debug($"получаем район");
                try
                {
                    DeliveryPointObj.SetСoordinates(latitude, longitude, uow);
                    logger.Debug($"получен DeliveryPoint");
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
                        //return JsonConvert.SerializeObject(rules);
                        return $"{rules.minBottles}+{rules.deliveryRule}+{rules.deliveryPrice}";
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

        //public string GetRulesByDistrict(decimal latitude, decimal longitude)
        //{
        //    var minbottles = "district not found";
        //    using (var uow = UnitOfWorkFactory.CreateWithoutRoot($"[MB]Получение  "))
        //    {

        //        DeliveryPoint.SetСoordinates(latitude, longitude, uow);
        //        var t = DeliveryPoint.District;

        //        if (t != null)
        //        {
        //            var deliveryPice = t.ScheduleRestrictedDistrictRuleItems.Count > 0
        //                ? t.ScheduleRestrictedDistrictRuleItems[0]?.DeliveryPrice
        //                : -1;
        //            var DeliveryPriceRule = t.ScheduleRestrictedDistrictRuleItems.Count > 0
        //                ? t.ScheduleRestrictedDistrictRuleItems[0]?.DeliveryPriceRule.ToString()
        //                : "-1";
        //            minbottles = "minbottles-" + t?.MinBottles + ";" + "deliveryPice-" + deliveryPice + ";" +
        //                         "DeliveryPriceRule-" + DeliveryPriceRule + ";";
        //        }
        //    }

        //    return minbottles;
        //}
    }

    class DeliveryRulesDTO
    { 
        public int? minBottles;
         
        public string deliveryRule;
         
        public decimal? deliveryPrice;
    }
}