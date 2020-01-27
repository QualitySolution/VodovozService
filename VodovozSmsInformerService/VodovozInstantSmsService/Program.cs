using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Threading;
using InstantSmsService;
using Mono.Unix;
using Mono.Unix.Native;
using Nini.Config;
using NLog;
using SmsBlissSendService;

namespace VodovozInstantSmsService
{
	public class Service
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private static readonly string configFile = "/etc/vodovoz-instant-sms-service.conf";

		//Service
		private static string serviceHostName;
		private static string servicePort;

		//SmsService
		private static string smsServiceLogin;
		private static string smsServicePassword;

		public static void Main(string[] args)
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException; ;

			try {
				IniConfigSource confFile = new IniConfigSource(configFile);
				confFile.Reload();

				IConfig serviceConfig = confFile.Configs["Service"];
				serviceHostName = serviceConfig.GetString("service_host_name");
				servicePort = serviceConfig.GetString("service_port");

				IConfig smsConfig = confFile.Configs["SmsService"];
				smsServiceLogin = smsConfig.GetString("sms_service_login");
				smsServicePassword = smsConfig.GetString("sms_service_password");

			}
			catch(Exception ex) {
				logger.Fatal(ex, "Ошибка чтения конфигурационного файла.");
				return;
			}

			try {
				SmsBlissSendController smsSender = new SmsBlissSendController(smsServiceLogin, smsServicePassword, SmsSendInterface.BalanceType.CurrencyBalance);
				InstantSmsServiceInstanceProvider instantSmsInstanceProvider = new InstantSmsServiceInstanceProvider(smsSender);
				ServiceHost InstantSmsServiceHost = new InstantSmsServiceHost(instantSmsInstanceProvider);

				InstantSmsServiceHost.AddServiceEndpoint(
					typeof(IInstantSmsService),
					new BasicHttpBinding(),
					String.Format("http://{0}:{1}/InstantSmsService", serviceHostName, servicePort)
				);

				InstantSmsServiceHost.Open();
				logger.Info("Запущена служба отправки моментальных sms сообщений");

				InstantSmsInformerInstanceProvider instantSmsInformerInstanceProvider = new InstantSmsInformerInstanceProvider(InstantSmsServiceHost, smsSender);
				WebServiceHost instantSmsInformerStatus = new InstantSmsInformerHost(instantSmsInformerInstanceProvider);
				instantSmsInformerStatus.AddServiceEndpoint(
					typeof(IInstantSmsInformerService),
					new WebHttpBinding(),
					String.Format("http://{0}:{1}/InstantSmsInformer", serviceHostName, servicePort)
				);
				instantSmsInformerStatus.Open();
				logger.Info("Запущена служба мониторинга отправки моментальных sms сообщений");

				UnixSignal[] signals = {
					new UnixSignal (Signum.SIGINT),
					new UnixSignal (Signum.SIGHUP),
					new UnixSignal (Signum.SIGTERM)};
				UnixSignal.WaitAny(signals);
			}
			catch(Exception ex) {
				logger.Fatal(ex);
			}
			finally {
				if(Environment.OSVersion.Platform == PlatformID.Unix)
					Thread.CurrentThread.Abort();
				Environment.Exit(0);
			}
		}

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			logger.Fatal((Exception)e.ExceptionObject, "UnhandledException");
		}
	}
}
