using System;

namespace Chat
{
	public class CallbackSubscriber
	{
		private IChatCallback callback;
		private int employeeId;

		public IChatCallback Callback { get { return callback; } }
		public int EmployeeId { get { return employeeId; } }

		public CallbackSubscriber (IChatCallback callback, int employeeId)
		{
			this.callback = callback;
			this.employeeId = employeeId;
		}
	}
}

