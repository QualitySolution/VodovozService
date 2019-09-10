﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NLog;
using QS.DomainModel.UoW;
using Vodovoz.Domain.Orders;
using Vodovoz.EntityRepositories.Orders;
using VodovozSalesReceiptsService.DTO;

namespace VodovozSalesReceiptsService
{
	public static class Fiscalization
	{
		static string baseAddress;
		static HttpClient httpClient;
		static Logger logger = LogManager.GetCurrentClassLogger();

		public static async Task RunAsync(string baseAddress, AuthenticationHeaderValue authentication)
		{
			if(httpClient == null) {
				httpClient = new HttpClient();
				Fiscalization.baseAddress = baseAddress;
				httpClient.BaseAddress = new Uri(baseAddress);
				httpClient.DefaultRequestHeaders.Accept.Clear();
				httpClient.DefaultRequestHeaders.Authorization = authentication;
				httpClient.DefaultRequestHeaders.Accept.Add(
					new MediaTypeWithQualityHeaderValue("application/json")
				);
			}
			logger.Info(string.Format("Авторизация и проверка фискального регистратора..."));
			FinscalizatorStatusResponseDTO response = await GetSatusAsync("fn/v1/status");

			if(response != null) {
				switch(response.Status) {
					case FiscalRegistratorStatus.Associated:
						logger.Warn("Клиент успешно связан с розничной точкой, но касса еще ни разу не вышла на связь и не сообщила свое состояние.");
						return;
					case FiscalRegistratorStatus.Failed:
						logger.Warn("Проблемы получения статуса фискального накопителя. Этот статус не препятствует добавлению документов для фискализации. Все документы будут добавлены в очередь на сервере и дождутся момента когда касса будет в состоянии их фискализировать.");
						break;
					case FiscalRegistratorStatus.Ready:
						logger.Info("Соединение с фискальным накопителем установлено и его состояние позволяет фискализировать чеки.");
						break;
					default:
						logger.Warn(string.Format("Провал с сообщением: \"{0}\".", response.Message));
						return;
				}
			} else {
				logger.Warn("Провал. Нет ответа от сервиса.");
				return;
			}

			logger.Info("Подготовка документа к отправке на сервер фискализации...");

			using(IUnitOfWork uow = UnitOfWorkFactory.CreateWithoutRoot("[Fisk] Получение списка подходящих новых заказов и не отправленных чеков...")) {
				var orderIds = GetShippedOrderIds(uow);
				foreach(var oId in orderIds) {
					var o = uow.GetById<Order>(oId);
					var preparedReceipt = uow.Session.QueryOver<CashReceipt>()
											 .Where(r => r.Order.Id == oId)
											 .Take(1)
											 .List()
											 .FirstOrDefault()
											 ;

					if(preparedReceipt != null && !preparedReceipt.Sent) {
						await SendSalesDocumentAsync(preparedReceipt, new SalesDocumentDTO(o));
						uow.Save(preparedReceipt);
						uow.Commit();
						continue;
					}

					if(preparedReceipt == null) {
						var receipt = new CashReceipt { Order = o };
						await SendSalesDocumentAsync(receipt, new SalesDocumentDTO(o));
						uow.Save(receipt);
						uow.Commit();
						continue;
					}
				}
			}

			/*var doc = new SalesDocumentDTO {
				Id = "order000010",
				DocNum = "order000010",
				Email = "+79217893945",
				PrintReceipt = true,
				CashierName = "opopve?"
				,
				TaxMode = null
			};
			doc.InventPositions = new List<InventPositionDTO> {
				new InventPositionDTO {
					Name = "NomenTest01",
					PriceWithoutDiscount = 0.15m,
					Quantity = 2,
					DiscSum = .2m,
					Vat = VAT.Vat20
				},
				new InventPositionDTO {
					Name = "NomenTest02",
					PriceWithoutDiscount = 0.1m,
					Quantity = 1,
					DiscSum = .1m,
					Vat = VAT.No
				}
			};
			doc.MoneyPositions = new List<MoneyPositionDTO> {
				new MoneyPositionDTO(.1m)
			};*/


			/*throw new Exception();

			logger.Info("Отправка документа на сервер фискализации...");
			var httpCode = await PostSalesDocumentAsync(doc);
			switch(httpCode) {
				case HttpStatusCode.OK:
					logger.Info("Документ успешно отправлен на сервер фискализации.");
					break;
				default:
					logger.Warn(string.Format("Документ не был отправлен на сервер фискализации. Http код - {0} ({1}).", (int)httpCode, httpCode));
					break;
			}*/
		}

		static async Task SendSalesDocumentAsync(CashReceipt preparedReceipt, SalesDocumentDTO doc)
		{
			if(doc.IsValid) {
				logger.Info("Отправка документа на сервер фискализации...");
				var httpCode = await PostSalesDocumentAsync(doc);
				switch(httpCode) {
					case HttpStatusCode.OK:
						logger.Info("Документ успешно отправлен на сервер фискализации.");
						preparedReceipt.Sent = true;
						break;
					default:
						logger.Warn(string.Format("Документ не был отправлен на сервер фискализации. Http код - {0} ({1}).", (int)httpCode, httpCode));
						preparedReceipt.Sent = false;
						break;
				}
				preparedReceipt.HttpCode = (int)httpCode;
			} else {
				preparedReceipt.HttpCode = -1;
				preparedReceipt.Sent = false;
			}
		}

		static int[] GetShippedOrderIds(IUnitOfWork uow)
		{
			int[] orderIds = null;
			orderIds = OrderSingletonRepository.GetInstance()
											   .GetShippeIdsStartingFromDate(
													uow,
													Vodovoz.Domain.Client.PaymentType.cash,
													DateTime.Today.AddDays(-1)
												);

			return orderIds;
		}

		//static IEnumerable<CashReceipt> GetNotSentReceipts(IUnitOfWork uow)
		//{
		//	var result = uow.Session.QueryOver<CashReceipt>()
		//					.Where(r => !r.Sent)
		//					.List()
		//					;
		//	return result;
		//}

		static async Task<FinscalizatorStatusResponseDTO> GetSatusAsync(string path)
		{
			FinscalizatorStatusResponseDTO statusResponse = null;
			HttpResponseMessage response = await httpClient.GetAsync(path);
			if(response.IsSuccessStatusCode)
				statusResponse = await response.Content.ReadAsAsync<FinscalizatorStatusResponseDTO>();

			return statusResponse;
		}

		static async Task<HttpStatusCode> PostSalesDocumentAsync(SalesDocumentDTO order)
		{
			HttpResponseMessage response = await httpClient.PostAsJsonAsync(baseAddress + "/fn/v1/doc", order);
			return response.StatusCode;
		}
	}
}