using System;
using System.Net;
using Newtonsoft.Json;
using NLog;
using QS.DomainModel.UoW;
using Vodovoz.Domain.Client;
using VodovozDeliveryTermsService.DTO;

namespace VodovozDeliveryTermsService
{
    public class DeliveryTerms: IDeliveryTerms
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        public virtual DeliveryPoint DeliveryPointObj { get; set; } = new DeliveryPoint();
        public static string BaseUrl { get; set; }

        public DeliveryTerms()
        {
            
        }

        public string GetRulesByDistrict(decimal latitude, decimal longitude)
        {
            var rules = new DeliveryRulesDTO() ;
            logger.Debug($"получены координаты {latitude} - {longitude}");

            try
            {
                var templat = (decimal) latitude;
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
            return "not found";
        }
    }
}