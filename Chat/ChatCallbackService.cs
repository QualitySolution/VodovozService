using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Chat
{
	public class ChatCallbackService : IChatCallbackService
	{
		private static List<CallbackSubscriber> callbackList = new List<CallbackSubscriber> ();

		#region IChatCallbackService implementation

		public bool SubscribeForUpdates (int employeeId)
		{
			try {
				var callback = OperationContext.Current.GetCallbackChannel<IChatCallback> ();
				var subscriber = new CallbackSubscriber (callback, employeeId);

				if (!callbackList.Contains (subscriber)) {
					callbackList.Add (subscriber);
				}
				return true;
			} catch (Exception e) {
				Console.WriteLine (e.StackTrace);
				return false;
			}
		}

		public bool UnsubscribeFromUpdates (int employeeId)
		{
			try {
				var callback = OperationContext.Current.GetCallbackChannel<IChatCallback> ();
				var subscriber = new CallbackSubscriber (callback, employeeId);

				if (callbackList.Contains (subscriber)) {
					callbackList.Remove (subscriber);
				}
				return true;
			} catch (Exception e) {
				Console.WriteLine (e.StackTrace);
				return false;
			}
		}

		public void KeepAlive ()
		{
			//Dummy method for keeping conection alive;
			return;
		}

		#endregion

		public static void NotifyChatUpdate (int chatId)
		{
			for (int i = 0; i < callbackList.Count; i++) {
				//TODO: Add logic for checking if subscriber is interested in callback
				try {
					callbackList [i].Callback.NotifyNewMessage (chatId);				
				} catch (Exception e) {
					callbackList.RemoveAt (i);
					i--;
					Console.WriteLine ("{0}\n{1}", e.Message, e.StackTrace);
				}
			}
		}
	}
}

