using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using QS.DomainModel.UoW;
using Vodovoz.Domain.Client;

namespace VodovozDeliveryTermsAPI.Models
{
    public class DeliveryTerms
    {
        public virtual DeliveryPoint DeliveryPoint { get; set; } = new DeliveryPoint();

        public string GetRulesByDistrict(decimal latitude, decimal longitude)
        {
            var minbottles = "district not found";
            using (var uow = UnitOfWorkFactory.CreateWithoutRoot($"[MB]Получение  "))
            {

                DeliveryPoint.SetСoordinates(latitude, longitude, uow);
                var t = DeliveryPoint.District;

                if (t != null)
                {
                    var deliveryPice = t.ScheduleRestrictedDistrictRuleItems.Count > 0
                        ? t.ScheduleRestrictedDistrictRuleItems[0]?.DeliveryPrice
                        : -1;
                    var DeliveryPriceRule = t.ScheduleRestrictedDistrictRuleItems.Count > 0
                        ? t.ScheduleRestrictedDistrictRuleItems[0]?.DeliveryPriceRule.ToString()
                        : "-1";
                    minbottles = "minbottles-" + t?.MinBottles + ";" + "deliveryPice-" + deliveryPice + ";" +
                                 "DeliveryPriceRule-" + DeliveryPriceRule + ";";
                }
            }

            return minbottles;
        }
    }
}