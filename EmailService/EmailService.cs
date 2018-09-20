using System;
using System.ServiceModel.Web;
using System.Threading.Tasks;
using QSEmailSending;

namespace EmailService
{
	public class EmailService : IEmailService, IMailjetEventService
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public EmailService()

		{
			EmailManager.Init();
		}

		public void PostEvent(MailjetEvent content)
		{
			logger.Info("Получено событие с сервера Mailjet");

			Task.Run(() => EmailManager.ProcessEvent(content));

			//Необходимо обязательно отправлять в ответ http code 200 - OK
			WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.OK;
		}

		public Tuple<bool, string> SendEmail(Email mail)
		{
			logger.Info("Получен запрос на отправку письма.");

			try {
				EmailManager.AddEmailToSend(mail);
			}
			catch(Exception ex) {
				return new Tuple<bool, string>(false, ex.Message);
			}
			return new Tuple<bool, string>(true, "Письмо добавлено в очередь на отправку");
		}
	}
}
