using System;
using System.Runtime.Serialization;
using Vodovoz.Domain.Orders;
using Vodovoz.Domain.Client;
using Gamma.Utilities;
using System.Collections.Generic;
using QSContacts;

namespace Android
{
	[DataContract]
	public class OrderDTO
	{
		[DataMember]
		public int Id;

		[DataMember]
		public string Title;

		//Регион
		[DataMember]
		public string Region;

		//Район области
		[DataMember]
		public string CityDistrict;

		//Район города
		[DataMember]
		public string StreetDistrict;

		//Широта
		[DataMember]
		public decimal? Latitude;

		//Долгота
		[DataMember]
		public decimal? Longitude;

		//Комментарий к адресу
		[DataMember]
		public string DeliveryPointComment;

		[DataMember]
		public string DPContact;

		[DataMember]
		public string DPPhone;

		[DataMember]
		public List<string> CPPhones;

		[DataMember]
		public List<string> OrderItems;

		[DataMember]
		public List<string> OrderEquipment;

		//Расписание доставки
		[DataMember]
		public string DeliverySchedule;

		[DataMember]
		public string OrderStatus;

		[DataMember]
		public string RouteListItemStatus;

		//Комментарий к заказу
		[DataMember]
		public string OrderComment;

		//Контрагент
		[DataMember]
		public string Counterparty;

		[DataMember]
		public string Address;

		public OrderDTO (Order order)
		{
			Id = order.Id;
			Title = order.Title;
			Region = order.DeliveryPoint.Region;
			CityDistrict = order.DeliveryPoint.CityDistrict;
			StreetDistrict = order.DeliveryPoint.StreetDistrict;
			Latitude = order.DeliveryPoint.Latitude;
			Longitude = order.DeliveryPoint.Longitude;
			DeliveryPointComment = order.DeliveryPoint.Comment;
			Address = order.DeliveryPoint.CompiledAddress;
			DeliverySchedule = order.DeliverySchedule.DeliveryTime;
			OrderStatus = order.OrderStatus.GetEnumTitle();
			RouteListItemStatus = "Тут будет статус"; //FIXME
			OrderComment = order.Comment;
			Counterparty = order.Client.FullName;

			if (order.DeliveryPoint.Contact != null)
			{
				DPContact = order.DeliveryPoint.Contact.FullName;
			}
			else 
			{
				DPContact = "Контактное лицо не указано";
			}

			DPPhone = order.DeliveryPoint.Phone;
			CPPhones= new List<string> ();
			foreach (Phone phone in order.Client.Phones) {
				CPPhones.Add (String.Format("{0}: {1}", phone.NumberType.Name, phone.Number));
			}

			OrderItems = new List<string> ();
			foreach (OrderItem item in order.OrderItems) {
				OrderItems.Add (String.Format ("{0}: {1} {2}", item.NomenclatureString, item.Count, item.Nomenclature.Unit == null ? String.Empty : item.Nomenclature.Unit.Name));
			}

			OrderEquipment = new List<string> ();
			foreach (OrderEquipment equipment in order.OrderEquipments) {
				OrderEquipment.Add (String.Format ("{0}: {1}", equipment.NameString, equipment.DirectionString));
			}
		}
	}
}

