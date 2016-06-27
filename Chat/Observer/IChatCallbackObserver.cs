using System;

namespace Chat
{
	public interface IChatCallbackObserver
	{
		int ChatId { get; }
		void HandleChatUpdate();
	}
}

