using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using MySql.Data.MySqlClient;
using Nini.Config;
using NLog;
using QS.Project.DB;
using QSProjectsLib;
using QSSupportLib;

namespace DeliveryTermsAPI
{
    public class WebApiApplication : HttpApplication
    {
        static string configFile
        {
            get
            {
                if(IsLinux())
                    return "/etc/vodovoz-delivery-rules-api.conf";

                return "vodovoz-delivery-rules-api.conf";
            }
        }
        private static Logger logger = NLog.LogManager.GetCurrentClassLogger();
        // private static string configFile = "vodovoz-delivery-rules-api.conf";
       // private static string configFile = "/etc/vodovoz-delivery-rules-api.conf";

        //Mysql
        private static string mysqlServerHostName;
        private static string mysqlServerPort;
        private static string mysqlUser;
        private static string mysqlPassword;
        private static string mysqlDatabase;

        private static string osrmServerUrl;
        public static bool IsLinux()
        {
            int p = (int)Environment.OSVersion.Platform;
            if ((p == 4) || (p == 6) || (p == 128))
            {
                return true;
            }

            return false;
        }

        protected void Application_Start()
        {
            AppDomain.CurrentDomain.UnhandledException += AppDomain_CurrentDomain_UnhandledException;

            try
            {
                IniConfigSource confFile = new IniConfigSource(configFile);
                confFile.Reload();
                IConfig OsrmConfig = confFile.Configs["OsrmService"];
                osrmServerUrl = OsrmConfig.GetString("server_url");

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
                QSOsm.Osrm.OsrmMain.ServerUrl = osrmServerUrl;// ;
                MainSupport.LoadBaseParameters();
               

                AreaRegistration.RegisterAllAreas();
                GlobalConfiguration.Configure(WebApiConfig.Register);
                FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
                RouteConfig.RegisterRoutes(RouteTable.Routes);
                BundleConfig.RegisterBundles(BundleTable.Bundles);

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
    }
}
