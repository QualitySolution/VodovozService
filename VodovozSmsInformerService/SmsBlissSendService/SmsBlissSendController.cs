using System;
using System.Threading.Tasks;
using SmsSendInterface;
using SmsBlissAPI;
using SmsBlissAPI.Model;
using System.Linq;
using NLog;

namespace SmsBlissSendService
{
	public class SmsBlissSendController : ISmsSender, ISmsBalanceNotifier
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private readonly SmsBlissClient smsBlissClient;

		public SmsBlissSendController(string login, string password)
		{
			smsBlissClient = new SmsBlissClient(login, password);
		}

		#region ISmsSender implementation

		public ISmsSendResult SendSms(ISmsMessage smsMessage)
		{
			if(smsMessage == null) {
				throw new ArgumentNullException(nameof(smsMessage));
			}

			if(!ValidateMobilePhoneNumberLength(smsMessage.MobilePhoneNumber)) {
				return new SmsBlissSentResult(SmsSentStatus.InvalidMobilePhone) { Description = "Сообщение не было отправлено, не корректная длина номера телефона" };
			}

			string phone = smsMessage.MobilePhoneNumber.ToString();
			Message message = new Message(smsMessage.LocalId, phone, smsMessage.MessageText);

			var response = smsBlissClient.SendMessages(new[] { message }, showBillingDetails: true);

			NotifyBalanceChangeOnSendSuccessfully(response);
			return ConvertToISmsSendResult(response);
		}

		public async Task<ISmsSendResult> SendSmsAsync(ISmsMessage smsMessage)
		{
			if(smsMessage == null) {
				throw new ArgumentNullException(nameof(smsMessage));
			}

			if(!ValidateMobilePhoneNumberLength(smsMessage.MobilePhoneNumber)) {
				return new SmsBlissSentResult(SmsSentStatus.InvalidMobilePhone) { Description = "Сообщение не было отправлено, не корректная длина номера телефона" };
			}

			string phone = smsMessage.MobilePhoneNumber.ToString();
			Message message = new Message(smsMessage.LocalId, phone, smsMessage.MessageText);

			var response = await smsBlissClient.SendMessagesAsync(new[] { message }, showBillingDetails: true);

			NotifyBalanceChangeOnSendSuccessfully(response);
			return ConvertToISmsSendResult(response);
		}

		#endregion ISmsSender implementation

		#region ISmsBalanceNotifier implementation

		public event EventHandler<SmsBalanceEventArgs> OnBalanceChange;

		#endregion ISmsBalanceNotifier implementation

		protected virtual bool ValidateMobilePhoneNumberLength(int phone)
		{
			if(phone < 0) {
				return false;
			}

			int phoneLegth = phone.ToString().Length;

			if(phoneLegth < 10 || phoneLegth > 11) {
				return false;
			}

			return true;
		}

		private ISmsSendResult ConvertToISmsSendResult(MessagesResponse response)
		{
			if(response == null || !response.Messages.Any()) {
				return new SmsBlissSentResult(SmsSentStatus.UnknownError) { Description = "От сервера получен пустой ответ" };
			}
			MessageResponse messageResponse = response.Messages.First();
			return new SmsBlissSentResult(messageResponse);
		}

		private void NotifyBalanceChangeOnSendSuccessfully(MessagesResponse response)
		{
			if(response == null || !response.Messages.Any()) {
				return;
			}
			MessageResponse messageResponse = response.Messages.First();
			if(messageResponse.Status == MessageResponseStatus.Accepted) {
				foreach(var item in response.Balances) {
					SmsSendInterface.BalanceType balanceType = SmsSendInterface.BalanceType.CurrencyBalance;
					switch(item.Type) {
					case SmsBlissAPI.Model.BalanceType.RUB:
						balanceType = SmsSendInterface.BalanceType.CurrencyBalance;
						break;
					case SmsBlissAPI.Model.BalanceType.SMS:
						balanceType = SmsSendInterface.BalanceType.SmsCounts;
						break;
					}
					if(!decimal.TryParse(item.BalanceValue, out decimal balance)) {
						logger.Warn($"Невозможно преобразовать значение баланса из \"{item.BalanceValue}\" в число");
						continue;
					}
					OnBalanceChange?.Invoke(this, new SmsBalanceEventArgs(balanceType, balance));
				}
			}
		}
	}
}
