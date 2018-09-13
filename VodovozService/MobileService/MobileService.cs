using System;
using System.Collections.Generic;
using System.Linq;
using QSOrmProject;
using Vodovoz.Domain.Goods;
using Vodovoz.MobileService.DTO;
using Vodovoz.Repository;

namespace Vodovoz.MobileService
{
	public class MobileService : IMobileService
	{

		public MobileService()
		{
		}

		public List<NomenclatureDTO> GetGoods(CatalogType type)
		{
			using(var uow = UnitOfWorkFactory.CreateWithoutRoot())
			{
				var types = Enum.GetValues(typeof(MobileCatalog))
				                .Cast<MobileCatalog>()
				                .Where(x => x.ToString().StartsWith(type.ToString()))
				                .ToArray();

				var list = NomenclatureRepository.GetNomenclatureWithPriceForMobileApp(uow, types);
				return list.Select(n => new NomenclatureDTO(n)).ToList();
			}
		}

		public void GetImage(int id)
		{
			throw new NotImplementedException();
		}
	}
}
