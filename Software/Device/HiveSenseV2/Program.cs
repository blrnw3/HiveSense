using Microsoft.SPOT;
using GT = Gadgeteer;
using Gadgeteer.Modules.GHIElectronics;

namespace HiveSenseV2 {
	/// <summary>
	/// Main
	/// </summary>
	/// <remarks>
	/// In other classes, references are sometimes made to "GHI documentation";
	/// this is found here: https://www.ghielectronics.com/docs/,
	/// and consists of SDK documentation for the Gadgeteer modules, with short code samples,
	/// some of which have been incorporated into this source code.
	/// </remarks>
	public partial class Program {
		uint dataCount = 0;

		GT.Timer timer;
		SensorsHandler sensorsHandle;
		SdHandler sdHandle;
		WifiHandler wifiHandle;
		TimeManager timeManager;
		APIconnector api;

		/// <summary>
		/// Called on Device boot
		/// </summary>
		void ProgramStarted() {
			Debug.Print( "Program Started" );

			Config.initialiseSettings();

			setupModules();
		}

		private void setupModules() {
			// ## System Time manager setup ##
			timeManager = new TimeManager();

			// ## Sensors setup ##
			sensorsHandle = new SensorsHandler( lightSensor, accelerometer, temperatureHumidity, barometer, camera );
			//Make initial, unhandled readings so values have time to initialise and later won't be null
			sensorsHandle.readNumericSensors();
			sensorsHandle.readBinarySensors();

			// ## Setup Sd card for config retrieval and data logging ##
			sdHandle = new SdHandler( sdCard, sensorsHandle );

			// ## Button setup ##
			button.ButtonPressed += new Button.ButtonEventHandler( buttonPressed );

			// ## Wifi setup ##
			wifiHandle = new WifiHandler( wifi_RS21, timeManager );

			// ## Timer setup ##
			timer = new GT.Timer( Config.updateRate * 1000 );
			timer.Tick += new GT.Timer.TickEventHandler( timer_Tick );
			timer.Start();
		}

		void timer_Tick( GT.Timer timer ) {
			timeManager.updateTime();
			//Create the api handler when a valid url is available
			if(Config.APIendpoint != "" && api == null) {
				api = new APIconnector( Config.APIendpoint, sensorsHandle );
			}
			retrieveAndProcessSensorData();
		}

		//Purely for debugging purposes - disable or re-enable network connection
		void buttonPressed( Button sender, Button.ButtonState state ) {
			if(wifi_RS21.IsNetworkConnected) {
				Debug.Print( "Disconnecting from wifi" );
				wifi_RS21.Interface.Disconnect();
			} else {
				Debug.Print( "Connecting to wifi" );
				wifiHandle.joinNetwork();
			}
		}

		/// <summary>
		/// Gets the latest processed sensor readings and takes a picture for POSTing
		/// before trying to save the text data to log as well as push it to the web<br />
		/// Periodically, this method will reboot the device for recovery management purposes.
		/// </summary>
		/// <remarks>The button's LED will turn on for the duration of this method</remarks>
		void retrieveAndProcessSensorData() {
			// Saftey mechanism in case of device hang
			if(dataCount == Config.rebootInterval) {
				Debug.Print( "REBOOTING NOW!" );
				Microsoft.SPOT.Hardware.PowerState.RebootDevice( true );
			}

			button.TurnLEDOn();
			Debug.Print( "Measurements being taken at : " + timeManager.getTime() );

			//Get data point and write to permanent log
			var dataNum = sensorsHandle.readNumericSensors();
			sdHandle.writeDataLineToSDcard( dataNum, SdHandler.datalogFilePathPerm, timeManager.getTime() );

			//Attempt to transmit data point to web API
			if(dataIsSendable() && api.sendCurrentData( dataNum )) {
				//Transmit buffer (if present)
				sdHandle.transmitSDloggedData();
				timeManager.resyncIfOld();

			} else {
				//Buffer to data point
				Debug.Print( "Data could not be sent to server! Writing to SD card instead" );
				sdHandle.writeDataLineToSDcard( dataNum, SdHandler.datalogFilePathBuffer, timeManager.getTime() );
				sdHandle.datalogBufferExists = true;
			}

			//Attempt to transmit binary data (the camera image)
			var dataBin = sensorsHandle.readBinarySensors();
			if(dataIsSendable()) {
				api.sendImage( dataBin );
			}

			button.TurnLEDOff();
			dataCount++;
		}

		bool dataIsSendable() {
			return wifi_RS21.IsNetworkConnected && api != null; 
		}
	}
}
