using System;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using QSOrmProject;
using Vodovoz.Domain.Employees;
using Vodovoz.Repository;
using Vodovoz.Repository.Logistics;
using Vodovoz.Domain.Logistic;
using Vodovoz.Domain.Orders;


namespace Android
{
	public class AndroidDriverService : IAndroidDriverService
	{
		IUnitOfWork uow = UnitOfWorkFactory.CreateWithoutRoot();

		#region IAndroidDriverService implementation

		/// <summary>
		/// Authenticating driver by login and password.
		/// </summary>
		/// <returns>authentication string or <c>null</c></returns>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		public string Auth (string login, string password)
		{
			#if DEBUG
			Console.WriteLine("Auth called with args:\nlogin: {0}\npassword: {1}", login, password);
			#endif

			var employee = EmployeeRepository.GetDriverByAndroidLogin (uow, login);

			if (employee == null)
				return null;

			//Generating hash from driver password
			var hash = (new SHA1Managed()).ComputeHash(Encoding.UTF8.GetBytes(employee.AndroidPassword));
			var hashString = string.Join("", hash.Select(b => b.ToString("x2")).ToArray());
			if (password == hashString) {

				//Creating session auth key if needed
				if (String.IsNullOrEmpty (employee.AndroidSessionKey)) {
					employee.AndroidSessionKey = Guid.NewGuid ().ToString ();
					uow.Save<Employee> (employee);
				}
				return employee.AndroidSessionKey;
			}
			return null;
		}

		/// <summary>
		/// Checking authentication key
		/// </summary>
		/// <returns><c>true</c>, if auth was checked, <c>false</c> otherwise.</returns>
		/// <param name="authKey">Auth key.</param>
		public bool CheckAuth (string authKey)
		{
			#if DEBUG
			Console.WriteLine("CheckAuth called with args:\nauthKey: {0}", authKey);
			#endif

			var driver = EmployeeRepository.GetDriverByAuthKey(uow, authKey);
			return driver != null;
		}

		/// <summary>
		/// Gets the route lists for driver authenticated with the specified key.
		/// </summary>
		/// <returns>The route lists or <c>null</c>.</returns>
		/// <param name="authKey">Authentication key.</param>
		public List<RouteListDTO> GetRouteLists (string authKey)
		{
			#if DEBUG
			Console.WriteLine("GetRouteLists called with args:\nauthKey: {0}", authKey);
			#endif

			if (!CheckAuth (authKey))
				return null;
			var driver = EmployeeRepository.GetDriverByAuthKey (uow, authKey);
			var routeLists = RouteListRepository.GetDriverRouteLists(uow, driver);

			var result = new List<RouteListDTO> ();
			foreach (RouteList rl in routeLists) {
				result.Add (new RouteListDTO (rl));
			}

			return result;
		}

		public List<OrderDTO> GetRouteListOrders (string authKey, int routeListId)
		{
			#if DEBUG
			Console.WriteLine("GetRouteListOrders called with args:\nauthKey: {0}\nrouteListId: {1}", authKey, routeListId);
			#endif

			if (!CheckAuth (authKey))
				return null;
			
			var routeListUoW = UnitOfWorkFactory.CreateForRoot<RouteList> (routeListId);

			if (routeListUoW == null || routeListUoW.Root == null)
				return null;

			var orders = new List<OrderDTO> ();
			foreach (RouteListItem item in routeListUoW.Root.Addresses) {
				orders.Add (new OrderDTO(item.Order));
			}
			return orders;
		}
		#endregion
	}
}

