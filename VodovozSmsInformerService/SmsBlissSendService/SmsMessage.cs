using System;
using SmsSendInterface;
namespace SmsBlissSendService
{
	public class SmsMessage : ISmsMessage
	{
		public int MobilePhoneNumber { get; set; }

		public string LocalId { get; set; }

		public DateTime ScheduleTime { get; set; }

		public string MessageText { get; set; }

		public SmsMessage(int phone, string localId, string messageText)
		{
			MobilePhoneNumber = phone;
			LocalId = localId;
			MessageText = messageText;
		}

		public SmsMessage(int phone, string localId, string messageText, DateTime scheduleTime) : this(phone, localId, messageText)
		{
			ScheduleTime = scheduleTime;
		}
	}
}
