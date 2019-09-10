using System;
using System.Net.Http.Headers;
using System.Text;
using Nini.Config;
using NLog;

namespace VodovozSalesReceiptsService
{
	public static class ReceiptServiceStarter
	{
		static Logger logger = LogManager.GetCurrentClassLogger();
		static System.Timers.Timer orderRoutineTimer;

		public static void StartService(IConfig kassaConfig)
		{
			string baseAddress;
			string userNameForService;
			string pwdForService;

			try {
				baseAddress = kassaConfig.GetString("base_address");
				userNameForService = kassaConfig.GetString("user_name");
				pwdForService = kassaConfig.GetString("password");
			} catch(Exception ex) {
				logger.Fatal(ex, "Ошибка чтения конфигурационного файла.");
				return;
			}
			var authentication = new AuthenticationHeaderValue(
				"Basic",
				Convert.ToBase64String(
				Encoding.GetEncoding("ISO-8859-1")
						.GetBytes(string.Format("{0}:{1}", userNameForService, pwdForService))
				)
			);

			logger.Info("Запуск службы фискализации и печати кассовых чеков...");

			orderRoutineTimer = new System.Timers.Timer(120000); //2 минуты
			orderRoutineTimer.Elapsed += (sender, e) => {
				try {
					Fiscalization.RunAsync(baseAddress, authentication).GetAwaiter().GetResult();
				} catch(Exception ex) {
					logger.Error(ex, "Исключение при выполение фоновой задачи.");
				}
			};
			orderRoutineTimer.Start();
			logger.Info("Служба фискализации запущена");
		}
	}
}