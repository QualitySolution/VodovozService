using System.ServiceModel;
using System.ServiceModel.Web;

namespace VodovozInstantSmsService
{
	[ServiceContract]
	public interface IInstantSmsInformerService
	{
		[OperationContract]
		[WebGet(ResponseFormat = WebMessageFormat.Json)]
		bool ServiceStatus();
	}
}
