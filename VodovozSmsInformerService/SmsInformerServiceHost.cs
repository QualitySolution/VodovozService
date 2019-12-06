using System;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;

namespace VodovozSmsInformerService
{
	public class SmsInformerServiceHost : WebServiceHost
	{
		private readonly SmsInformerInstanceProvider serviceStatusInstanceProvider;

		public SmsInformerServiceHost(SmsInformerInstanceProvider serviceStatusInstanceProvider) : base(typeof(SmsInformerService))
		{
			this.serviceStatusInstanceProvider = serviceStatusInstanceProvider ?? throw new ArgumentNullException(nameof(serviceStatusInstanceProvider));

			Description.Behaviors.Add(new SmsInformerServiceBehavior(serviceStatusInstanceProvider));
		}
	}
}
