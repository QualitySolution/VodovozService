using System;
using System.ServiceModel.Web;
using EmailService.Mailjet;

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
			EmailManager.AddEvent(content);

			//Необходимо обязательно отправлять в ответ http code 200 - OK
			WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.OK;
		}

		public Tuple<bool, string> SendEmail(Email mail)
		{
			return EmailManager.AddEmail(mail);
		}
	}
}
