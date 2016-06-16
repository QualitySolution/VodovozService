using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Collections.Generic;

namespace Android
{
	[ServiceContract]
	public interface IAndroidDriverService
	{
		[OperationContract]
		[WebInvoke (UriTemplate = "/Auth", BodyStyle = WebMessageBodyStyle.WrappedRequest)] 
		string Auth (string login, string password);

		[OperationContract]
		[WebInvoke (UriTemplate = "/CheckAuth", BodyStyle = WebMessageBodyStyle.WrappedRequest)] 
		bool CheckAuth (string authKey);

		[OperationContract]
		[WebInvoke (UriTemplate = "/GetRouteLists", BodyStyle = WebMessageBodyStyle.WrappedRequest)] 
		List<RouteListDTO> GetRouteLists (string authKey);

		[OperationContract]
		[WebInvoke (UriTemplate = "/GetRouteListOrders", BodyStyle = WebMessageBodyStyle.WrappedRequest)] 
		List<ShortOrderDTO> GetRouteListOrders (string authKey, int routeListId);

		[OperationContract]
		[WebInvoke (UriTemplate = "/GetOrderDetailed", BodyStyle = WebMessageBodyStyle.WrappedRequest)] 
		OrderDTO GetOrderDetailed (string authKey, int orderId);

		[OperationContract]
		[WebInvoke (UriTemplate = "/SendCoordinates", BodyStyle = WebMessageBodyStyle.WrappedRequest)] 
		bool SendCoordinates (string authKey, int trackId, TrackPointList TrackPointList);

		[OperationContract]
		[WebInvoke (UriTemplate = "/StartOrResumeTrack", BodyStyle = WebMessageBodyStyle.WrappedRequest)] 
		int? StartOrResumeTrack (string authKey, int routeListId);

		[OperationContract]
		[WebInvoke (UriTemplate = "/ChangeOrderStatus", BodyStyle = WebMessageBodyStyle.WrappedRequest)] 
		bool ChangeOrderStatus (string authKey, int orderId, string status);
	}
}

