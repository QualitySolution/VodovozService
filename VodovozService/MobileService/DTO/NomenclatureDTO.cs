using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Vodovoz.Domain.Goods;

namespace Vodovoz.MobileService.DTO
{
	[DataContract]
	public class NomenclatureDTO 
	{
		[DataMember]
		public int Id;

		[DataMember]
		public string Name;

		[DataMember]
		public List<NomenclaturePriceDTO> Prices;

		[DataMember]
		public string Image;

		public NomenclatureDTO(Nomenclature nomenclature)
		{
			Id = nomenclature.Id;
			Name = nomenclature.OfficialName;
			Prices = nomenclature.NomenclaturePrice.Select(p => new NomenclaturePriceDTO(p)).ToList();
		}
	}
}
