using System;
using Microsoft.SPOT;
using Gadgeteer.Networking;

namespace HiveSenseV2 {
	/// <summary>
	/// Manager for the Device system clock
	/// </summary>
	class TimeManager {

		private DateTime systemTime;

		public TimeManager() {
			//unix epoch so easy to sync with web time server
			systemTime = new DateTime( 1970, 1, 1, 0, 0, 0, 0 );
		}

		/// <summary>
		/// Tries to sync the system time with a UTC web time server
		/// </summary>
		public void syncTime() {
			HttpRequest wc = WebClient.GetFromWeb( Config.timeServer );
			wc.ResponseReceived += new HttpRequest.ResponseHandler( syncResponseHandle );
		}
		void syncResponseHandle( HttpRequest sender, HttpResponse response ) {
			long webTime = long.Parse( response.Text );
			if(systemTime.Year == 1970) {
				systemTime = systemTime.AddSeconds( webTime );
			}
			Debug.Print( "new datetime is " + systemTime.ToString( "HH:mm dd MM yyyy z" ) );
		}

		public void resyncIfOld() {
			//If unsynced due to failure on network join
			if(systemTime.Year == 1970) {
				syncTime();
			}
		}

		/// <summary>
		/// Advance the clock
		/// </summary>
		public void updateTime() {
			systemTime = systemTime.AddSeconds( Config.updateRate );
		}

		/// <summary>
		/// Get the current system time
		/// </summary>
		public DateTime getTime() {
			return systemTime;
		}
	}
}
