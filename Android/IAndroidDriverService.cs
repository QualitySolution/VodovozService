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
		List<OrderDTO> GetRouteListOrders (string authKey, int routeListId);
	}
}

