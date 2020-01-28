using System.ServiceModel;
using System.ServiceModel.Web;

namespace InstantSmsService
{
	[ServiceContract]
	public interface IInstantSmsService
	{
		[OperationContract]
		SmsMessageResult SendSms(InstantSmsMessage smsNotification);

		[OperationContract]
		[WebGet(ResponseFormat = WebMessageFormat.Json)]
		bool ServiceStatus();
	}
}
