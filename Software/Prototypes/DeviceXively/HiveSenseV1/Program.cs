using System;
using Microsoft.SPOT;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using Gadgeteer.Modules.GHIElectronics;
using GHI.Premium.Net;

namespace HiveSenseV1 {

	/// <summary>
	/// Periodically get sensor data then pump to Web and SDcard <br />
	/// Code help sources:
	///		GHI electronics - https://www.ghielectronics.com/docs/
	/// </summary>
	public partial class Program {

		uint missedUpload = 0; //not used atm, but here for potential future use
		uint dataCount = 0;

		GT.Timer timer;

		//unix epoch so easy to sync with web time server
		DateTime systemTime = new DateTime( 1970, 1, 1, 0, 0, 0, 0 );

		SensorsHandler sensorsHandle;
		SdHandler sdHandle;
		WifiHandler wifiHandle;

		/// <summary>
		/// Run when the mainboard is powered up or reset.<br />
		/// Sets up all the attached modules, as well as the timer to make the sensor readings continuous
		/// </summary>
		void ProgramStarted() {
			Debug.Print( "Program Started" );

			Config.initialiseSettings();

			// ## Setup Sd card for config retrieval and data logging ##
			sdHandle = new SdHandler( sdCard );
			
			// ## Sensors setup ##
			sensorsHandle = new SensorsHandler( lightSensor, accelerometer, temperatureHumidity, barometer );
			//Make initial, unhandled reading so values have time to initialise and later won't be null
			sensorsHandle.readSensors();

			// ## Camera setup ##
			camera.PictureCaptured +=new Camera.PictureCapturedEventHandler(pictureCaptured);

			// ## Button setup ##
			button.ButtonPressed +=new Button.ButtonEventHandler(buttonPressed);

			// ## Wifi setup ##
			wifi_RS21.Interface.WirelessConnectivityChanged += new WiFiRS9110.WirelessConnectivityChangedEventHandler( wirelessConnectivityChanged );
			wifiHandle = new WifiHandler( wifi_RS21 );

			// ## Timer setup ##
			timer = new GT.Timer( Config.updateRate * 1000 );
			timer.Tick += new GT.Timer.TickEventHandler( timer_Tick );
			timer.Start();
		}

		// ##### Event Handlers ##### \\

		void wirelessConnectivityChanged( object sender, WiFiRS9110.WirelessConnectivityEventArgs e ) {
			if(e.IsConnected) {
				Debug.Print( "NETWORK UP." );
				// After connecting, get the true time from the web for synching system clock
				syncTime();
			} else {
				Debug.Print( "NETWORK DOWN." );
			}
		}

		void timer_Tick( GT.Timer timer ) {
			systemTime = systemTime.AddSeconds( Config.updateRate );
			retrieveAndProcessSensorData();
		}

		/// <summary>
		/// Handler for when a picture is taken: attempts to POST it to the web
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="picture">raw pic data</param>
		void pictureCaptured( Camera sender, GT.Picture picture ) {
			byte[] cameraSnapshot = picture.PictureData;
			if(cameraSnapshot != null) {
				if(wifi_RS21.IsNetworkConnected) {
					HttpHandler.Send( cameraSnapshot );
				} else {
					Debug.Print( "No network for POST" );
				}
			} else {
				Debug.Print( "Null pic data" );
			}
		}

		//Purely for debugging purposes atm - disable or re-enable network connection
		void buttonPressed( Button sender, Button.ButtonState state ) {
			if(wifi_RS21.IsNetworkConnected) {
				Debug.Print( "Disconnecting from wifi" );
				wifi_RS21.Interface.Disconnect();
			} else {
				Debug.Print( "Connecting to wifi" );
				wifiHandle.searchAndJoinNetwork();
			}
		}


		/// <summary>
		/// Tries to sync the system time with a UTC web time server<br />
		/// Source: GHI docs
		/// </summary>
		void syncTime() {
			HttpRequest wc = WebClient.GetFromWeb( "http://nw3weather.co.uk/CP_Solutions/SQLdown.php?time" );
			wc.ResponseReceived += new HttpRequest.ResponseHandler( syncResponseHandle );
		}
		void syncResponseHandle( HttpRequest sender, HttpResponse response ) {
			long webTime = long.Parse( response.Text );
			systemTime = systemTime.AddSeconds( webTime );
			Debug.Print( "new datetime is " + systemTime.ToString( "HH:mm dd MM yyyy z" ) );
		}

		/// <summary>
		/// Gets the latest processed sensor readings and takes a picture for POSTing
		/// before trying to save the text data to log as well as push it to the web<br />
		/// Periodically, this method will reboot the device for recovery management purposes.
		/// </summary>
		/// <remarks>The button's LED will turn on for the duration of a call to this method</remarks>
		void retrieveAndProcessSensorData() {

			// Saftey mechanism in case of device hang
			if(dataCount == Config.rebootInterval) {
				Debug.Print( "REBOOTING NOW!" );
				Microsoft.SPOT.Hardware.PowerState.RebootDevice( true );
			}

			button.TurnLEDOn();
			Debug.Print( "Measurements being taken at : " + systemTime.ToLocalTime() );

			camera.TakePicture();

			var data = sensorsHandle.readSensors();
			sdHandle.writeDataLineToSDcard( data, SdHandler.datalogFilePathPermanent, systemTime );

			if(wifi_RS21.IsNetworkConnected) {
				Xively.sendCurrentData( data );
				missedUpload = 0;
				sdHandle.sendSDloggedDataToWeb();

				//If unsynced due to failure on network join
				if(systemTime.Year == 1970) {
					syncTime();
				}
			} else {
				Debug.Print( "No network for PUT! Writing to SD card instead" );
				sdHandle.writeDataLineToSDcard( data, SdHandler.datalogFilePath, systemTime );
				sdHandle.datalogFileExists = true;
				missedUpload++;
			}
	
			button.TurnLEDOff();
			dataCount++;
		}

	}
}