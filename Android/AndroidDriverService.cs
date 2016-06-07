using System;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Android
{
	public class AndroidDriverService : IAndroidDriverService
	{
		#region IAndroidDriverService implementation

		public string Auth (string login, string password)
		{
			//TODO: Replace with real logic
			Console.WriteLine("Auth called with args:\nlogin: {0}\npassword: {1}", login, password);
			var hash = (new SHA1Managed()).ComputeHash(Encoding.UTF8.GetBytes("123"));
			string str = string.Join("", hash.Select(b => b.ToString("x2")).ToArray());
			if (login == "vad" && password == str)
				return "74d5607f28d9051a5dc43h2jfe7dc8eb";
			else
				return null;
		}

		public bool CheckAuth (string authKey)
		{
			//TODO: Replace with real logic
			Console.WriteLine("CheckAuth called with args:\nauthKey: {0}", authKey);
			if (authKey == "74d5607f28d9051a5dc43h2jfe7dc8eb")
				return true;
			return false;
		}

		public string GetRouteLists (string authKey)
		{
			//TODO: Replace with real logic
			Console.WriteLine("GetRouteLists called with args:\nauthKey: {0}", authKey);
			if (!CheckAuth (authKey))
				return null;
			List<string> routeLists = new List<string> ();
			routeLists.Add ("Маршрутный лист 1");
			routeLists.Add ("Маршрутный лист 2");
			return JsonConvert.SerializeObject (routeLists);		
		}

		public string GetRouteListOrders (string authKey, int routeListId)
		{
			//TODO: Replace with real logic
			Console.WriteLine("GetRouteListOrders called with args:\nauthKey: {0}\nrouteListId: {1}", authKey, routeListId);
			if (!CheckAuth (authKey))
				return null;
			List<string> orders = new List<string> ();
			if (routeListId == 1) {
				orders.Add ("Заказ 1");
				orders.Add ("Заказ 2");
			} else {
				orders.Add ("Заказ 3");
				orders.Add ("Заказ 4");
			}
			return JsonConvert.SerializeObject (orders);
		}
		#endregion
	}
}

