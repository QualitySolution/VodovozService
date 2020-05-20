using System.Net;

namespace SmsPaymentService
{
    public class SendResponse
    {
        public HttpStatusCode HttpStatusCode { get; set; }
        
        /// <summary>
        /// ID платежа во внешней базе
        /// </summary>
        public int ExternalId { get; set; }
    }
}