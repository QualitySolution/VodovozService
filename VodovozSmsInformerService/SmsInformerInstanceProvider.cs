using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Description;
using Vodovoz.EntityRepositories.SmsNotifications;
using Vodovoz.Services;
using System.Collections.ObjectModel;

namespace VodovozSmsInformerService
{
	public class SmsInformerInstanceProvider : IInstanceProvider
	{
		private readonly ISmsNotificationRepository smsNotificationRepository;
		private readonly ISmsNotificationServiceSettings smsNotificationServiceSettings;

		public SmsInformerInstanceProvider(ISmsNotificationRepository smsNotificationRepository, ISmsNotificationServiceSettings smsNotificationServiceSettings)
		{
			this.smsNotificationRepository = smsNotificationRepository ?? throw new ArgumentNullException(nameof(smsNotificationRepository));
			this.smsNotificationServiceSettings = smsNotificationServiceSettings ?? throw new ArgumentNullException(nameof(smsNotificationServiceSettings));
		}

		#region IInstanceProvider implementation

		public object GetInstance(InstanceContext instanceContext)
		{
			return new SmsInformerService(smsNotificationRepository, smsNotificationServiceSettings);
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
