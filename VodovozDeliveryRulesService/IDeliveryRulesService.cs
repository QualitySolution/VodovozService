using System;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace VodovozDeliveryRulesService
{
	[ServiceContract]
	public interface IDeliveryRulesService
	{
		[WebGet(ResponseFormat = WebMessageFormat.Json)]
		[OperationContract]
		DeliveryRuleDTO GetRulesByDistrict(decimal latitude, decimal longitude);
	}
}
