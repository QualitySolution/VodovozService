using System;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace VodovozOSMService
{
	[ServiceContract]
	public interface IOsmServiceMonitor
	{
		[OperationContract()]
		[WebGet(ResponseFormat = WebMessageFormat.Json)]
		bool ServiceStatus();
	}
}
