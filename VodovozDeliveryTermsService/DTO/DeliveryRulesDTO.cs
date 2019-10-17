using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace VodovozDeliveryTermsService.DTO
{
    [DataContract]
    class DeliveryRulesDTO
    {
        [DataMember]
        public int? minBottles;

        [DataMember]
        public string deliveryRule;

        [DataMember]
        public decimal? deliveryPrice;
    }
}
