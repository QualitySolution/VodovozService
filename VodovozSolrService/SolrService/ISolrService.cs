using System;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace SolrImportService
{
	[ServiceContract]
	public interface ISolrService
	{
		[OperationContract]
		[WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json)]
		string RunDeltaImport(string solrEntityName);
	}
}
