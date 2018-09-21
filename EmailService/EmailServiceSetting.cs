using System.ServiceModel;

namespace EmailService
{
	public static class EmailServiceSetting
	{
		public static bool CanSendEmail => !string.IsNullOrWhiteSpace(EmailServiceURL);

		public static string EmailServiceURL { get; set; }

		public static IEmailService GetEmailService()
		{
			if(!CanSendEmail) {
				return null;
			}
			return new ChannelFactory<IEmailService>(new BasicHttpBinding(), string.Format("http://{0}/EmailService", EmailServiceURL))
				.CreateChannel();
		}
	}
}
