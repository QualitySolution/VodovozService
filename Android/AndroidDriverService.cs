using System;

namespace Android
{
	public class AndroidDriverService : IAndroidDriverService
	{
		#region IAndroidDriverService implementation

		public string Auth (string login, string password)
		{
			Console.WriteLine ("login:{0} password:{1}", login, password);
			if (login == "vad" && password == "123")
				return "SuC3ss";
			else
				return "Fa1lure";
		}

		#endregion
	}
}

