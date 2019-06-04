using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Web;
using System.Text;
using NLog;
using QS.DomainModel.UoW;
using Vodovoz.Domain.Goods;
using Vodovoz.Domain.Payments;
using Vodovoz.MobileService.DTO;
using Vodovoz.Repository;

namespace Vodovoz.MobileService
{
#if DEBUG
	[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
#endif
	public class MobileService : IMobileService
	{
		static Logger logger = LogManager.GetCurrentClassLogger();
		public static string BaseUrl { get; set; }

		public MobileService()
		{
		}

		public List<NomenclatureDTO> GetGoods(CatalogType type)
		{
			using(var uow = UnitOfWorkFactory.CreateWithoutRoot($"[MB]Получение каталога товаров {type}")) {
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

			using(var uow = UnitOfWorkFactory.CreateWithoutRoot($"[MB]Получение картинки {id}")) {
				var image = uow.GetById<NomenclatureImage>(id);
				if(image == null) {
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

		public Func<MobileOrderDTO, int> OrderTestGap;
		public int Order(MobileOrderDTO mobileOrder)
		{
			if(!IsMobileOrderDTOValid(mobileOrder)) {
				logger.Error(string.Format("[MB]Не корректный DTO мобильного заказа."));
				if(WebOperationContext.Current?.OutgoingResponse != null)
					WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
				return -1;
			}

			if(mobileOrder.OrderId > 0) {
				//реализовать для изменения заказа
				logger.Error(string.Format("[MB]Запрос на измение мобильного заказа '{0}'. Пока не реализованно.", mobileOrder.OrderId));
				if(WebOperationContext.Current?.OutgoingResponse != null)
					WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotImplemented;
				return -1;
			}

			var res = OrderTestGap == null ? SaveAndGetId(mobileOrder) : OrderTestGap(mobileOrder);

			if(res > 0) {
				if(WebOperationContext.Current?.OutgoingResponse != null)
					WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
				return res;
			}

			logger.Error(string.Format("[MB]Не корректный DTO мобильного заказа."));
			if(WebOperationContext.Current?.OutgoingResponse != null)
				WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
			return -1;
		}

		bool IsMobileOrderDTOValid(MobileOrderDTO mobileOrder)
		{
			if(mobileOrder == null) {
				logger.Error("[MB]Отсутсвует заказ");
				return false;
			}

			if(!mobileOrder.IsOrderSumValid()) {
				logger.Error(string.Format("[MB]Неправильная сумма заказа: \"{0}\"", mobileOrder.OrderSum));
				return false;
			}

			if(!mobileOrder.IsUuidValid()) {
				logger.Error(string.Format("[MB]Неправильный Uuid: \"{0}\"", mobileOrder.UuidRaw));
				return false;
			}
			return true;
		}

		int SaveAndGetId(MobileOrderDTO mobileOrder)
		{
			int resId = -1;
			if(mobileOrder.IsUuidValid() && mobileOrder.IsOrderSumValid()) {
				using(var uow = UnitOfWorkFactory.CreateWithNewRoot<OrderIdProviderForMobileApp>($"[MB]Регистрация заказа для '{mobileOrder.GetUuid()}' на сумму '{mobileOrder.OrderSum}'")) {
					uow.Root.Uuid = mobileOrder.GetUuid();
					uow.Root.OrderSum = mobileOrder.OrderSum;
					try {
						uow.Save();
					}
					catch(Exception ex) {
						logger.Error(string.Format("[MB]Ошибка при сохранении: {0}", ex.Message));
						throw ex;
					}
					resId = uow.Root.Id;
				}
			}
			return resId;
		}
	}
}