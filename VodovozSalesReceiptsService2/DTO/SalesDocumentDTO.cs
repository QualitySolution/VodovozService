using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Vodovoz.Domain.Orders;

namespace VodovozSalesReceiptsService.DTO
{
	[DataContract]
	public class SalesDocumentDTO
	{
		public SalesDocumentDTO() { }
		public SalesDocumentDTO(Order order)
		{
			DocNum = Id = string.Concat("order_", order.Id);
			Email = order.GetContact();
			CashierName = order.Author.ShortName;
			foreach(var item in order.OrderItems) {
				InventPositions = new List<InventPositionDTO> {
					new InventPositionDTO {
						Name = item.Nomenclature.OfficialName,
						PriceWithoutDiscount = item.Price,
						Quantity = item.Count,
						DiscSum = item.DiscountMoney,
						Vat = item.Nomenclature.VAT
					}
				};
			}
			MoneyPositions = new List<MoneyPositionDTO> {
				new MoneyPositionDTO(order.OrderItems.Sum(i => i.Price * i.Count - i.DiscountMoney))
			};
		}

		[DataMember(IsRequired = true)]
		string id;
		public string Id {
			get => id;
			set => id = value;
		}

		[DataMember(IsRequired = true)]
		string docNum;
		public string DocNum {
			get => docNum;
			set => docNum = value;
		}

		[DataMember(IsRequired = true)]
		readonly string docType = "SALE";

		[DataMember(IsRequired = true)]
		readonly string checkoutDateTime = DateTime.Now.ToString("O");

		[DataMember(IsRequired = true)]
		string email;
		public string Email {
			get => email;
			set => email = value;
		}

		[DataMember]
		bool printReceipt;
		public bool PrintReceipt {
			get => printReceipt;
			set => printReceipt = value;
		}

		[DataMember]
		string cashierName;
		public string CashierName {
			get => cashierName;
			set => cashierName = value;
		}

		[DataMember]
		string cashierPosition;
		public string CashierPosition {
			get => cashierPosition;
			set => cashierPosition = value;
		}

		[DataMember]
		string responseURL;
		public string ResponseURL {
			get => responseURL;
			set => responseURL = value;
		}

		[DataMember]
		string taxMode;// = "ENVD";
		public string TaxMode {
			get => taxMode;
			set => taxMode = value;
		}

		[DataMember(IsRequired = true)]
		List<InventPositionDTO> inventPositions;
		public List<InventPositionDTO> InventPositions {
			get => inventPositions;
			set => inventPositions = value;
		}

		[DataMember(IsRequired = true)]
		List<MoneyPositionDTO> moneyPositions;
		public List<MoneyPositionDTO> MoneyPositions {
			get => moneyPositions;
			set => moneyPositions = value;
		}

		public bool IsValid {
			get {
				return Id != null
					&& DocNum != null
					&& Email != null
					&& InventPositions != null
					&& MoneyPositions != null
					&& InventPositions.Any()
					&& MoneyPositions.Any();
			}
		}
	}
}