using System;
using QSOrmProject;
using Vodovoz.Domain.Logistic;
using System.Linq;

namespace VodovozService
{
	public static class BackgroundTask
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger ();

		public static void OrderTimeIsRunningOut()
		{
			logger.Info("Рутина по обрабоке заказов...");
			var startTime = DateTime.Now;
			int messagesCount = 0;

			var UoW = UnitOfWorkFactory.CreateWithoutRoot();
			var todayAddresses = Vodovoz.Repository.Logistics.RouteListItemRepository.GetRouteListItemAtDay(UoW, DateTime.Today, RouteListItemStatus.EnRoute);

			var now = DateTime.Now.TimeOfDay;
			var nowMinus30 = DateTime.Now.TimeOfDay.Add(new TimeSpan(0, 30, 0));
			foreach(var address in todayAddresses)
			{
				if(address.Order.DeliverySchedule.To <= now)
				{
					if(address.NotifiedTimeout)
						continue;

					address.NotifiedTimeout = true;
					UoW.Save(address);

					var mes = String.Format("Доставка заказа №{1} по адресу {2} просрочена! Время доставки было {0}.",
						address.Order.DeliverySchedule.Name,
						address.Order.Id,
						address.Order.DeliveryPoint.ShortAddress
					);

					var notify = String.Format("Адрес {2} просрочен!",
						address.Order.DeliverySchedule.Name,
						address.Order.Id,
						address.Order.DeliveryPoint.ShortAddress
					);

					Chat.ChatService.SendServerNotificationToDriver(UoW, address.RouteList.Driver, mes, notify);

					messagesCount++;
					continue;
				}
				else if(address.Order.DeliverySchedule.To <= nowMinus30 && address.Notified30Minutes == false)
				{
					address.Notified30Minutes = true;
					UoW.Save(address);

					var mes = String.Format("Крайний срок доставки {0} заказа №{1} по адресу {2} наступит менее чем через 30 минут.",
						address.Order.DeliverySchedule.Name,
						address.Order.Id,
						address.Order.DeliveryPoint.ShortAddress
					);
					var notify = String.Format("Осталось менее 30 минут для доставки по адресу {2}.",
						address.Order.DeliverySchedule.Name,
						address.Order.Id,
						address.Order.DeliveryPoint.ShortAddress
					);

					Chat.ChatService.SendServerNotificationToDriver(UoW, address.RouteList.Driver, mes, notify);

					messagesCount++;
				}
			}

			logger.Info("Обрабока заказов завершена за {0} сек. Отправлено сообщений {1}.", 
				(DateTime.Now - startTime).TotalSeconds,
				messagesCount
			);
		}
	}
}

