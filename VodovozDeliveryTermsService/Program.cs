using System;
using NLog;
using MySql.Data.MySqlClient;
using Nini.Config;
using QS.Project.DB;
using QSProjectsLib;
using System.Threading;
using Mono.Unix;
using Mono.Unix.Native;
using QSSupportLib;

namespace VodovozDeliveryTermsService
{
    class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        //private static string configFile = "vodovoz-delivery-rules-api.conf";
        private static string configFile = "/etc/vodovoz-delivery-rules-api.conf";

        //Mysql
        private static string mysqlServerHostName;
        private static string mysqlServerPort;
        private static string mysqlUser;
        private static string mysqlPassword;
        private static string mysqlDatabase;

        private static string osrmServerUrl;

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += AppDomain_CurrentDomain_UnhandledException;
            IConfig serviceConfig;

            try
            {
                IniConfigSource confFile = new IniConfigSource(configFile);
                confFile.Reload();
                serviceConfig = confFile.Configs["Service"];


                IConfig osrmConfig = confFile.Configs["OsrmService"];
                osrmServerUrl = osrmConfig.GetString("server_url");

                IConfig mysqlConfig = confFile.Configs["Mysql"];
                mysqlServerHostName = mysqlConfig.GetString("mysql_server_host_name");
                mysqlServerPort = mysqlConfig.GetString("mysql_server_port", "3306");
                mysqlUser = mysqlConfig.GetString("mysql_user");
                mysqlPassword = mysqlConfig.GetString("mysql_password");
                mysqlDatabase = mysqlConfig.GetString("mysql_database");
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Error reading config file.");
                return;
            }

            try
            {
                var conStrBuilder = new MySqlConnectionStringBuilder();
                conStrBuilder.Server = mysqlServerHostName;
                conStrBuilder.Port = UInt32.Parse(mysqlServerPort);
                conStrBuilder.Database = mysqlDatabase;
                conStrBuilder.UserID = mysqlUser;
                conStrBuilder.Password = mysqlPassword;
                conStrBuilder.SslMode = MySqlSslMode.None;

                QSMain.ConnectionString = conStrBuilder.GetConnectionString(true);
                var dbConfig = FluentNHibernate.Cfg.Db.MySQLConfiguration.Standard
                    .Dialect<NHibernate.Spatial.Dialect.MySQL57SpatialDialect>()
                    .ConnectionString(QSMain.ConnectionString);

                OrmConfig.ConfigureOrm(dbConfig,
                    new[] {

                       System.Reflection.Assembly.GetAssembly (typeof(QS.Banks.Domain.Bank)),
                       System.Reflection.Assembly.GetAssembly (typeof(QS.Contacts.Phone)),
                       System.Reflection.Assembly.GetAssembly (typeof(QS.Project.Domain.UserBase)),
                       System.Reflection.Assembly.GetAssembly (typeof(Vodovoz.HibernateMapping.OrganizationMap))
                    });
                QSOsm.Osrm.OsrmMain.ServerUrl = osrmServerUrl;// ;
                MainSupport.LoadBaseParameters();
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Ошибка в настройке подключения к БД.");
            }

            logger.Info("Запуск службы правил доставки");
            try
            {
                DeliveryRulesServiceStarter.StartService(serviceConfig);

                UnixSignal[] signals = {
                    new UnixSignal (Signum.SIGINT),
                    new UnixSignal (Signum.SIGHUP),
                    new UnixSignal (Signum.SIGTERM)};
                UnixSignal.WaitAny(signals);
            }
            catch (Exception e)
            {
                logger.Fatal(e);
            }
            finally
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
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
