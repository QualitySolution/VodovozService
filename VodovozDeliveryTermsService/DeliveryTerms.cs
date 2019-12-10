using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using QS.DomainModel.UoW;
using Vodovoz.Domain.Client;
using VodovozDeliveryTermsService;
using VodovozDeliveryTermsService.DTO;

namespace VodovozDeliveryTermsAPI.Models
{
    public class DeliveryTerms: IDeliveryTerms
    {
        public virtual DeliveryPoint DeliveryPoint { get; set; } = new DeliveryPoint();
        public static string BaseUrl { get; set; }

        public DeliveryTerms()
        {
            
        }

        public string GetRulesByDistrict(decimal latitude, decimal longitude)
        {
            var rules = new DeliveryRulesDTO();

            using (var uow = UnitOfWorkFactory.CreateWithoutRoot($"[MB]Получение  "))
            {

                DeliveryPoint.SetСoordinates(latitude, longitude, uow);
                var district = DeliveryPoint.District;

                if (district != null)
                {
                    rules.minBottles = district?.MinBottles;
                    rules.deliveryPrice = district.ScheduleRestrictedDistrictRuleItems.Count > 0
                        ? district.ScheduleRestrictedDistrictRuleItems[0]?.DeliveryPrice
                        : -1;
                    rules.deliveryRule = district.ScheduleRestrictedDistrictRuleItems.Count > 0
                        ? district.ScheduleRestrictedDistrictRuleItems[0]?.DeliveryPriceRule.ToString()
                        : "-1";
                   
                }
            }

            return minbottles;
        }
    }
}