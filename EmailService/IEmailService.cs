using System;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace EmailService
{
	[ServiceContract]
	public interface IEmailService
	{
		[WebInvoke(UriTemplate = "/SendEmail", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
		[OperationContract]
		Tuple<bool, string> SendEmail(Email mail);
	}
}
