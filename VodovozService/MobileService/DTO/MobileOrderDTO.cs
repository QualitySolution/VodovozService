using System.Runtime.Serialization;

namespace Vodovoz.MobileService.DTO
{
	[DataContract]
	public class MobileOrderDTO
	{
		[DataMember]
		public int OrderId { get; private set; }

		[DataMember]
		public string Imei { get; private set; }

		[DataMember]
		public decimal OrderSum { get; private set; }

		[DataMember]
		public string Created { get; private set; }

		public MobileOrderDTO(int id, string imei, decimal sum) {
			OrderId = id;
			Imei = imei;
			OrderSum = sum;
		}

		public MobileOrderDTO() { }
	}
}