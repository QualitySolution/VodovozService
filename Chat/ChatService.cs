using System;
using QSOrmProject;
using Vodovoz.Repository;
using Vodovoz.Domain.Chat;
using ChatClass = Vodovoz.Domain.Chat.Chat;
using Vodovoz.Repository.Chat;
using Vodovoz.Domain.Employees;

namespace Chat
{
	public class ChatService : IChatService
	{
		#region IChatService implementation

		public bool SendMessageToLogistician (string authKey, string message)
		{
			try {
				var uow = UnitOfWorkFactory.CreateWithoutRoot ();
				var driver = EmployeeRepository.GetDriverByAuthKey (uow, authKey);
				if (driver == null)
					return false;

				var chat = ChatRepository.GetChatForDriver (uow, driver);
				if (chat == null) {
					chat = new ChatClass ();
					chat.ChatType = ChatType.DriverAndLogists;
					chat.Driver = driver;
				}

				ChatMessage chatMessage = new ChatMessage ();
				chatMessage.Chat = chat;
				chatMessage.DateTime = DateTime.Now;
				chatMessage.Message = message;
				chatMessage.Sender = driver;

				chat.Messages.Add (chatMessage);
				uow.Save (chat);
				uow.Commit ();
				return true;
			} catch (Exception e) {
				return false;
			}
		}

		public bool SendMessageToDriver (int senderId, int recipientId, string message)
		{
			try {
				var senderUoW = UnitOfWorkFactory.CreateForRoot<Employee> (senderId);
				var recipientUoW = UnitOfWorkFactory.CreateForRoot<Employee> (recipientId);

				var chat = ChatRepository.GetChatForDriver (senderUoW, recipientUoW.Root);
				if (chat == null) {
					chat = new ChatClass ();
					chat.ChatType = ChatType.DriverAndLogists;
					chat.Driver = recipientUoW.Root;
				}

				ChatMessage chatMessage = new ChatMessage ();
				chatMessage.Chat = chat;
				chatMessage.DateTime = DateTime.Now;
				chatMessage.Message = message;
				chatMessage.Sender = senderUoW.Root;

				chat.Messages.Add (chatMessage);
				senderUoW.Save (chat);
				senderUoW.Commit ();

				FCMHelper.SendMessage (recipientUoW.Root.AndroidToken, senderUoW.Root.FullName, message);
				return true;
			} catch (Exception e) {
				return false;
			}
		}

		#endregion
	}
}

