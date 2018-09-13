using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using System.Threading;
using Android;
using Mono.Unix;
using Mono.Unix.Native;
using MySql.Data.MySqlClient;
using Nini.Config;
using NLog;
using QSOrmProject;
using QSOsm;
using QSProjectsLib;
using QSSupportLib;
using Vodovoz.MobileService;
using VodovozService.Chats;
using WCFServer;

namespace VodovozService
{
	class Service
	{
		private static Logger logger = LogManager.GetCurrentClassLogger (); 
		private static string ConfigFile = "/etc/vodovozservice.conf";
		private static string server;
		private static string port;
		private static string user;
		private static string pass;
		private static string db;
		private static string firebaseServerApiToken;
		private static string firebaseSenderId;
		private static string servicePort;
		private static string serviceHostName;
		private static System.Timers.Timer OrderRoutineTimer;
		private static System.Timers.Timer TrackRoutineTimer;

		public static void Main (string[] args)
		{
			AppDomain.CurrentDomain.UnhandledException += AppDomain_CurrentDomain_UnhandledException;
			try {
				IniConfigSource configFile = new IniConfigSource (ConfigFile);
				configFile.Reload ();	
				IConfig config = configFile.Configs ["General"];
				server = config.GetString ("server");
				port = config.GetString ("port", "3306");
				user = config.GetString ("user");
				pass = config.GetString ("password");
				db = config.GetString ("database");
				firebaseServerApiToken = config.GetString ("server_api_token");
				firebaseSenderId = config.GetString ("firebase_sender");
				servicePort = config.GetString ("service_port");
				serviceHostName = config.GetString ("service_host_name");

				OsmService.ConfigureService (configFile);

			} catch (Exception ex) {
				logger.Fatal (ex, "Ошибка чтения конфигурационного файла.");
				return;
			}

			WebServiceHost OsmHost = new WebServiceHost (typeof (OsmService));

			logger.Info (String.Format ("Создаем и запускаем службы..."));
			try {
				var conStrBuilder = new MySqlConnectionStringBuilder();
				conStrBuilder.Server = server;
				conStrBuilder.Port = UInt32.Parse(port);
				conStrBuilder.Database = db;
				conStrBuilder.UserID = user;
				conStrBuilder.Password = pass;

				QSMain.ConnectionString = conStrBuilder.GetConnectionString(true);
                var db_config = FluentNHibernate.Cfg.Db.MySQLConfiguration.Standard
                                         .Dialect<NHibernate.Spatial.Dialect.MySQL57SpatialDialect>()
					                     .ConnectionString(QSMain.ConnectionString);

				OrmMain.ConfigureOrm (db_config,
					new System.Reflection.Assembly[] {
					System.Reflection.Assembly.GetAssembly (typeof(Vodovoz.HibernateMapping.OrganizationMap)),
					System.Reflection.Assembly.GetAssembly (typeof(QSBanks.QSBanksMain)),
					System.Reflection.Assembly.GetAssembly (typeof(QSContacts.QSContactsMain))
				});

				MainSupport.LoadBaseParameters ();

				FCMHelper.Configure(firebaseServerApiToken, firebaseSenderId);
					
				ServiceHost ChatHost = new ServiceHost (typeof(ChatService));
				ServiceHost AndroidDriverHost = new ServiceHost (typeof(AndroidDriverService));
				ServiceHost MobileHost = new WebServiceHost(typeof(MobileService));

				ChatHost.AddServiceEndpoint (
					typeof (IChatService),
					new BasicHttpBinding(),
					String.Format("http://{0}:{1}/ChatService", serviceHostName, servicePort)
				);
				AndroidDriverHost.AddServiceEndpoint (
					typeof(IAndroidDriverService), 
					new BasicHttpBinding(),
					String.Format("http://{0}:{1}/AndroidDriverService", serviceHostName, servicePort)
				);

				MobileHost.AddServiceEndpoint(
					typeof(IMobileService),
					new WebHttpBinding(),
					String.Format("http://{0}:{1}/Mobile", serviceHostName, servicePort)
				);

				OsmWorker.ServiceHost = serviceHostName;
				OsmWorker.ServicePort = Int32.Parse (servicePort);
				OsmHost.AddServiceEndpoint (typeof (IOsmService), new WebHttpBinding (), OsmWorker.ServiceAddress);
				
				#if DEBUG
				ChatHost.Description.Behaviors.Add (new PreFilter());
				AndroidDriverHost.Description.Behaviors.Add (new PreFilter ());
				MobileHost.Description.Behaviors.Add(new PreFilter());
				OsmHost.Description.Behaviors.Add (new PreFilter ());
				#endif

				ChatHost.Open();
				AndroidDriverHost.Open();
				MobileHost.Open();
				OsmHost.Open ();

				//Запускаем таймеры рутины
				OrderRoutineTimer = new System.Timers.Timer(120000); //2 минуты
				OrderRoutineTimer.Elapsed += OrderRoutineTimer_Elapsed;
				OrderRoutineTimer.Start();
				TrackRoutineTimer = new System.Timers.Timer(30000); //30 секунд
				TrackRoutineTimer.Elapsed += TrackRoutineTimer_Elapsed;
				TrackRoutineTimer.Start();

				logger.Info("Server started.");

				UnixSignal[] signals = { 
					new UnixSignal (Signum.SIGINT),
					new UnixSignal (Signum.SIGHUP),
					new UnixSignal (Signum.SIGTERM)};
				UnixSignal.WaitAny (signals);
			} catch (Exception e) {
				logger.Fatal (e);
			} finally {
				if (OsmHost.State == CommunicationState.Opened)
					OsmHost.Close ();

				if (Environment.OSVersion.Platform == PlatformID.Unix)
					Thread.CurrentThread.Abort ();
				Environment.Exit (0);
			}
		}

