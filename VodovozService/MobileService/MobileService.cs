using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using QS.DomainModel.UoW;
using Vodovoz.Domain.Goods;
using Vodovoz.MobileService.DTO;
using Vodovoz.Repository;

namespace Vodovoz.MobileService
{
#if DEBUG
	[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
	#endif
	public class MobileService : IMobileService
	{
		public static string BaseUrl;

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
				if(type == CatalogType.Water)
					list = list.OrderByDescending(n => n.Weight)
						.ThenBy(n => n.NomenclaturePrice.Any() ? n.NomenclaturePrice.Max(p => p.Price) : 0)
						.ToList();
				else
					list = list.OrderBy(n => (int)n.MobileCatalog)
						.ThenBy(n => n.NomenclaturePrice.Any() ? n.NomenclaturePrice.Max(p => p.Price) : 0)
						.ToList();

				var listDto = list.Select(n => new NomenclatureDTO(n)).ToList();

				var imageIds = NomenclatureRepository.GetNomenclatureImagesIds(uow, list.Select(x => x.Id).ToArray());
				listDto.Where(dto => imageIds.ContainsKey(dto.Id))
					.ToList()
					.ForEach(dto => dto.imagesIds = imageIds[dto.Id]);
				return listDto;
			}
		}

		public Stream GetImage(string filename)
		{		
			if(!filename.EndsWith(".jpg")) {
				WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
				return ReturnErrorInStream("Support only .jpg images.");
			}

			int id;
			string number = filename.Substring(0, filename.Length - 4);
			Console.WriteLine(number);
			if(!int.TryParse(number, out id)) {
				Console.WriteLine(id);
				WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
				return ReturnErrorInStream($"Can't parse {number} as image id.");
			}

			using (var uow = UnitOfWorkFactory.CreateWithoutRoot())
			{
				var image = uow.GetById<NomenclatureImage>(id);
				if(image == null)
			    {
					WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotFound;
					return ReturnErrorInStream($"Nomenclature Image with id:{id} not found");
				}

				using(MemoryStream ms = new MemoryStream(image.Image)) {
					WebOperationContext.Current.OutgoingResponse.ContentType = "image/jpeg";
					WebOperationContext.Current.OutgoingResponse.Headers.Add("Content-disposition", $"inline; filename={id}.jpg");
					ms.Position = 0;
					return ms;
				}
			}
        }

		Stream ReturnErrorInStream(string error)
		{
			WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain";
			return new MemoryStream(Encoding.UTF8.GetBytes(error));
		}
	}
}
