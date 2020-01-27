using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using SmsSendInterface;

namespace VodovozInstantSmsService
{
	public class InstantSmsInformerInstanceProvider : IInstanceProvider
	{
		public InstantSmsInformerInstanceProvider(ServiceHost instantSmsServiceHost, ISmsSender smsSender)
		{
			this.instantSmsServiceHost = instantSmsServiceHost ?? throw new ArgumentNullException(nameof(instantSmsServiceHost));
			this.smsSender = smsSender ?? throw new ArgumentNullException(nameof(smsSender));
		}

		ServiceHost instantSmsServiceHost;
		ISmsSender smsSender;

		#region IInstanceProvider implementation

		public object GetInstance(InstanceContext instanceContext)
		{
			return new InstantSmsInformerService(instantSmsServiceHost, smsSender);
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
