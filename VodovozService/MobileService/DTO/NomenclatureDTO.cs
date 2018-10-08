using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using Gamma.Utilities;
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
		public string Image {
			get {
				return Images.FirstOrDefault();
			}
			set { }
		}

		[DataMember]
		public string CategoryName;

		[DataMember]
		public string CategoryId;

		[DataMember]
		public List<string> Images {
			get {
				return imagesIds.Select(x => MobileService.BaseUrl + $"/Catalog/Images/{x}.jpg").ToList();
			}
			set { }
		}

		public int[] imagesIds = new int[]{ };

		public NomenclatureDTO(Nomenclature nomenclature)
		{
			Id = nomenclature.Id;
			Name = nomenclature.OfficialName;
			Prices = nomenclature.NomenclaturePrice.Select(p => new NomenclaturePriceDTO(p)).ToList();

			if(nomenclature.MobileCatalog == MobileCatalog.Water) {
				CategoryName = MobileCatalog.Water.GetEnumTitle();
				CategoryId = MobileCatalog.Water.ToString();
			} else if(nomenclature.MobileCatalog != MobileCatalog.None) {
				var title = nomenclature.MobileCatalog.GetEnumTitle();
				CategoryName = title.Substring(title.IndexOf('.') + 1);
				var categoryId = nomenclature.MobileCatalog.ToString();
				CategoryId = categoryId.Substring(categoryId.IndexOf('_') + 1);
			}

		}
	}
}
