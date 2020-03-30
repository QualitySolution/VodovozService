using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using SolrImportService;

namespace VodovozSolrService
{
	public class SolrInstanceProvider : IInstanceProvider
	{
		private readonly SolrService solrService;

		public SolrInstanceProvider(SolrService solrService)
		{
			this.solrService = solrService ?? throw new ArgumentNullException(nameof(solrService));
		}

		#region IInstanceProvider implementation

		public object GetInstance(InstanceContext instanceContext)
		{
			return solrService;
		}

		public object GetInstance(InstanceContext instanceContext, Message message)
		{
			return GetInstance(instanceContext);
		}

		public void ReleaseInstance(InstanceContext instanceContext, object instance)
		{
		}

		#endregion IInstanceProvider implementation
	}
}
