﻿using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using System.Threading;
using Android;
using EmailService;
using Mono.Unix;
using Mono.Unix.Native;
using MySql.Data.MySqlClient;
using Nini.Config;
using NLog;
using QS.Project.DB;
using QSOsm;
using QSProjectsLib;
using QSSupportLib;
using Vodovoz.MobileService;
using Chats;
using VodovozOSMService;

namespace VodovozService
{
	class Service
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();
		private static string configFile = "/etc/vodovozservice.conf";
		private static string server;
		private static string port;
		private static string user;
		private static string pass;
		private static string db;
		private static string firebaseServerApiToken;
		private static string firebaseSenderId;
		private static string servicePort;
		private static string serviceHostName;
		private static string externalAddress;
		private static System.Timers.Timer orderRoutineTimer;
		private static System.Timers.Timer trackRoutineTimer;
		private static System.Timers.Timer onlineStoreCatalogSyncTimer;

		public static void Main(string[] args)
		{
			AppDomain.CurrentDomain.UnhandledException += AppDomain_CurrentDomain_UnhandledException;
			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
			try {
				IniConfigSource confFile = new IniConfigSource(configFile);
				confFile.Reload();
				IConfig config = confFile.Configs["General"];
				server = config.GetString("server");
				port = config.GetString("port", "3306");
				user = config.GetString("user");
				pass = config.GetString("password");
				db = config.GetString("database");
				firebaseServerApiToken = config.GetString("server_api_token");
				firebaseSenderId = config.GetString("firebase_sender");
				servicePort = config.GetString("service_port");
				serviceHostName = config.GetString("service_host_name");
				externalAddress = config.GetString("external_address");

				OsmService.ConfigureService(confFile);

			}
			catch(Exception ex) {
				logger.Fatal(ex, "Ошибка чтения конфигурационного файла.");
				return;
			}

			WebServiceHost OsmHost = new WebServiceHost(typeof(OsmService));

			logger.Info(String.Format("Создаем и запускаем службы..."));
			try {
				var conStrBuilder = new MySqlConnectionStringBuilder();
				conStrBuilder.Server = server;
				conStrBuilder.Port = UInt32.Parse(port);
				conStrBuilder.Database = db;
				conStrBuilder.UserID = user;
				conStrBuilder.Password = pass;
				conStrBuilder.SslMode = MySqlSslMode.None;

				QSMain.ConnectionString = conStrBuilder.GetConnectionString(true);
				var db_config = FluentNHibernate.Cfg.Db.MySQLConfiguration.Standard
										 .Dialect<NHibernate.Spatial.Dialect.MySQL57SpatialDialect>()
										 .ConnectionString(QSMain.ConnectionString);

				OrmConfig.ConfigureOrm(db_config,
					new System.Reflection.Assembly[] {
					System.Reflection.Assembly.GetAssembly (typeof(Vodovoz.HibernateMapping.OrganizationMap)),
					System.Reflection.Assembly.GetAssembly (typeof(QS.Banks.Domain.Bank)),
					System.Reflection.Assembly.GetAssembly (typeof(QSContacts.QSContactsMain)),
					System.Reflection.Assembly.GetAssembly (typeof(EmailService.Email)),
					System.Reflection.Assembly.GetAssembly (typeof(QS.HistoryLog.HistoryMain)),
					System.Reflection.Assembly.GetAssembly (typeof(QS.Project.Domain.UserBase))
				});

				MainSupport.LoadBaseParameters();
				QS.HistoryLog.HistoryMain.Enable();

				FCMHelper.Configure(firebaseServerApiToken, firebaseSenderId);

				ServiceHost ChatHost = new ServiceHost(typeof(ChatService));
				ServiceHost AndroidDriverHost = new ServiceHost(typeof(AndroidDriverService));
				ServiceHost EmailSendingHost = new ServiceHost(typeof(EmailService.EmailService));
				WebServiceHost MailjetEventsHost = new WebServiceHost(typeof(EmailService.EmailService));
				WebServiceHost mobileHost = new WebServiceHost(typeof(MobileService));

				ChatHost.AddServiceEndpoint(
					typeof(IChatService),
					new BasicHttpBinding(),
					String.Format("http://{0}:{1}/ChatService", serviceHostName, servicePort)
				);
				AndroidDriverHost.AddServiceEndpoint(
					typeof(IAndroidDriverService),
					new BasicHttpBinding(),
					String.Format("http://{0}:{1}/AndroidDriverService", serviceHostName, servicePort)
				);
				EmailSendingHost.AddServiceEndpoint(
					typeof(IEmailService),
					new BasicHttpBinding(),
					String.Format("http://{0}:{1}/EmailService", serviceHostName, servicePort)
				);
				MailjetEventsHost.AddServiceEndpoint(
					typeof(IMailjetEventService),
					new WebHttpBinding(),
					String.Format("http://{0}:{1}/Mailjet", serviceHostName, servicePort)
				);

				MobileService.BaseUrl = String.Format("http://{0}:{1}/Mobile", serviceHostName, servicePort);
				mobileHost.AddServiceEndpoint(
					typeof(IMobileService),
					new WebHttpBinding(),
					MobileService.BaseUrl
				);

				OsmWorker.ServiceHost = serviceHostName;
				OsmWorker.ServicePort = Int32.Parse(servicePort);
				OsmHost.AddServiceEndpoint(
					typeof(IOsmService),
					new WebHttpBinding(),
					OsmWorker.ServiceAddress
				);

				//FIXME Тут добавлен без дебага, потому что без него не работает отдача изображений в потоке. Метод Stream GetImage(string filename)
				// Просто не смог быстро разобраться. А конкретнее нужна строка reply = TraceMessage (reply.CreateBufferedCopy (int.MaxValue), Action.Send);
				// видимо она как то обрабатывает сообщение.
				mobileHost.Description.Behaviors.Add(new PreFilter());

#if DEBUG
				ChatHost.Description.Behaviors.Add(new PreFilter());
				AndroidDriverHost.Description.Behaviors.Add(new PreFilter());
				EmailSendingHost.Description.Behaviors.Add(new PreFilter());
				MailjetEventsHost.Description.Behaviors.Add(new PreFilter());
				OsmHost.Description.Behaviors.Add(new PreFilter());
#endif

				ChatHost.Open();
				AndroidDriverHost.Open();
				EmailSendingHost.Open();
				MailjetEventsHost.Open();
				mobileHost.Open();
				OsmHost.Open();

				//Запускаем таймеры рутины
				orderRoutineTimer = new System.Timers.Timer(120000); //2 минуты
				orderRoutineTimer.Elapsed += OrderRoutineTimer_Elapsed;
				orderRoutineTimer.Start();
				trackRoutineTimer = new System.Timers.Timer(30000); //30 секунд
				trackRoutineTimer.Elapsed += TrackRoutineTimer_Elapsed;
				trackRoutineTimer.Start();
				onlineStoreCatalogSyncTimer = new System.Timers.Timer(3600000); //1 час
				onlineStoreCatalogSyncTimer.Elapsed += OnlineStoreCatalogSyncTimer_Elapsed;
				onlineStoreCatalogSyncTimer.Start();

				logger.Info("Server started.");

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
				if(OsmHost.State == CommunicationState.Opened)
					OsmHost.Close();

				if(Environment.OSVersion.Platform == PlatformID.Unix)
					Thread.CurrentThread.Abort();
				Environment.Exit(0);
			}
		}

