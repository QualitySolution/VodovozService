using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using Vodovoz.Domain.WageCalculation.CalculationServices.RouteList;
using Android;

namespace VodovozAndroidDriverService
{
	public class AndroidDriverServiceInstanceProvider : IInstanceProvider
	{
		private readonly WageCalculationServiceFactory wageCalculationServiceFactory;

		public AndroidDriverServiceInstanceProvider(WageCalculationServiceFactory wageCalculationServiceFactory)
		{
			this.wageCalculationServiceFactory = wageCalculationServiceFactory ?? throw new ArgumentNullException(nameof(wageCalculationServiceFactory));
		}

		#region IInstanceProvider implementation

		public object GetInstance(InstanceContext instanceContext)
		{
			return new AndroidDriverService(wageCalculationServiceFactory);
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
