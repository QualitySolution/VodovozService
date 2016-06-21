using System;
using System.Runtime.Serialization;
using Vodovoz.Domain.Orders;
using Vodovoz.Domain.Client;
using Gamma.Utilities;
using System.Collections.Generic;
using QSContacts;
using Vodovoz.Domain.Logistic;

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
		public string RouteListItemStatus;

		//Комментарий к заказу
		[DataMember]
		public string OrderComment;

		//Контрагент
		[DataMember]
		public string Counterparty;

		[DataMember]
		public string Address;

		[DataMember]
		public string BottlesReturn;

		public OrderDTO (RouteListItem item)
		{
			Id = item.Order.Id;
			Title = item.Order.Title;
			Region = item.Order.DeliveryPoint.Region;
			CityDistrict = item.Order.DeliveryPoint.CityDistrict;
			StreetDistrict = item.Order.DeliveryPoint.StreetDistrict;
			Latitude = item.Order.DeliveryPoint.Latitude;
			Longitude = item.Order.DeliveryPoint.Longitude;
			DeliveryPointComment = item.Order.DeliveryPoint.Comment;
			Address = item.Order.DeliveryPoint.CompiledAddress;
			DeliverySchedule = item.Order.DeliverySchedule.DeliveryTime;
			RouteListItemStatus = item.Status.GetEnumTitle ();
			OrderComment = item.Order.Comment;
			Counterparty = item.Order.Client.FullName;
			BottlesReturn = item.DriverBottlesReturned == null ? null :item.DriverBottlesReturned.ToString() ;

			if (item.Order.DeliveryPoint.Contact != null)
			{
				DPContact = item.Order.DeliveryPoint.Contact.FullName;
			}
			else 
			{
				DPContact = "Контактное лицо не указано";
			}

			DPPhone = item.Order.DeliveryPoint.Phone;
			CPPhones= new List<string> ();
			foreach (Phone phone in item.Order.Client.Phones) {
				CPPhones.Add (String.Format("{0}: {1}", phone.NumberType.Name, phone.Number));
			}

			OrderItems = new List<string> ();
			foreach (OrderItem orderItem in item.Order.OrderItems) {
				OrderItems.Add (String.Format ("{0}: {1} {2}", orderItem.NomenclatureString, orderItem.Count, orderItem.Nomenclature.Unit == null ? String.Empty : orderItem.Nomenclature.Unit.Name));
			}

			OrderEquipment = new List<string> ();
			foreach (OrderEquipment equipment in item.Order.OrderEquipments) {
				OrderEquipment.Add (String.Format ("{0}: {1}", equipment.NameString, equipment.DirectionString));
			}
		}
	}
}

