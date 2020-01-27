using System.ServiceModel;

namespace InstantSmsService
{
	[ServiceContract]
	public interface IInstantSmsService
	{
		[OperationContract]
		SmsMessageResult SendSms(InstantSmsMessage smsNotification);
	}
}
