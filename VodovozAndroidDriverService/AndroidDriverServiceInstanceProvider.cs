using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using Vodovoz.Domain.WageCalculation.CalculationServices.RouteList;
using Android;
using Vodovoz.Services;

namespace VodovozAndroidDriverService
{
	public class AndroidDriverServiceInstanceProvider : IInstanceProvider
	{
		private readonly WageCalculationServiceFactory wageCalculationServiceFactory;
		private readonly IDriverServiceParametersProvider parameters;

		public AndroidDriverServiceInstanceProvider(WageCalculationServiceFactory wageCalculationServiceFactory, IDriverServiceParametersProvider parameters)
		{
			this.wageCalculationServiceFactory = wageCalculationServiceFactory ?? throw new ArgumentNullException(nameof(wageCalculationServiceFactory));
			this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
		}

		#region IInstanceProvider implementation

		public object GetInstance(InstanceContext instanceContext)
		{
			return new AndroidDriverService(wageCalculationServiceFactory, parameters);
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
