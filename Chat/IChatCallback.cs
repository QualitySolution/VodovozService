using System;
using System.ServiceModel;

namespace Chat
{
	public interface IChatCallback
	{
		[OperationContract(IsOneWay = true)]
		void NotifyNewMessage(int chatId);
	}
}

