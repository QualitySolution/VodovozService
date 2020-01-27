using System;
using System.ServiceModel.Web;

namespace VodovozInstantSmsService
{
	public class InstantSmsInformerHost : WebServiceHost
	{
		public InstantSmsInformerHost(InstantSmsInformerInstanceProvider serviceStatusInstanceProvider) : base(typeof(InstantSmsInformerService))
		{
			Description.Behaviors.Add(new InstantSmsInformerBehavior(serviceStatusInstanceProvider));
		}
	}
}
