using System;
namespace Chat
{
	public static class ChatMain
	{
		public static string ChatServer { get; set; }
		
		public static string ChatServiceUrl{
			get{
				return $"http://{ChatServer}/ChatService";
			}
		}
	}
}
