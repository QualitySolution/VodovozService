using System;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace Chat
{
	[ServiceContract]
	public interface IChatService
	{
		[OperationContract]
		[WebInvoke (UriTemplate = "/SendMessageToLogistician", BodyStyle = WebMessageBodyStyle.WrappedRequest)] 
		bool SendMessageToLogistician (string authKey, string message);

		[OperationContract]
		[WebInvoke (UriTemplate = "/SendMessageToDriver", BodyStyle = WebMessageBodyStyle.WrappedRequest)] 
		bool SendMessageToDriver (int senderId, int recipientId, string message);
	}
}

