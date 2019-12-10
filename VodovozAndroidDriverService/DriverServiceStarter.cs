using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using Android;
using Chats;
using MySql.Data.MySqlClient;
using Nini.Config;
using NLog;
using QS.Project.DB;
using QSProjectsLib;
using QSSupportLib;
using System.ServiceModel.Description;

namespace VodovozAndroidDriverService
{
	public static class DriverServiceStarter
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private static readonly string configFile = "/etc/vodovoz-driver-service.conf";

		private static System.Timers.Timer orderRoutineTimer;
		private static System.Timers.Timer trackRoutineTimer;

		public static void StartService(IConfig serviceConfig, IConfig firebaseConfig)
		{
			string serviceHostName;
			string servicePort;
			string serviceWebPort;

			string firebaseServerApiToken;
			string firebaseSenderId;

			try {
				serviceHostName = serviceConfig.GetString("service_host_name");
				servicePort = serviceConfig.GetString("service_port");
				serviceWebPort = serviceConfig.GetString("service_web_port");

				firebaseServerApiToken = firebaseConfig.GetString("firebase_server_api_token");
				firebaseSenderId = firebaseConfig.GetString("firebase_sender_id");
			}
			catch(Exception ex) {
				logger.Fatal(ex, "Ошибка чтения конфигурационного файла.");
				throw;
			}

			logger.Info(String.Format("Запуск службы для водителей"));

			FCMHelper.Configure(firebaseServerApiToken, firebaseSenderId);

			ServiceHost ChatHost = new ServiceHost(typeof(ChatService));
			ServiceHost AndroidDriverHost = new ServiceHost(typeof(AndroidDriverService));

			ChatHost.AddServiceEndpoint(
				typeof(IChatService),
				new BasicHttpBinding(),
				String.Format("http://{0}:{1}/ChatService", serviceHostName, servicePort)
			);

			ServiceEndpoint webEndPoint = AndroidDriverHost.AddServiceEndpoint(
					typeof(IAndroidDriverServiceWeb),
					new WebHttpBinding(),
					String.Format("http://{0}:{1}/AndroidDriverServiceWeb", serviceHostName, serviceWebPort)
				);
			WebHttpBehavior httpBehavior = new WebHttpBehavior();
			webEndPoint.Behaviors.Add(httpBehavior);

			AndroidDriverHost.AddServiceEndpoint(
				typeof(IAndroidDriverService),
				new BasicHttpBinding(),
				String.Format("http://{0}:{1}/AndroidDriverService", serviceHostName, servicePort)
			);

#if DEBUG
			ChatHost.Description.Behaviors.Add(new PreFilter());
			AndroidDriverHost.Description.Behaviors.Add(new PreFilter());
#endif

			ChatHost.Open();
			AndroidDriverHost.Open();

			//Запускаем таймеры рутины
			orderRoutineTimer = new System.Timers.Timer(120000); //2 минуты
			orderRoutineTimer.Elapsed += OrderRoutineTimer_Elapsed;
			orderRoutineTimer.Start();
			trackRoutineTimer = new System.Timers.Timer(30000); //30 секунд
			trackRoutineTimer.Elapsed += TrackRoutineTimer_Elapsed;
			trackRoutineTimer.Start();

			logger.Info("Server started.");
		}

		private static void TrackRoutineTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			TracksService.RemoveOldWorkers();
		}

		private static void OrderRoutineTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			try {
				BackgroundTask.OrderTimeIsRunningOut();
			}
			catch(Exception ex) {
				logger.Error(ex, "Исключение при выполение фоновой задачи.");
			}
		}
	}
}