		static void TrackRoutineTimer_Elapsed (object sender, System.Timers.ElapsedEventArgs e)
		{
			TracksService.RemoveOldWorkers();
		}

		static void OrderRoutineTimer_Elapsed (object sender, System.Timers.ElapsedEventArgs e)
		{
			try{
				BackgroundTask.OrderTimeIsRunningOut();
			}
			catch (Exception ex)
			{
				logger.Error(ex, "Исключение при выполение фоновой задачи.");
			}
		}

		static void AppDomain_CurrentDomain_UnhandledException (object sender, UnhandledExceptionEventArgs e)
		{
			logger.Fatal((Exception)e.ExceptionObject, "UnhandledException");
		}
	}
		
	public class PreFilter : IServiceBehavior
	{
		public void AddBindingParameters (ServiceDescription description, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection parameters)
		{
		}

		public void Validate (ServiceDescription description, ServiceHostBase serviceHostBase)
		{
		}

		public void ApplyDispatchBehavior (ServiceDescription desc, ServiceHostBase host)
		{
			foreach (ChannelDispatcher cDispatcher in host.ChannelDispatchers)
				foreach (EndpointDispatcher eDispatcher in cDispatcher.Endpoints)
					eDispatcher.DispatchRuntime.MessageInspectors.Add (new ConsoleMessageTracer ());
		}
	}

	public class ConsoleMessageTracer: IDispatchMessageInspector
	{
		static Logger logger = LogManager.GetCurrentClassLogger ();

		enum Action
		{	
			Send,
			Receive
		};

		private Message TraceMessage (MessageBuffer buffer, Action action)
		{
			Message msg = buffer.CreateMessage ();
			try {
				if (action == Action.Receive)
					logger.Debug("Received: {0}", msg);
				else
					logger.Debug("Sended: {0}", msg);
			} catch (Exception ex) {
				logger.Error (ex, "Ошибка логгирования сообщения.");
			}
			return buffer.CreateMessage ();
		}

		public object AfterReceiveRequest (ref Message request, IClientChannel channel, InstanceContext instanceContext)
		{
			request = TraceMessage (request.CreateBufferedCopy (int.MaxValue), Action.Receive);
			return null;
		}

		public void BeforeSendReply (ref Message reply, object correlationState)
		{
			reply = TraceMessage (reply.CreateBufferedCopy (int.MaxValue), Action.Send);
		}
	}
}


