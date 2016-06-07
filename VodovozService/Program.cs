using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Threading;
using Android;
using Mono.Unix;
using Mono.Unix.Native;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace VodovozService
{
	class Service
	{
		public static void Main (string[] args)
		{
			try {
				ServiceHost AndroidDriverHost = new ServiceHost (typeof(AndroidDriverService));
				AndroidDriverHost.AddServiceEndpoint (
					typeof(IAndroidDriverService), 
					new BasicHttpBinding(),
					"http://rs.qsolution.ru:9000/AndroidDriverService");
				AndroidDriverHost.Open();

				UnixSignal[] signals = { 
					new UnixSignal (Signum.SIGINT),
					new UnixSignal (Signum.SIGHUP),
					new UnixSignal (Signum.SIGTERM)};
				UnixSignal.WaitAny (signals);
			} catch (Exception e) {
				Console.Write (e.StackTrace);
			} finally {
				if (Environment.OSVersion.Platform == PlatformID.Unix)
					Thread.CurrentThread.Abort ();
				Environment.Exit (0);
			}
		}


	}
}


