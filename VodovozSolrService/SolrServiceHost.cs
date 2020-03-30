using System;
using System.ServiceModel.Web;
using SolrImportService;

namespace VodovozSolrService
{
	public class SolrServiceHost : WebServiceHost
	{
		public SolrServiceHost(SolrInstanceProvider solrInstanceProvider) : base(typeof(SolrService))
		{
			if(solrInstanceProvider == null) {
				throw new ArgumentNullException(nameof(solrInstanceProvider));
			}

			Description.Behaviors.Add(new SolrServiceBehavior(solrInstanceProvider));
		}
	}
}
