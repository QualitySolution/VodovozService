using System;
using System.Threading;
using Mono.Unix;
using Mono.Unix.Native;
using MySql.Data.MySqlClient;
using Nini.Config;
using NLog;
using QS.Project.DB;
using QSProjectsLib;
using QSSupportLib;
using SmsBlissSendService;
using SmsSendInterface;
using Vodovoz.Core.DataService;

namespace VodovozSmsInformerService
{
	class Service
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private static readonly string configFile = "/etc/vodovoz-smsinformer-service.conf";

		//Mysql
		private static string mysqlServerHostName;
		private static string mysqlServerPort;
		private static string mysqlUser;
		private static string mysqlPassword;
		private static string mysqlDatabase;

		//SmsService
		private static string smsServiceLogin;
		private static string smsServicePassword;

		static NewClientSmsInformer newClientInformer;

		public static void Main(string[] args)
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;;
			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;;

			try {
				IniConfigSource confFile = new IniConfigSource(configFile);
				confFile.Reload();

				IConfig mysqlConfig = confFile.Configs["Mysql"];
				mysqlServerHostName = mysqlConfig.GetString("mysql_server_host_name");
				mysqlServerPort = mysqlConfig.GetString("mysql_server_port", "3306");
				mysqlUser = mysqlConfig.GetString("mysql_user");
				mysqlPassword = mysqlConfig.GetString("mysql_password");
				mysqlDatabase = mysqlConfig.GetString("mysql_database");

				IConfig smsConfig = confFile.Configs["SmsService"];
				smsServiceLogin = smsConfig.GetString("sms_service_login");
				smsServicePassword = smsConfig.GetString("sms_service_password");

			}
			catch(Exception ex) {
				logger.Fatal(ex, "Ошибка чтения конфигурационного файла.");
				return;
			}

			try {
				var conStrBuilder = new MySqlConnectionStringBuilder();
				conStrBuilder.Server = mysqlServerHostName;
				conStrBuilder.Port = UInt32.Parse(mysqlServerPort);
				conStrBuilder.Database = mysqlDatabase;
				conStrBuilder.UserID = mysqlUser;
				conStrBuilder.Password = mysqlPassword;
				conStrBuilder.SslMode = MySqlSslMode.None;

				QSMain.ConnectionString = conStrBuilder.GetConnectionString(true);
				var db_config = FluentNHibernate.Cfg.Db.MySQLConfiguration.Standard
										 .Dialect<NHibernate.Spatial.Dialect.MySQL57SpatialDialect>()
										 .ConnectionString(QSMain.ConnectionString);

				OrmConfig.ConfigureOrm(db_config,
					new System.Reflection.Assembly[] {
					System.Reflection.Assembly.GetAssembly (typeof(Vodovoz.HibernateMapping.OrganizationMap)),
					System.Reflection.Assembly.GetAssembly (typeof(QS.Banks.Domain.Bank)),
					System.Reflection.Assembly.GetAssembly (typeof(QS.Contacts.Phone)),
					System.Reflection.Assembly.GetAssembly (typeof(QS.HistoryLog.HistoryMain)),
					System.Reflection.Assembly.GetAssembly (typeof(QS.Project.Domain.UserBase))
				});

				MainSupport.LoadBaseParameters();
				QS.HistoryLog.HistoryMain.Enable();

				SmsBlissSendController smsSender = new SmsBlissSendController(smsServiceLogin, smsServicePassword);
				newClientInformer = new NewClientSmsInformer(smsSender);
				newClientInformer.Start();

				BaseParametersProvider parametersProvider = new BaseParametersProvider();
				LowBalanceNotifier lowBalanceNotifier = new LowBalanceNotifier(smsSender, smsSender, parametersProvider);
				lowBalanceNotifier.Start();

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

		static void CurrentDomain_ProcessExit(object sender, EventArgs e)
		{
			newClientInformer?.Stop();
		}
	}
}
