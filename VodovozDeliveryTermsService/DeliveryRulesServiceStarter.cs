using System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using Nini.Config;
using NLog;
using VodovozDeliveryTermsAPI.Models;

namespace VodovozDeliveryTermsService
{
    public class DeliveryRulesServiceStarter
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static System.Timers.Timer onlineStoreCatalogSyncTimer;

        public static void StartService(IConfig serviceConfig)
        {
            string serviceHostName;
            string servicePort;

            try
            {
                serviceHostName = serviceConfig.GetString("service_host_name");
                servicePort = serviceConfig.GetString("service_port");
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Ошибка чтения конфигурационного файла.");
                throw;
            }

            logger.Info(String.Format("Запуск службы мобильного приложения"));

            WebServiceHost mobileHost = new WebServiceHost(typeof(DeliveryTerms));

            DeliveryTerms.BaseUrl = String.Format("http://{0}:{1}/DeliveryRules", serviceHostName, servicePort);
            mobileHost.AddServiceEndpoint(
                typeof(IDeliveryTerms),
                new WebHttpBinding(),
                DeliveryTerms.BaseUrl
            );

            //mobileHost.Description.Behaviors.Add(new PreFilter());

            mobileHost.Open();

            //Запускаем таймеры рутины
            onlineStoreCatalogSyncTimer = new System.Timers.Timer(3600000); //1 час
            onlineStoreCatalogSyncTimer.Elapsed += OnlineStoreCatalogSyncTimer_Elapsed;
            onlineStoreCatalogSyncTimer.Start();

            logger.Info("Server started.");
        }

        private static bool onlineStoreSyncRunning = false;

        static void OnlineStoreCatalogSyncTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //if (onlineStoreSyncRunning)
            //    return;

            ////Выполняем сихнронизацию только с 8 до 23.
            //if (DateTime.Now.Hour < 7 || DateTime.Now.Hour > 23)
            //    return;

            //try
            //{
            //    onlineStoreSyncRunning = true;
            //    BackgroundTask.OnlineStoreCatalogSync();
            //}
            //catch (Exception ex)
            //{
            //    logger.Error(ex, "Исключение при выполение фоновой задачи.");
            //}
            //finally
            //{
            //    onlineStoreSyncRunning = false;
            //}
        }

        static void AppDomain_CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Fatal((Exception)e.ExceptionObject, "UnhandledException");
        }
    }
}
