using System.ServiceModel;

namespace EmailService
{
	public static class EmailServiceSetting
	{
		public static string EmailServiceURL { get; set; }

		public static IEmailService GetEmailService()
		{
			return new ChannelFactory<IEmailService>(new BasicHttpBinding(), string.Format("http://{0}/EmailService", EmailServiceURL))
				.CreateChannel();
		}
	}
}
