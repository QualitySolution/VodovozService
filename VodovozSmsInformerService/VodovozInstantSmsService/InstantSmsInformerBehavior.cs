using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace VodovozInstantSmsService
{
	public class InstantSmsInformerBehavior : IServiceBehavior
	{
		private readonly InstantSmsInformerInstanceProvider instantSmsInformerInstanceProvider;

		public InstantSmsInformerBehavior(InstantSmsInformerInstanceProvider instantsmsInformerInstanceProvider)
		{
			this.instantSmsInformerInstanceProvider = instantsmsInformerInstanceProvider ?? throw new ArgumentNullException(nameof(instantsmsInformerInstanceProvider));
		}

		public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
		{
		}

		public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
		{
			foreach(ChannelDispatcherBase cdb in serviceHostBase.ChannelDispatchers) {
				ChannelDispatcher cd = cdb as ChannelDispatcher;
				if(cd != null) {
					foreach(EndpointDispatcher ed in cd.Endpoints) {
						ed.DispatchRuntime.InstanceProvider = instantSmsInformerInstanceProvider;
					}
				}
			}
		}

		public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
		{
		}
	}
}
