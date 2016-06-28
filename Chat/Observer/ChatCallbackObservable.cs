using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using QSProjectsLib;
using ChatClass = Vodovoz.Domain.Chat.Chat;
using Vodovoz.Repository.Chat;
using QSOrmProject;
using Vodovoz.Domain.Employees;
using Vodovoz.Domain.Chat;

namespace Chat
{
	public class ChatCallbackObservable
	{
		private static ChatCallbackObservable instance;
		private const int REFRESH_INTERVAL = 30000;
		private int refreshInterval = REFRESH_INTERVAL;

		private IUnitOfWorkGeneric<Employee> employeeUoW;
		private IList<IChatCallbackObserver> observers;
		private TimerCallback callback;
		private Timer timer;

		private Dictionary<int, int> unreadedMessages;

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
			employeeUoW = UnitOfWorkFactory.CreateForRoot<Employee>(employeeId);
			unreadedMessages = ChatMessageRepository.GetLastChatMessages(employeeUoW, employeeUoW.Root);

			//Initiates new message check every 30 seconds.
			callback = new TimerCallback(Refresh);
			timer = new Timer(callback, null, 0, refreshInterval);
		}

		public void AddObserver(IChatCallbackObserver observer) 
		{
			if (!observers.Contains(observer))
			{
				observers.Add(observer);
				if (observer.RequestedRefreshInterval != null && observer.RequestedRefreshInterval < refreshInterval)
				{
					refreshInterval = (int)observer.RequestedRefreshInterval;
					timer.Change(0, refreshInterval);
				}
			}
		}

		public void RemoveObserver(IChatCallbackObserver observer) 
		{
			if (observers.Contains(observer))
				observers.Remove(observer);

			int interval = REFRESH_INTERVAL;
			foreach (var obs in observers)
			{
				if (obs.RequestedRefreshInterval != null && obs.RequestedRefreshInterval < interval)
					interval = (int)obs.RequestedRefreshInterval;
			}
			refreshInterval = interval;
			timer.Change(0, refreshInterval);
		}

		private void Refresh(Object StateInfo)
		{
			var tempUnreadedMessages = ChatMessageRepository.GetLastChatMessages(employeeUoW, employeeUoW.Root);
			foreach (var item in tempUnreadedMessages)
			{
				if (!unreadedMessages.ContainsKey(item.Key) || unreadedMessages[item.Key] != item.Value)
					NotifyNewMessage(item.Key);
			}
			unreadedMessages = tempUnreadedMessages;
		}
			
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
				if (observers[i].ChatId == null || observers[i].ChatId == chatId)
					observers[i].HandleChatUpdate();
			}
		}
	}
}

