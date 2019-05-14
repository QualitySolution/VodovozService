using System.Collections;
using NSubstitute;
using NUnit.Framework;
using Vodovoz.Domain.Goods;
using Vodovoz.MobileService.DTO;

namespace VodovozServiceTests.MobileService.DTO
{
	[TestFixture]
	public class NomenclatureDTOTests
	{
		#region Номенклатуры
		static IEnumerable WeightVolumeForWaterNomenclaturesAndResult()
		{
			yield return new object[] { 0.5, TareVolume.Vol600ml, MobileCatalog.Water, 12 };
			yield return new object[] { 6, TareVolume.Vol6L, MobileCatalog.Water, 2 };
			yield return new object[] { 20, TareVolume.Vol19L, MobileCatalog.Water, 2 };
		}

		[TestCaseSource(nameof(WeightVolumeForWaterNomenclaturesAndResult))]
		[Test(Description = "Создание и проверка NomenclatureDTO из Номенклатуры со свойствами из списка WeightVolumeForWaterNomenclaturesAndResult()")]
		public void NomenclatureDTO_CreatingThenCheckingOfNomenclatureDTOByNomenclatureWithPropertiesFromWeightVolumeForWaterNomenclaturesAndResultList(double weight, TareVolume vol, MobileCatalog cat, int result)
		{
			//Arrange
			Nomenclature nomenclatureMock = Substitute.For<Nomenclature>();
			nomenclatureMock.Weight.Returns(weight);
			nomenclatureMock.TareVolume.Returns(vol);
			nomenclatureMock.MobileCatalog.Returns(cat);

			//Act
			NomenclatureDTO nomenclatureDTO0 = new NomenclatureDTO(nomenclatureMock);

			//Assert
			Assert.That(nomenclatureDTO0.Count, Is.EqualTo(result));
		}

		[Test(Description = "Создание и проверка NomenclatureDTO из Номенклатуры для чая либо кофе и без цен. Возвращает кол-во 1.")]
		public void NomenclatureDTO_CreatingThenCheckingOfNomenclatureDTOForCoffeeTeaWithNoPrices_ReturnsOne()
		{
			//Arrange
			Nomenclature nomenclatureMock = Substitute.For<Nomenclature>();
			nomenclatureMock.Weight.Returns(0);
			nomenclatureMock.TareVolume.Returns(x => null);
			nomenclatureMock.MobileCatalog.Returns(MobileCatalog.Goods_CoffeeTea);

			//Act
			NomenclatureDTO nomenclatureDTO0 = new NomenclatureDTO(nomenclatureMock);

			//Assert
			Assert.That(nomenclatureDTO0.Count, Is.EqualTo(1));
		}

		[Test(Description = "Создание и проверка NomenclatureDTO из Номенклатуры для оборудования с ценами. Возвращает самое маеньшее минимальное кол-во из цен.")]
		public void NomenclatureDTO_CreatingThenCheckingOfNomenclatureDTOForEquipmentWithPrices_ReturnsMinimalCountFromPrices()
		{
			//Arrange
			NomenclaturePrice nomenclaturePriceMock0 = Substitute.For<NomenclaturePrice>();
			nomenclaturePriceMock0.MinCount.Returns(99);
			NomenclaturePrice nomenclaturePriceMock1 = Substitute.For<NomenclaturePrice>();
			nomenclaturePriceMock1.MinCount.Returns(17);
			NomenclaturePrice nomenclaturePriceMock2 = Substitute.For<NomenclaturePrice>();
			nomenclaturePriceMock2.MinCount.Returns(51);

			Nomenclature nomenclatureMock = Substitute.For<Nomenclature>();
			nomenclatureMock.Weight.Returns(2);
			nomenclatureMock.TareVolume.Returns(x => null);
			nomenclatureMock.MobileCatalog.Returns(MobileCatalog.Goods_Equipment);
			nomenclatureMock.NomenclaturePrice.Returns(new[] { nomenclaturePriceMock0, nomenclaturePriceMock1, nomenclaturePriceMock2 });

			//Act
			NomenclatureDTO nomenclatureDTO0 = new NomenclatureDTO(nomenclatureMock);

			//Assert
			Assert.That(nomenclatureDTO0.Count, Is.EqualTo(17));
		}

		#endregion Номенклатуры
	}
}
