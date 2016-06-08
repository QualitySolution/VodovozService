using System;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using QSOrmProject;
using Vodovoz.Domain.Employees;


namespace Android
{
	public class AndroidDriverService : IAndroidDriverService
	{
		IUnitOfWork uow = UnitOfWorkFactory.CreateWithoutRoot();

		#region IAndroidDriverService implementation

		public string Auth (string login, string password)
		{
			#if DEBUG
			Console.WriteLine("Auth called with args:\nlogin: {0}\npassword: {1}", login, password);
			#endif

			Employee employeeAlias = null;

			var employees = uow.Session.QueryOver<Employee> (() => employeeAlias)
				.Where (() => employeeAlias.AndroidLogin == login)
				.Where (() => employeeAlias.IsFired == false)
				.List ();

			if (employees == null)
				return null;
			var employee = employees.First ();

			var hash = (new SHA1Managed()).ComputeHash(Encoding.UTF8.GetBytes(employee.AndroidPassword));
			string str = string.Join("", hash.Select(b => b.ToString("x2")).ToArray());
			if (password == str) {
				if (String.IsNullOrEmpty (employee.AndroidSessionKey)) {
					employee.AndroidSessionKey = Guid.NewGuid ().ToString ();
					uow.Save<Employee> (employee);
				}
				return employee.AndroidSessionKey;
			}
			else
				return null;
		}

		public bool CheckAuth (string authKey)
		{
			#if DEBUG
			Console.WriteLine("CheckAuth called with args:\nauthKey: {0}", authKey);
			#endif

			Employee employeeAlias = null;

			var employees = uow.Session.QueryOver<Employee> (() => employeeAlias)
				.Where (() => employeeAlias.AndroidSessionKey == authKey)
				.Where (() => employeeAlias.IsFired == false)
				.List ();

			if (employees == null)
				return false;
			return true;
		}

		public List<string> GetRouteLists (string authKey)
		{
			//TODO: Replace with real logic
			Console.WriteLine("GetRouteLists called with args:\nauthKey: {0}", authKey);
			if (!CheckAuth (authKey))
				return null;
			List<string> routeLists = new List<string> ();
			routeLists.Add ("Маршрутный лист 1");
			routeLists.Add ("Маршрутный лист 2");

			return routeLists; 		
		}

		public List<string> GetRouteListOrders (string authKey, int routeListId)
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
			return orders;
		}
		#endregion
	}
}

