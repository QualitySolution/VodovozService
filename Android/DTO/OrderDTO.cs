using System;
using System.Runtime.Serialization;
using Vodovoz.Domain.Orders;
using Vodovoz.Domain.Client;

namespace Android
{
	[DataContract]
	public class OrderDTO
	{
		[DataMember]
		public int Id;

		[DataMember]
		public string Title;

		//Тип населенного пункта
		[DataMember]
		public string LocalityType;

		//Город
		[DataMember]
		public string City;

		//Корпус
		[DataMember]
		public string Housing;

		//Литера
		[DataMember]
		public string Letter;

		//Строение
		[DataMember]
		public string Structure;

		//Помещение
		[DataMember]
		public string Placement;

		//Этаж
		[DataMember]
		public int Floor;

		//Регион
		[DataMember]
		public string Region;

		//Район области
		[DataMember]
		public string CityDistrict;

		//Улица
		[DataMember]
		public string Street;

		//Район города
		[DataMember]
		public string StreetDistrict;

		//Номер дома
		[DataMember]
		public string Building;

		//Тип помещения
		[DataMember]
		public string RoomType;

		//Офис квартира
		[DataMember]
		public string Room;

		//Широта
		[DataMember]
		public decimal? Latitude;

		//Долгота
		[DataMember]
		public decimal? Longitude;

		//Комментарий к адресу
		[DataMember]
		public string DeliveryPointComment;

		//Контактное лицо
		[DataMember]
		public string Contact;

		//Телефон
		[DataMember]
		public string Phone;

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

		public OrderDTO (Order order)
		{
			Id = order.Id;
			Title = order.Title;
			LocalityType = order.DeliveryPoint.LocalityType.ToString(); //FIXME
			City = order.DeliveryPoint.City;
			Housing = order.DeliveryPoint.Housing;
			Letter = order.DeliveryPoint.Letter;
			Structure = order.DeliveryPoint.Structure;
			Placement = order.DeliveryPoint.Placement;
			Floor = order.DeliveryPoint.Floor;
			Region = order.DeliveryPoint.Region;
			CityDistrict = order.DeliveryPoint.CityDistrict;
			Street = order.DeliveryPoint.Street;
			StreetDistrict = order.DeliveryPoint.StreetDistrict;
			Building = order.DeliveryPoint.Building;
			RoomType = order.DeliveryPoint.RoomType.ToString(); //FIXME
			Room = order.DeliveryPoint.Room;
			Latitude = order.DeliveryPoint.Latitude;
			Longitude = order.DeliveryPoint.Longitude;
			DeliveryPointComment = order.DeliveryPoint.Comment;
			Contact = order.DeliveryPoint.Contact.FullName;
			Phone = order.DeliveryPoint.Phone;
			DeliverySchedule = order.DeliverySchedule.DeliveryTime;
			OrderStatus = order.OrderStatus.ToString(); //FIXME
			RouteListItemStatus = "Тут будет статус"; //FIXME
			OrderComment = order.Comment;
			Counterparty = order.Client.FullName;
		}
	}
}

