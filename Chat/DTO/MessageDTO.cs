using System;
using System.Runtime.Serialization;
using Vodovoz.Domain.Chat;
using Vodovoz.Domain.Employees;

namespace Chat
{
	[DataContract]
	public class MessageDTO
	{
		//Сообщение
		[DataMember]
		public string Message;

		//Отправитель
		[DataMember]
		public string Sender;

		//Дата и время
		[DataMember]
		public DateTime DateTime;

		public MessageDTO (ChatMessage item)
		{
			Message = item.Message;
			Sender = item.Sender.ShortName;
			DateTime = item.DateTime;
		}

		public MessageDTO (ChatMessage item, Employee driver) : this (item)
		{
			if (item.Sender.Id == driver.Id)
				Sender = String.Empty;
		}
	}
}

