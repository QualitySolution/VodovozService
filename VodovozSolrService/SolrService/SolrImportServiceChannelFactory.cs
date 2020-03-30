using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using NLog;
namespace SolrImportService
{
	public class SolrImportServiceChannelFactory
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private readonly string serverAddress;
		private readonly string serverPort;

		public SolrImportServiceChannelFactory(string serverAddress, string serverPort)
		{
			if(string.IsNullOrWhiteSpace(serverAddress)) {
				throw new ArgumentNullException(nameof(serverAddress));
			}

			if(string.IsNullOrWhiteSpace(serverPort)) {
				throw new ArgumentNullException(nameof(serverPort));
			}

			this.serverAddress = serverAddress;
			this.serverPort = serverPort;
		}

		private string GetServiceAddress() => $"http://{serverAddress}:{serverPort}/SolrService";

		private WebChannelFactory<ISolrService> channelFactory;

		public ISolrService GetSolrService()
		{
			try {
				if(channelFactory == null || channelFactory.State == CommunicationState.Closed) {
					Uri address = new Uri(GetServiceAddress());
					channelFactory = new WebChannelFactory<ISolrService>(new WebHttpBinding {
						AllowCookies = true,
					}, address);
				}
				return channelFactory.CreateChannel();
			}
			catch(Exception ex) {
				logger.Error(ex, "Ошибка создания подключения к SolrImport сервису");
				return null;
			}
		}
	}
}
