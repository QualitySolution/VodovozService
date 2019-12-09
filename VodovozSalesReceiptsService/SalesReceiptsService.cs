using System;
using System.Linq;
using NLog;
using QS.DomainModel.UoW;
using Vodovoz.Domain.Client;
using Vodovoz.EntityRepositories.Orders;
using Vodovoz.Services;

namespace VodovozSalesReceiptsService
{
	public class SalesReceiptsService : ISalesReceiptsService
	{
		private readonly ISalesReceiptsServiceSettings salesReceiptsServiceSettings;
		private readonly IOrderRepository orderRepository;
		ILogger logger = LogManager.GetCurrentClassLogger();

		public SalesReceiptsService(ISalesReceiptsServiceSettings salesReceiptsServiceSettings, IOrderRepository orderRepository)
		{
			this.salesReceiptsServiceSettings = salesReceiptsServiceSettings ?? throw new ArgumentNullException(nameof(salesReceiptsServiceSettings));
			this.orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
		}

		public bool ServiceStatus()
		{
			logger.Info("Запрос статуса службы отправки кассовых чеков");
			try {
				using(var uow = UnitOfWorkFactory.CreateWithoutRoot()) {
					int receiptsToSend = 0;
					var ordersAndReceiptNodes = orderRepository.GetShippedOrdersWithReceiptsForDates(uow, PaymentType.cash, DateTime.Today.AddDays(-3));
					var withoutReceipts = ordersAndReceiptNodes.Where(r => r.ReceiptId == null);
					var withNotSentReceipts = ordersAndReceiptNodes.Where(r => r.ReceiptId.HasValue && r.WasSent != true);
					receiptsToSend = withoutReceipts.Count() + withNotSentReceipts.Count();

					logger.Info($"Количество чеков на отправку: {receiptsToSend}");
					if(receiptsToSend > salesReceiptsServiceSettings.MaxUnsendedCashReceiptsForWorkingService) {
						return false;
					}
					return true;
				}
			}
			catch(Exception ex) {
				logger.Error(ex, "Ошибка при проверке работоспособности службы отправки кассовых чеков");
				return false;
			}
		}
	}
}
