using System;
using Nini.Config;
using NLog;
using System.ServiceModel.Web;
using System.ServiceModel.Description;
using System.ServiceModel;
using Mono.Unix;
using Mono.Unix.Native;
using System.Threading;
using SolrImportService;

namespace VodovozSolrService
{
	class Service
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private static readonly string configFile = "/etc/vodovoz-solr-service.conf";

		private static IConfig solrConfig = null;

		//Service
		private static string serviceHostName;
		private static string servicePort;

		public static void Main(string[] args)
		{
			AppDomain.CurrentDomain.UnhandledException += AppDomain_CurrentDomain_UnhandledException;

			try {
				IniConfigSource confFile = new IniConfigSource(configFile);
				confFile.Reload();
				IConfig serviceConfig = confFile.Configs["Service"];
				serviceHostName = serviceConfig.GetString("service_host_name");
				servicePort = serviceConfig.GetString("service_port");

				solrConfig = confFile.Configs["SolrServer"];

			}
			catch(Exception ex) {
				logger.Fatal(ex, "Ошибка чтения конфигурационного файла.");
				return;
			}

			try {
				SolrService service = new SolrService(solrConfig);

				SolrInstanceProvider solrInstanceProvider = new SolrInstanceProvider(service);
				WebServiceHost serviceHost = new SolrServiceHost(solrInstanceProvider);

				ServiceEndpoint endPoint = serviceHost.AddServiceEndpoint(
					typeof(ISolrService),
					new WebHttpBinding(),
					String.Format("http://{0}:{1}/SolrService", serviceHostName, servicePort)
				);

				serviceHost.Open();

				logger.Info("Solr service started");

				UnixSignal[] signals = {
						new UnixSignal (Signum.SIGINT),
						new UnixSignal (Signum.SIGHUP),
						new UnixSignal (Signum.SIGTERM)};
				UnixSignal.WaitAny(signals);
			}
			catch(Exception e) {
				logger.Fatal(e);
			}
			finally {
				if(Environment.OSVersion.Platform == PlatformID.Unix)
					Thread.CurrentThread.Abort();
				Environment.Exit(0);
			}
		}

		static void AppDomain_CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			logger.Fatal((Exception)e.ExceptionObject, "UnhandledException");
		}
	}
}
