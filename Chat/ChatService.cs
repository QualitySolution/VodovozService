﻿using System;
using QSOrmProject;
using Vodovoz.Repository;
using Vodovoz.Domain.Chat;
using ChatClass = Vodovoz.Domain.Chat.Chat;
using Vodovoz.Repository.Chat;
using Vodovoz.Domain.Employees;
using System.Collections.Generic;
using Vodovoz.Domain.Logistic;
using Gamma.Utilities;

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
				Console.WriteLine (e.StackTrace);
				return false;
			}
		}

		public bool SendMessageToDriver (int senderId, int recipientId, string message)
		{
			try {
				var senderUoW = UnitOfWorkFactory.CreateForRoot<Employee> (senderId);
				var recipient = senderUoW.GetById<Employee> (recipientId);

				var chat = ChatRepository.GetChatForDriver (senderUoW, recipient);
				if (chat == null) {
					chat = new ChatClass ();
					chat.ChatType = ChatType.DriverAndLogists;
					chat.Driver = recipient;
				}

				ChatMessage chatMessage = new ChatMessage ();
				chatMessage.Chat = chat;
				chatMessage.DateTime = DateTime.Now;
				chatMessage.Message = message;
				chatMessage.Sender = senderUoW.Root;

				chat.Messages.Add (chatMessage);
				senderUoW.Save (chat);
				senderUoW.Commit ();

				FCMHelper.SendMessage (recipient.AndroidToken, senderUoW.Root.ShortName, message);
				return true;
			} catch (Exception e) {
				Console.WriteLine (e.StackTrace);
				return false;
			}
		}

		public List<MessageDTO> AndroidGetChatMessages (string authKey, int days)
		{
			try {
				var uow = UnitOfWorkFactory.CreateWithoutRoot ();
				var driver = EmployeeRepository.GetDriverByAuthKey (uow, authKey);
				if (driver == null)
					return null;

				var chat = ChatRepository.GetChatForDriver (uow, driver);
				if (chat == null)
					return null;
				var messages = new List<MessageDTO> ();
				var chatMessages = ChatMessageRepository.GetChatMessagesForPeriod (uow, chat, days);
				foreach (var m in chatMessages) {
					messages.Add (new MessageDTO (m, driver));
				}
				return messages;
			} catch (Exception e) {
				Console.WriteLine (e.StackTrace);
				return null;
			}
		}

		public bool SendOrderStatusNotificationToDriver (int senderId, int routeListItemId) {
			try {
				var senderUoW = UnitOfWorkFactory.CreateForRoot<Employee> (senderId);
				var routeListItem = senderUoW.GetById<RouteListItem> (routeListItemId);
				var driver = routeListItem.RouteList.Driver;

				if (driver == null)
					return false;
				
				var chat = ChatRepository.GetChatForDriver (senderUoW, driver);
				if (chat == null) {
					chat = new ChatClass ();
					chat.ChatType = ChatType.DriverAndLogists;
					chat.Driver = driver;
				}

				ChatMessage chatMessage = new ChatMessage ();
				chatMessage.Chat = chat;
				chatMessage.DateTime = DateTime.Now;
				chatMessage.Message = String.Format("Заказ №{0} из маршрутного листа №{1} был переведен в статус \"{2}\".",
					routeListItem.Order.Id,
					routeListItem.RouteList.Id,
					routeListItem.Status.GetEnumTitle());
				chatMessage.Sender = senderUoW.Root;

				chat.Messages.Add (chatMessage);
				senderUoW.Save (chat);
				senderUoW.Commit ();
				var message = String.Format("Изменение статуса заказа №{0}", routeListItem.Order.Id);

				FCMHelper.SendOrderStatusChangeMessage (driver.AndroidToken, senderUoW.Root.ShortName, message);
				return true;
			} catch (Exception e) {
				Console.WriteLine (e.StackTrace);
				return false;
			}
		}

		#endregion
	}
}

