using System.Collections;
using NUnit.Framework;
using MobileAppService = Vodovoz.MobileService.MobileService;
using Vodovoz.MobileService.DTO;

namespace VodovozServiceTests.MobileService
{
	[TestFixture]
	public class MobileServiceTests
	{
		#region Заглушка с номером заказа

		static IEnumerable ParametersForNewIdAndResults()
		{
			//провалы
			yield return new object[] { "123456789012345", 0m, -1 };//не положительная сумма
			yield return new object[] { "123e4567-e89b-12d3-v456-426655440000", 10m, -1 };//не HEX uuid
			yield return new object[] { "123e4567+e89b-12d3-a456-426655440000", 20m, -1 };//+ в HEX uuid
			yield return new object[] { "00000000-0000-0000-0000-000000000000", 403m, -1 };//nil uuid
			yield return new object[] { "123e4567-e89b-12d3-a456-426655440000-1", 242m, -1 };//длинный HEX uuid 

			//удачные исходы
			yield return new object[] { "123456789012345", 110m, 555 };//положительная сумма, короткий uuid
			yield return new object[] { "123e4567e89b12d3a456426655440000", 1540m, 555 };//HEX uuid без "-"
			yield return new object[] { "123e4567-e89b-12d3-a456-426655440000", 413m, 555 };//8-4-4-4-12 HEX uuid
		}

		[TestCaseSource(nameof(ParametersForNewIdAndResults))]
		[Test(Description = "Тестирование сценариев результирующего OrderId, с входными параметрами из ParametersForNewIdAndResults()")]
		public void Order_TestScenario(string uuid, decimal sum, int result)
		{
			//arrange
			var ms = new MobileAppService {
				OrderTestGap = mobOrder => 555
			};
			var mo = new MobileOrderDTO(0, uuid, sum);

			//act
			int res = ms.Order(mo);

			//assert
			Assert.That(res, Is.EqualTo(result));
		}

		#endregion
	}
}
