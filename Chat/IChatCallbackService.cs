using System;
using System.ServiceModel;

namespace Chat
{
	[ServiceContract(
		CallbackContract = typeof(IChatCallback),
		SessionMode = SessionMode.Required)]
	public interface IChatCallbackService
	{
		/// <summary>
		/// Subscribes for updates in chat.
		/// </summary>
		/// <returns><c>true</c>, if instance was subscribed for updates, <c>false</c> otherwise.</returns>
		/// <param name="employeeId">Employee identifier.</param>
		[OperationContract]
		bool SubscribeForUpdates (int employeeId);

		/// <summary>
		/// Unsubscribes from updates.
		/// </summary>
		/// <returns><c>true</c>, if instance was unsubscribed from updates, <c>false</c> otherwise.</returns>
		/// <param name="employeeId">Employee identifier.</param>
		[OperationContract]
		bool UnsubscribeFromUpdates (int employeeId);

		/// <summary>
		/// Keeps the connection alive.
		/// </summary>
		/// <returns><c>true</c>, on success, <c>false</c> otherwise.</returns>
		[OperationContract]
		void KeepAlive ();
	}
}

