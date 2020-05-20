using System.ServiceModel;
using System.ServiceModel.Web;
using InstantSmsService;
using Vodovoz.Domain;

namespace SmsPaymentService
{
    [ServiceContract]
    public interface ISmsPaymentService
    {
        /// <summary>
        /// Меняет статус платежа и время оплаты
        /// </summary>
        [OperationContract, WebInvoke(RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        string ReceivePayment(RequestBody body);
        
        /// <summary>
        /// Формирует и отправляет платеж
        /// </summary>
        /// <param name="orderId">Номер заказа</param>
        /// <param name="phoneNumber">Номер телефона клиента</param>
        [OperationContract, WebGet(ResponseFormat = WebMessageFormat.Json)]
        ResultMessage SendPayment(int orderId, string phoneNumber);
        
        /// <summary>
        /// Записывает в базу и возвращает актуальный статус платежа
        /// </summary>
        /// <param name="externalId">Id платежа во внешней базе</param>
        [OperationContract, WebGet(ResponseFormat = WebMessageFormat.Json)]
        SmsPaymentStatus? RefreshPaymentStatus(int externalId);

        
        [OperationContract, WebGet(ResponseFormat = WebMessageFormat.Json)]
        bool ServiceStatus();
    }
}