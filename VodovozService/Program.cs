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

namespace VodovozService
{
	class Service
	{
		[STAThread]
		public static void Main (string[] args)
		{
			try {
				var conStrBuilder = new MySqlConnectionStringBuilder();
				conStrBuilder.Server = "vod-srv.qsolution.ru";
				conStrBuilder.Port = 3306;
				conStrBuilder.Database = "Vodovoz";
				conStrBuilder.UserID = "vad";
				conStrBuilder.Password = "123";

				var connStr = conStrBuilder.GetConnectionString(true);
				//QSMain.connectionDB = new MySqlConnection (connStr);
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
				//	"http://vinogradov.sknt.ru:9000/AndroidDriverService");
				#if DEBUG
				//AndroidDriverHost.Description.Behaviors.Add (new PreFilter ());
				#endif

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
					Console.WriteLine ("\n\nReceived: {0}", msg);
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


