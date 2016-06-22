using System;
using System.Net;
using System.Web.Script.Serialization;
using System.Text;
using System.IO;

namespace Chat
{
	public static class FCMHelper
	{
		private const string FCM_URI = "https://fcm.googleapis.com/fcm/send";

		private static string serverApiKey = String.Empty;
		private static string senderId = String.Empty;

		public static void Configure (string serverApiKey, string senderId)
		{ 
			FCMHelper.serverApiKey = serverApiKey;
			FCMHelper.senderId = senderId;
		}

		public static void SendMessage (string deviceId, string sender, string message)
		{
			WebRequest request = WebRequest.Create (FCM_URI);
			request.Method = "post";
			request.ContentType = "application/json";
			request.Headers.Add (string.Format ("Authorization: key={0}", serverApiKey));
			request.Headers.Add(string.Format("Sender: id={0}", senderId));

			var data = new {
				to = deviceId,
				message = new {
					from = sender,
					message = message
				}
			};
			var serializer = new JavaScriptSerializer ();
			var json = serializer.Serialize (data);
			Byte[] byteArray = Encoding.UTF8.GetBytes (json);
			request.ContentLength = byteArray.Length;
			using (Stream dataStream = request.GetRequestStream ()) {
				dataStream.Write (byteArray, 0, byteArray.Length);
				using (WebResponse response = request.GetResponse ())
				using (Stream dataStreamResponse = response.GetResponseStream ())
				using (StreamReader reader = new StreamReader (dataStreamResponse)) {
					String responseFromServer = reader.ReadToEnd ();
				}
			}
		}

	}
}

