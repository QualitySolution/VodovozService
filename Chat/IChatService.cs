using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Collections.Generic;

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

		[OperationContract]
		[WebInvoke (UriTemplate = "/AndroidGetChatMessages", BodyStyle = WebMessageBodyStyle.WrappedRequest)] 
		List<MessageDTO> AndroidGetChatMessages (string authKey, int days);
	}
}

