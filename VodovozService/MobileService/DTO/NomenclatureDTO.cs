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
		public int Count = 1;

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
				//Вес больше 1 килограма это 19л и 6л бутыли. Меньше 0.6л.
				Count = nomenclature.Weight > 1 ? 2 : 12;
			} else if(nomenclature.MobileCatalog != MobileCatalog.None) {
				var title = nomenclature.MobileCatalog.GetEnumTitle();
				CategoryName = title.Substring(title.IndexOf('.') + 1);
				var categoryId = nomenclature.MobileCatalog.ToString();
				CategoryId = categoryId.Substring(categoryId.IndexOf('_') + 1);
				if(Prices.Count > 0)
					Count = Prices.Min(p => p.MinCount);
			}

		}
	}
}
