using System;
using System.ServiceModel;
using System.Threading;
using Android;
using Mono.Unix;
using Mono.Unix.Native;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Collections.ObjectModel;
using MySql.Data.MySqlClient;
using QSProjectsLib;
using QSOrmProject;
using NLog;
using Nini.Config;

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

		[STAThread]
		public static void Main (string[] args)
		{
			try {
				IniConfigSource configFile = new IniConfigSource (ConfigFile);
				configFile.Reload ();	
				IConfig config = configFile.Configs ["General"];
				server = config.GetString ("server");
				port = config.GetString ("port", "3306");
				user = config.GetString ("user");
				pass = config.GetString ("password");
				db = config.GetString ("database");
			} catch (Exception ex) {
				logger.Fatal (ex, "Ошибка чтения конфигурационного файла.");
				return;
			}

			logger.Info (String.Format ("Создаем и запускаем службы..."));
			try {
				var conStrBuilder = new MySqlConnectionStringBuilder();
				conStrBuilder.Server = server;
				conStrBuilder.Port = UInt32.Parse(port);
				conStrBuilder.Database = db;
				conStrBuilder.UserID = user;
				conStrBuilder.Password = pass;

				var connStr = conStrBuilder.GetConnectionString(true);
				QSMain.ConnectionString = connStr;


				OrmMain.ConfigureOrm (QSMain.ConnectionString,
					new System.Reflection.Assembly[] {
					System.Reflection.Assembly.GetAssembly (typeof(Vodovoz.HMap.OrganizationMap)),
					System.Reflection.Assembly.GetAssembly (typeof(QSBanks.QSBanksMain)),
					System.Reflection.Assembly.GetAssembly (typeof(QSContacts.QSContactsMain))
				});
					
				ServiceHost AndroidDriverHost = new ServiceHost (typeof(AndroidDriverService));
				AndroidDriverHost.AddServiceEndpoint (
					typeof(IAndroidDriverService), 
					new BasicHttpBinding(),
					"http://rs.qsolution.ru:9000/AndroidDriverService");
				#if DEBUG
				AndroidDriverHost.Description.Behaviors.Add (new PreFilter ());
				#endif

				logger.Info("Server started.");
				AndroidDriverHost.Open();
				UnixSignal[] signals = { 
					new UnixSignal (Signum.SIGINT),
					new UnixSignal (Signum.SIGHUP),
					new UnixSignal (Signum.SIGTERM)};
				UnixSignal.WaitAny (signals);
			} catch (Exception e) {
				Console.Write (e.StackTrace);
			} finally {
				if (Environment.OSVersion.Platform == PlatformID.Unix)
					Thread.CurrentThread.Abort ();
				Environment.Exit (0);
			}
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
					Console.WriteLine("\n\nReceived: {0}", msg);
				else
					Console.WriteLine ("\n\nSended: {0}", msg);
			} catch (Exception ex) {
				Console.WriteLine ("Ошибка логгирования сообщения {0}.", ex.StackTrace);
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