		static void CurrentDomain_ProcessExit(object sender, EventArgs e)
		{
			EmailManager.StopWorkers();
		}


		static void TrackRoutineTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			TracksService.RemoveOldWorkers();
		}

		static void OrderRoutineTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			try {
				BackgroundTask.OrderTimeIsRunningOut();
			}
			catch(Exception ex) {
				logger.Error(ex, "Исключение при выполение фоновой задачи.");
			}
		}

		private static bool onlineStoreSyncRunning = false;

		static void OnlineStoreCatalogSyncTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if(onlineStoreSyncRunning)
				return;

			//Выполняем сихнронизацию только с 8 до 23.
			if(DateTime.Now.Hour < 7 || DateTime.Now.Hour > 23)
				return;

			try {
				onlineStoreSyncRunning = true;
				BackgroundTask.OnlineStoreCatalogSync();
			}
			catch(Exception ex) {
				logger.Error(ex, "Исключение при выполение фоновой задачи.");
			}
			finally {
				onlineStoreSyncRunning = false;
			}
		}

		static void AppDomain_CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			logger.Fatal((Exception)e.ExceptionObject, "UnhandledException");
		}
	}

	public class PreFilter : IServiceBehavior
	{
		public void AddBindingParameters(ServiceDescription description, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection parameters)
		{
		}

		public void Validate(ServiceDescription description, ServiceHostBase serviceHostBase)
		{
		}

		public void ApplyDispatchBehavior(ServiceDescription desc, ServiceHostBase host)
		{
			foreach(ChannelDispatcher cDispatcher in host.ChannelDispatchers)
				foreach(EndpointDispatcher eDispatcher in cDispatcher.Endpoints)
					eDispatcher.DispatchRuntime.MessageInspectors.Add(new ConsoleMessageTracer());
		}
	}

	public class ConsoleMessageTracer : IDispatchMessageInspector
	{
		static Logger logger = LogManager.GetCurrentClassLogger();

		enum Action
		{
			Send,
			Receive
		};

		private Message TraceMessage(MessageBuffer buffer, Action action)
		{
			Message msg = buffer.CreateMessage();
			try {
				if(action == Action.Receive) {
					logger.Info("Received: {0}", msg.Headers.To.AbsoluteUri);
					if(!msg.IsEmpty)
						logger.Debug("Received Body: {0}", msg);
				} else
					logger.Debug("Sended: {0}", msg);
			}
			catch(Exception ex) {
				logger.Error(ex, "Ошибка логгирования сообщения.");
			}
			return buffer.CreateMessage();
		}

		public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
		{
			request = TraceMessage(request.CreateBufferedCopy(int.MaxValue), Action.Receive);
			return null;
		}

		public void BeforeSendReply(ref Message reply, object correlationState)
		{
			reply = TraceMessage(reply.CreateBufferedCopy(int.MaxValue), Action.Send);
		}
	}
}


