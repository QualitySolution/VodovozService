using System;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
//using System.Web.Optimization;
using System.Web.Routing;
using NLog;
using MySql.Data.MySqlClient;
using Nini.Config;
using QS.Project.DB;
using QSProjectsLib;
using QSSupportLib;
using VodovozDeliveryTermsAPI.App_Start;


namespace VodovozDeliveryTermsAPI
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private static Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static string configFile = "vodovoz-delivery-rules-api.conf";
        //private static string configFile = "/etc/vodovoz-delivery-rules-api.conf";

        //Mysql
        private static string mysqlServerHostName;
        private static string mysqlServerPort;
        private static string mysqlUser;
        private static string mysqlPassword;
        private static string mysqlDatabase;

        protected void Application_Start()
        {
            AppDomain.CurrentDomain.UnhandledException += AppDomain_CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;


            try
            {
                QSOsm.Osrm.OsrmMain.ServerUrl = "http://osrm.vod.qsolution.ru:5000";

                IniConfigSource confFile = new IniConfigSource(configFile);
                confFile.Reload();
                IConfig OsrmConfig = confFile.Configs["OsrmService"];

                IConfig mysqlConfig = confFile.Configs["Mysql"];
                mysqlServerHostName = mysqlConfig.GetString("mysql_server_host_name");
                mysqlServerPort = mysqlConfig.GetString("mysql_server_port", "3306");
                mysqlUser = mysqlConfig.GetString("mysql_user");
                mysqlPassword = mysqlConfig.GetString("mysql_password");
                mysqlDatabase = mysqlConfig.GetString("mysql_database");
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Ошибка чтения конфигурационного файла.");
                return;
            }

            logger.Info(String.Format("Запуск WEB API"));

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
                var db_config = FluentNHibernate.Cfg.Db.MySQLConfiguration.Standard
                    .Dialect<NHibernate.Spatial.Dialect.MySQL57SpatialDialect>()
                    .ConnectionString(QSMain.ConnectionString);

                OrmConfig.ConfigureOrm(db_config,
                    new System.Reflection.Assembly[] {

                       System.Reflection.Assembly.GetAssembly (typeof(QS.Banks.Domain.Bank)),
                       System.Reflection.Assembly.GetAssembly (typeof(QS.Contacts.Phone)),
                       System.Reflection.Assembly.GetAssembly (typeof(QS.Project.Domain.UserBase)),
                       System.Reflection.Assembly.GetAssembly (typeof(Vodovoz.HibernateMapping.OrganizationMap))
                    });
                
                MainSupport.LoadBaseParameters();
                //QS.HistoryLog.HistoryMain.Enable();

                AreaRegistration.RegisterAllAreas();
                GlobalConfiguration.Configure(WebApiConfig.Register);
                FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
                RouteConfig.RegisterRoutes(RouteTable.Routes);
                //BundleConfig.RegisterBundles(BundleTable.Bundles);
            }
            catch (Exception e)
            {
                logger.Fatal(e);
            }
            finally
            {
                //if (Environment.OSVersion.Platform == PlatformID.Unix)
                //    Thread.CurrentThread.Abort();
                //Environment.Exit(0);
            }
        }

        static void AppDomain_CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Fatal((Exception)e.ExceptionObject, "UnhandledException");
        }

        private static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            logger.Fatal((Exception)e.Exception, "UnhandledException");
        }
    }
}
