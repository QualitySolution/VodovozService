using System;
using System.Collections.Generic;
using System.Threading;
using System.ServiceModel;

namespace Chat
{
	public class ChatCallbackObservable: IChatCallback
	{
		private static ChatCallbackObservable instance;

		private const int REFRESH_PERIOD = 180000;

		private IList<IChatCallbackObserver> observers;
		private IChatCallbackService proxy;
		private TimerCallback callback;
		private Timer timer;

		public static bool IsInitiated { get { return instance != null; } }

		public static void CreateInstance(int employeeId) 
		{
			instance = new ChatCallbackObservable (employeeId);
		}

		public static ChatCallbackObservable GetInstance() 
		{
			if (instance == null)
				throw new NullReferenceException("Попытка вызова метода ChatCallbackObservable.GetInstance()" +
					" без предварительной инициализации. Сначала требуется вызвать CreateInstance(int employeeId).");
			return instance;
		}

		private ChatCallbackObservable (int employeeId)
		{
			observers = new List<IChatCallbackObserver>();
			proxy = new DuplexChannelFactory<IChatCallbackService>(
				new InstanceContext(this), 
				new NetTcpBinding(), 
				"net.Tcp://vod-srv.qsolution.ru:9001/ChatCallbackService").CreateChannel();
			proxy.SubscribeForUpdates(employeeId);
			//Initiates connection keep alive query every 5 minutes.
			callback = new TimerCallback(Refresh);
			timer = new Timer(callback, null, 0, REFRESH_PERIOD);
		}

		public void AddObserver(IChatCallbackObserver observer) 
		{
			if (!observers.Contains(observer))
				observers.Add(observer);
		}

		public void RemoveObserver(IChatCallbackObserver observer) 
		{
			if (observers.Contains(observer))
				observers.Remove(observer);
		}

		private void Refresh(Object StateInfo)
		{
			proxy.KeepAlive();
		}

		#region IChatCallback implementation

		public void NotifyNewMessage(int chatId)
		{
			for (int i = 0; i < observers.Count; i++)
			{
				if (observers[i] == null)
				{
					observers.RemoveAt(i);
					i--;
					continue;
				}
				if (observers[i].ChatId == chatId)
					observers[i].HandleChatUpdate();
			}
		}

		#endregion
	}
}

