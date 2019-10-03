using System;
using SmsSendInterface;
using QS.DomainModel.UoW;
using Vodovoz.Domain.Sms;
using System.Collections.Generic;
using System.Linq;
using SmsBlissSendService;
using System.Timers;

namespace VodovozSmsInformerService
{
	/// <summary>
	/// Отправляет смс уведомление при проведении первого заказа для нового клиента
	/// </summary>
	public class NewClientSmsInformer
	{
		private readonly ISmsSender smsSender;
		private Timer timer;
		private bool sendingInProgress = false;

		public NewClientSmsInformer(ISmsSender smsSender)
		{
			this.smsSender = smsSender ?? throw new ArgumentNullException(nameof(smsSender));
		}

		public void Start()
		{
			timer = new Timer(60000);
			timer.Elapsed += Timer_Elapsed;
			timer.Start();
		}

		public void Stop()
		{
			timer?.Stop();
			timer?.Dispose();
			timer = null;
		}

		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			SendNewNotifications();
		}

		private void CloseExpiredNotifications(IUnitOfWork uow, IEnumerable<NewClientSmsNotification> notifications)
		{
			var expiredNotifications = notifications.Where(x => x.Status == SmsNotificationStatus.New).Where(x => x.ExpiredTime <= DateTime.Now);
			foreach(var expiredNotification in expiredNotifications) {
				expiredNotification.Status = SmsNotificationStatus.SendExpired;
				uow.Save(expiredNotification);
			}
		}

		private void SendNewNotifications()
		{
			if(sendingInProgress) {
				return;
			}
			sendingInProgress = true;
			try {
				using(var uow = UnitOfWorkFactory.CreateWithoutRoot()) {
					var newNotifications = uow.Session.QueryOver<NewClientSmsNotification>()
						.Where(x => x.Status == SmsNotificationStatus.New)
						.List();

					//закрытие просроченных уведомлений
					CloseExpiredNotifications(uow, newNotifications);
					newNotifications = newNotifications.Where(x => x.Status == SmsNotificationStatus.New).ToList();

					foreach(var notification in newNotifications) {
						SendNotification(notification);
						uow.Save(notification);
					}
					uow.Commit();
				}
			}
			finally {
				sendingInProgress = false;
			}
		}

		private void SendNotification(NewClientSmsNotification notification)
		{
			SmsMessage smsMessage = new SmsMessage(notification.MobilePhone, notification.Id.ToString(), notification.MessageText);
			var result = smsSender.SendSms(smsMessage);

			notification.ServerMessageId = result.ServerId;
			notification.Status = result.Status == SmsSentStatus.Accepted ? SmsNotificationStatus.Accepted : SmsNotificationStatus.Error;
			switch(result.Status) {
			case SmsSentStatus.InvalidMobilePhone:
				notification.ErrorDescription = $"Неверно заполнен номер мобильного телефона. ({notification.MobilePhone})";
				break;
			case SmsSentStatus.TextIsEmpty:
				notification.ErrorDescription = $"Не заполнен текст сообщения";
				break;
			case SmsSentStatus.SenderAddressInvalid:
				notification.ErrorDescription = $"Неверное имя отправителя";
				break;
			case SmsSentStatus.NotEnoughBalance:
				notification.ErrorDescription = $"Недостаточно средств на счете";
				break;
			case SmsSentStatus.UnknownError:
				notification.ErrorDescription = $"{result.Description}";
				break;
			}
		}
	}
}
