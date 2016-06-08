using System;
using Vodovoz.Domain.Logistic;
using System.Runtime.Serialization;

namespace Android
{
	[DataContract]
	public class RouteListDTO
	{
		[DataMember]
		public int Id;

		[DataMember]
		public string Status;

		[DataMember]
		public string Forwarder;

		[DataMember]
		public DateTime Date;

		[DataMember]
		public string DeliveryShift;

		public RouteListDTO (RouteList routeList)
		{
			Id = routeList.Id;
			Status = routeList.Status.ToString(); //FIXME
			Forwarder = routeList.Forwarder.FullName;
			Date = routeList.Date;
			DeliveryShift = routeList.Shift.Name;
		}
	}
}

