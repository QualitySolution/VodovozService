using System;
namespace SmsSendInterface
{
	public class SmsBalanceEventArgs : EventArgs
	{
		BalanceType BalanceType { get; }
		decimal Balance { get; }

		public SmsBalanceEventArgs(BalanceType balanceType, decimal balance)
		{
			BalanceType = balanceType;
			Balance = balance;
		}
	}
}
