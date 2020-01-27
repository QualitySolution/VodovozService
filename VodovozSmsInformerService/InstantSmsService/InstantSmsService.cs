using System;
using NLog;
using SmsBlissSendService;
using SmsSendInterface;

namespace InstantSmsService
{
	public class InstantSmsService : IInstantSmsService
	{
		public InstantSmsService(ISmsSender smsSender)
		{
			this.smsSender = smsSender;
		}

		private static Logger logger = LogManager.GetCurrentClassLogger();
		private ISmsSender smsSender;

		public SmsMessageResult SendSms(InstantSmsMessage smsNotification)
		{
			SmsMessageResult smsResult = new SmsMessageResult { MessageStatus = SmsMessageStatus.Ok};
			try {
				SmsMessage smsMessage = new SmsMessage(smsNotification.MobilePhone, smsNotification.ServerMessageId, smsNotification.MessageText);

				if(DateTime.Now > smsNotification.ExpiredTime) {
					smsResult.ErrorDescription = "Време отправки Sms сообщения вышло";
					return smsResult;
				}
				var result = smsSender.SendSms(smsMessage);

				logger.Info($"Отправлено уведомление. Тел.: {smsMessage.MobilePhoneNumber}, результат: {result.Status}");

				switch(result.Status) {
				case SmsSentStatus.InvalidMobilePhone:
					smsResult.ErrorDescription = $"Неверно заполнен номер мобильного телефона. ({smsNotification.MobilePhone})";
					break;
				case SmsSentStatus.TextIsEmpty:
					smsResult.ErrorDescription = $"Не заполнен текст сообщения";
					break;
				case SmsSentStatus.SenderAddressInvalid:
					smsResult.ErrorDescription = $"Неверное имя отправителя";
					break;
				case SmsSentStatus.NotEnoughBalance:
					smsResult.ErrorDescription = $"Недостаточно средств на счете";
					break;
				case SmsSentStatus.UnknownError:
					smsResult.ErrorDescription = $"{result.Description}";
					break;
				}
			}
			catch(Exception ex) {
				smsResult.ErrorDescription = $"Ошибка при отправке смс сообщения. {ex.Message}";
				logger.Error(ex);
			}
			return smsResult;
		}
	}
}
