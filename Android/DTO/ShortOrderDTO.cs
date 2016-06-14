using System;
using System.Runtime.Serialization;
using Vodovoz.Domain.Orders;
using Vodovoz.Domain.Client;
using Gamma.Utilities;

namespace Android
{
	[DataContract]
	public class ShortOrderDTO
	{
		[DataMember]
		public int Id;

		//Расписание доставки
		[DataMember]
		public string DeliverySchedule;

		//Статус заказа
		[DataMember]
		public string OrderStatus;

		//Контрагент
		[DataMember]
		public string Counterparty;

		//Адрес
		[DataMember]
		public string Address;

		public ShortOrderDTO (Order order)
		{
			Id = order.Id;
			DeliverySchedule = order.DeliverySchedule.DeliveryTime;
			OrderStatus = order.OrderStatus.GetEnumTitle();
			Counterparty = order.Client.FullName;
			Address = order.DeliveryPoint.ShortAddress ?? String.Empty;
		}
	}
}

