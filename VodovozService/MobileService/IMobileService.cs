using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Collections.Generic;
using Vodovoz.MobileService.DTO;

namespace Vodovoz.MobileService
{
	[ServiceContract]
	public interface IMobileService
	{
		[OperationContract]
		[WebGet(UriTemplate = "/Catalog/{type}/", ResponseFormat = WebMessageFormat.Json)]
		List<NomenclatureDTO> GetGoods(CatalogType type);

		[OperationContract]
		void GetImage(int id);
	}
}
