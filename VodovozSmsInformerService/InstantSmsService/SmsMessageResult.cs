using System.Runtime.Serialization;

namespace InstantSmsService
{
	[DataContract]
	public class SmsMessageResult
	{
		[DataMember]
		private string errorDescription;
		[DataMember]
		public string ErrorDescription { 
		get => errorDescription;
		set {
			MessageStatus = SmsMessageStatus.Error;
			errorDescription = value;
			}
		}

		[DataMember]
		public SmsMessageStatus MessageStatus { get; set; }
	}

	public enum SmsMessageStatus
	{
		Ok,
		Error
	}
}
