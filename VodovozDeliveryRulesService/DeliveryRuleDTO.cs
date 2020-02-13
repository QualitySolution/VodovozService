using System;
using System.Runtime.Serialization;
namespace VodovozDeliveryRulesService
{
	[DataContract]
	public class DeliveryRuleDTO
	{
		[DataMember]
		public int? MinBottles { get; set; }

		[DataMember]
		public string DeliveryRule { get; set; }

		[DataMember]
		public decimal? DeliveryPrice { get; set; }
	}
}
