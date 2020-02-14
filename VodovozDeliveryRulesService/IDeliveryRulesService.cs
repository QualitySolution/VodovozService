using System.ServiceModel;
using System.ServiceModel.Web;

namespace VodovozDeliveryRulesService
{
	[ServiceContract]
	public interface IDeliveryRulesService
	{
		[WebGet(ResponseFormat = WebMessageFormat.Json)]
		[OperationContract]
		DeliveryRulesResponse GetRulesByDistrict(decimal latitude, decimal longitude);
	}
}
