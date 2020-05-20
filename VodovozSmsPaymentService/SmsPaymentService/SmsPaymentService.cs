using System;
using System.Linq;
using System.Net;
using InstantSmsService;
using Newtonsoft.Json;
using NLog;
using QS.DomainModel.UoW;
using Vodovoz.Domain;
using Vodovoz.Domain.Client;
using Vodovoz.Domain.Orders;

namespace SmsPaymentService
{
    public class SmsPaymentService : ISmsPaymentService
    {
        public SmsPaymentService(IPaymentWorker paymentWorker)
        {
            this.paymentWorker = paymentWorker ?? throw new ArgumentNullException(nameof(paymentWorker));
        }
        
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly IPaymentWorker paymentWorker;
        
        public string ReceivePayment(RequestBody body)
        {
            var externalId = body.ExternalId;
            var status = (SmsPaymentStatus)body.Status;
            var paidDate = DateTime.Parse(body.PaidDate);
            
            logger.Info($"Поступил запрос на изменения статуса платежа с параметрами externalId: {externalId}, status: {status} и paidDate: {paidDate}");

            var acceptedStatuses = new[] { SmsPaymentStatus.Paid, SmsPaymentStatus.Cancelled };
            if (externalId == 0 || !acceptedStatuses.Contains(status)) { 
                logger.Error($"Запрос на изменение статуса пришёл с неверным статусом (status: {status})");
                return JsonConvert.SerializeObject( new { status = HttpStatusCode.BadRequest });
            }
            try {
                using (IUnitOfWork uow = UnitOfWorkFactory.CreateWithoutRoot()) {
                    var payment = uow.Session.QueryOver<SmsPayment>().Where(x => x.ExternalId == externalId).Take(1).SingleOrDefault();
                    if (payment == null) {
                        logger.Error($"Запрос на изменение статуса платежа указывает на несуществующий платеж (externalId: {externalId}"); 
                        return JsonConvert.SerializeObject( new { status = HttpStatusCode.UnsupportedMediaType });
                    }
                    var oldStatus = payment.SmsPaymentStatus;
                    payment.SmsPaymentStatus = status;
                    if (status == SmsPaymentStatus.Paid)
                        payment.PaidDate = paidDate;
                    
                    uow.Save(payment);
                    uow.Commit();
                    
                    logger.Info($"Статус платежа № {payment.Id} изменён c {oldStatus}" + $" на {status}");
                }
            }
            catch (Exception ex) {
                logger.Error(ex, $"Ошибка при обработке поступившего платежа (externalId: {externalId}, status: {status})");
                return  JsonConvert.SerializeObject( new { status = HttpStatusCode.InternalServerError });
            }
            return JsonConvert.SerializeObject( new { status = HttpStatusCode.OK });
        }

