using System.Collections;
using NUnit.Framework;
using MobileAppService = Vodovoz.MobileService.MobileService;
using Vodovoz.MobileService.DTO;

namespace VodovozServiceTests.MobileService
{
	[TestFixture()]
	public class MobileServiceTests
	{
		#region Заглушка с номером заказа

		static IEnumerable ParametersForNewIdAndResults()
		{
			yield return new object[] { "123456789012345", 0m, -1 };//не положительная сумма
			yield return new object[] { "12345678901234", 10m, -1 };//слишком короткий imei
			yield return new object[] { "1234567890123456", 20m, -1 };//слишком длинный imei
			yield return new object[] { "123456789012err", 110m, -1 };//в imei недопустимые символы
			yield return new object[] { "-12345678901234", 1m, -1 };//в imei недопустимые символы
		}

		[TestCaseSource(nameof(ParametersForNewIdAndResults))]
		[Test(Description = "Тестирование негативных сценариев результирующего OrderId, с входными параметрами из ParametersForNewIdAndResults()")]
		public void Order_TestOfNegativeScenario(string imei, decimal sum, int result)
		{
			//arrange
			var ms = new MobileAppService();
			var mo = new MobileOrderDTO(0, imei, sum);
			//act
			int res = ms.Order(mo);
			//assert
			Assert.That(res, Is.EqualTo(result));
		}

		[Ignore("Не получается, т.к. есть подключение к БД. Есть вариант через внедрение зависимостей, но пока не осилил. Разбираюсь.")]
		[Test(Description = "Тестирование позитивного сценария результирующего OrderId")]
		public void Order_TestOfPositiveScenario()
		{
			//arrange
			var ms = new MobileAppService();
			var mo = new MobileOrderDTO(0, "123456789012345", 11);
			//act
			int res = ms.Order(mo);
			//assert
			Assert.That(res, Is.GreaterThan(0));
		}

		#endregion
	}
}
