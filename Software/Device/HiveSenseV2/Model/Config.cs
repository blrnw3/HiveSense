using System;
using Microsoft.SPOT;

namespace HiveSenseV2 {
	/// <summary>
	/// Configuration settings for main program, wifi, sensors, storage etc.<br />
	/// Some fields are overridden by settings loaded from a configuration file on the SD card (if present)
	/// </summary>
	/// <remarks>
	/// Add more stuff here to improve ease of customisations
	/// </remarks>
	class Config {
		/// <summary>
		/// Frequency in update cycles at which to reboot the device in a vain attempt
		/// to reduce the chance of time-induced crashes
		/// </summary>
		public const int rebootInterval = 50;

		/// <summary>
		/// Frequency in update cycles at which to resync the interal device clock with the time server<br />
		/// Not needed whilst the reboot practice is used
		/// </summary>
		public const int reSyncInterval = 20;

		/// <summary>
		/// Reliable source for synchronising the Device clock
		/// </summary>
		public const string timeServer = "http://wxapp.nw3weather.co.uk/API/SQLdown.php?time";

		/// <summary>
		/// Validation key for the security HTTP header required by the API
		/// </summary>
		public const string APIsecurityKey = "blr2013ucl";

		/// <summary>
		/// SSID of the wifi network to which there is a desire to connect
		/// </summary>
		public static string desiredSSID { get; set; }
		/// <summary>
		/// Passphrase/word for the wifi network one is desirous of joining
		/// </summary>
		public static string wifiPass { get; set; }

		/// <summary>
		/// Frequency in seconds at which to retrieve, log and transmit sensor data (process cycle)
		/// </summary>
		public static int updateRate { get; set; }

		/// <summary>
		/// Sensitivity of the accelerometer movement detection.<br />
		/// -8 is the most sensitive, +8 the least. Any value outside this range is rejected.
		/// </summary>
		public static double movementSensitivity { get; set; }

		/// <summary>
		/// URL of the HTTP web API
		/// </summary>
		public static string APIendpoint { get; set; }

		/// <summary>
		/// Initialises config settings to best-guess values
		/// </summary>
		public static void initialiseSettings() {
			movementSensitivity = -7.5;
			updateRate = 60;
			desiredSSID = "BTOpenzone";
			wifiPass = "";
			APIendpoint = "";
		}

		//As determined by stress testing
		private static int maxFrequency = 5;

		/// <summary>
		/// Loads configuration settings from the XML file on the SD card, if present, and overrides current defaults
		/// </summary>
		public static void getXmlSettings( Xml config ) {
			Config.desiredSSID = config.getElement( "SSID" );
			Debug.Print( "new SSID: " + Config.desiredSSID );
			Config.wifiPass = config.getElement( "password" );
			Debug.Print( "new wifipass: " + Config.wifiPass );

			Config.APIendpoint = config.getElement( "server" );
			Debug.Print( "new server: " + Config.APIendpoint );

			try {
				int newRate = Int32.Parse( config.getElement( "updateRate" ) );
				Debug.Print( "new updaterate: " + newRate );
				if(newRate >= maxFrequency) {
					//Timer is set up after this method is called, so this is sufficient
					Config.updateRate = newRate;
				}
			} catch {
				Debug.Print( "bad updateRate specified in config" );
			}

			try {
				double newSensitivity = Double.Parse( config.getElement( "sensitivity" ) );
				Debug.Print( "new sensitivity: " + newSensitivity );
				// 8 is the magic number given by the accelerometer SDK docs.
				if(newSensitivity > -8 && newSensitivity < 8) {
					Config.movementSensitivity = newSensitivity;
				}
			} catch {
				Debug.Print( "bad sensitivity specified in config" );
			}

		}

	}
}
