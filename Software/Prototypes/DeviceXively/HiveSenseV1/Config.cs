using System;
using Microsoft.SPOT;

namespace HiveSenseV1 {
	/// <summary>
	/// Configuration settings for main program, wifi, sensors, storage etc.
	/// </summary>
	/// <remarks>
	/// Add more stuff here to improve customisability
	/// </remarks>
	class Config {

		public const int rebootInterval = 50;
		public const int reSyncInterval = 20;

		/// <summary>
		/// SSID of the wifi network to which there is a desire to connect
		/// </summary>
		public static string desiredSSID { get; set; }
		/// <summary>
		/// Passphrase/word for the wifi network one is desirous of joining
		/// </summary>
		public static string wifiPass { get; set; }

		/// <summary>
		/// Frequency in seconds at which to retrieve, log and transmit sensor data
		/// </summary>
		public static int updateRate { get; set; }
		/// <summary>
		/// Maximum number of significant figures to be used in sensor data
		/// </summary>
		public static int precision { get; set; }
		/// <summary>
		/// Sensitivity of the accelerometer movement detection.<br />
		/// -8 is the most sensitive, 8 the least. Any value outside this range is rejected.
		/// </summary>
		public static double movementSensitivity { get; set; }

		/// <summary>
		/// Initialises config settings to best-guess values
		/// </summary>
		public static void initialiseSettings() {
			movementSensitivity = -7.5;
			updateRate = 60;
			precision = 4;
			desiredSSID = "BTOpenzone";
			wifiPass = "";
		}

	
		/*** Implementation of singleton design pattern  ***/
		//private static Config instance;

		// set default config
		//private Config() {
		//    desiredSSID = "";
		//    wifiPass = "";

		//    updateRate = 60;
		//    precision = 4;
		//    movementSensitivity = -7.5;
		//}

		//public static Config getInstance() {
		//    if(instance == null) {
		//        instance = new Config();
		//    }
		//    return instance;
		//}



	}
}