        public ResultMessage SendPayment(int orderId, string phoneNumber)
        {
            logger.Error($"Поступил запрос на отправку платежа с данными orderId: {orderId}, phoneNumber: {phoneNumber}");
            ResultMessage resultMessage = new ResultMessage { MessageStatus = SmsMessageStatus.Ok };
            if (orderId <= 0) {
                resultMessage.ErrorDescription = "Неверное значение номера заказа";
                logger.Error("Запрос на отправку платежа пришёл с неверным значением номера заказа");
                return resultMessage;
            }
            if (String.IsNullOrWhiteSpace(phoneNumber)) {
                resultMessage.ErrorDescription = "Неверное значение номера телефона";
                logger.Error("Запрос на отправку платежа пришёл с неверным значение номера телефона");
                return resultMessage;
            }
            phoneNumber = phoneNumber.TrimStart('+').TrimStart('7').TrimStart('8');
            if (String.IsNullOrWhiteSpace(phoneNumber)
                || phoneNumber.Length == 0
                || phoneNumber.First() != '9'
                || phoneNumber.Length != 10) 
            {
                resultMessage.ErrorDescription = "Неверный формат номера телефона";
                logger.Error("Запрос на отправку платежа пришёл с неверным форматом номера телефона");
                return resultMessage;
            }
            phoneNumber = $"+7{phoneNumber}";
            try
            {
                using (IUnitOfWork uow = UnitOfWorkFactory.CreateWithoutRoot()) {
                    var order = uow.GetById<Order>(orderId);
                    if (order == null) {
                        resultMessage.ErrorDescription = $"Заказ с номером {orderId} не существующет в базе";
                        logger.Error( $"Запрос на отправку платежа пришёл со значением номера заказа, не существующем в базе (Id: {orderId})");
                        return resultMessage;
                    }
                    var paymentDto = new SmsPaymentDTO {
                        Recepient = order.Client.Name,
                        RecepientId = order.Client.Id,
                        PhoneNumber = phoneNumber,
                        PaymentStatus = SmsPaymentStatus.WaitingForPayment,
                        OrderId = order.Id,
                        PaymentCreationDate = DateTime.Now,
                        Amount = order.OrderTotalSum,
                        RecepientType = order.Client.PersonType
                    };
                    
                    var sendResponse = paymentWorker.SendPayment(paymentDto);
                    
                    if (sendResponse.HttpStatusCode == HttpStatusCode.OK) {
                        var payment = CreateNewSmsPayment(uow, paymentDto, sendResponse.ExternalId);
                        uow.Save(payment);
                        uow.Commit();
                        logger.Info($"Создан новый платеж с данными: Id: {payment.Id}, orderId: {payment.Order.Id}, phoneNumber: {payment.PhoneNumber}");
                    }
                    else {
                        resultMessage.ErrorDescription = $"Не получилось отправить платёж. Http код: {sendResponse}";
                        logger.Error(resultMessage.ErrorDescription, $"Не получилось отправить платёж.  Http код: {sendResponse}." +
                                                                     $" (orderId: {orderId}, phoneNumber: {phoneNumber})");
                        return resultMessage;
                    }
                }
            }
            catch(Exception ex) {
                resultMessage.ErrorDescription = $"Ошибка при отправке платежа. {ex.Message}";
                logger.Error(ex, $"Ошибка при отправке платежа (orderId: {orderId}, phoneNumber: {phoneNumber})");
            }
            return resultMessage;
        }

        public SmsPaymentStatus? RefreshPaymentStatus(int externalId)
        {
            logger.Error($"Поступил запрос на обновление статуса платежа с externalId: {externalId}");
            try {
                using (var uow = UnitOfWorkFactory.CreateWithoutRoot()) {
                    var payment = uow.Session.QueryOver<SmsPayment>()
                       .Where(x => x.ExternalId == externalId)
                       .Take(1)
                       .SingleOrDefault();
                    
                    if (payment == null) {
                        logger.Error($"Платеж с externalId: {externalId} не найден в базе");
                        return null;
                    }
                    var status = paymentWorker.GetPaymentStatus(externalId);
                    if (status == null)
                        return null;
                    
                    if (payment.SmsPaymentStatus != status) {
                        var oldStatus = payment.SmsPaymentStatus;
                        payment.SmsPaymentStatus = status.Value;
                        uow.Save(payment);
                        uow.Commit();
                        logger.Error($"Платеж с externalId: {externalId} сменил статус с {Enum.GetName(typeof(SmsPaymentStatus), oldStatus)} на {Enum.GetName(typeof(SmsPaymentStatus), status)}");
                    }
                    
                    return status;
                }
            }
            catch (Exception ex) {
                logger.Error(ex, $"Ошибка при обновлении статуса платежа externalId: {externalId}");
                return null;
            }
        }

        public bool ServiceStatus()
        {
            try {
                using (IUnitOfWork uow = UnitOfWorkFactory.CreateWithoutRoot()) {
                    uow.GetById<Order>(123);
                }
            }
            catch {
                return false;
            }
        
            return true;
        }
        
        private SmsPayment CreateNewSmsPayment(IUnitOfWork uow, SmsPaymentDTO dto, int externalId)
        {
            return new SmsPayment {
                ExternalId = externalId,
                Amount = dto.Amount,
                Order = uow.GetById<Order>(dto.OrderId),
                Recepient = uow.GetById<Counterparty>(dto.RecepientId),
                CreationDate = dto.PaymentCreationDate,
                PhoneNumber = dto.PhoneNumber,
                SmsPaymentStatus = SmsPaymentStatus.WaitingForPayment
            };
        }
    }

    public struct RequestBody
    {
        public int ExternalId { get; set; }
        public int Status { get; set; }
        public string PaidDate { get; set; }
    }
}