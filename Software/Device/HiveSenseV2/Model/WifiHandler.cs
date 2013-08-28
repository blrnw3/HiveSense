using System;
using Microsoft.SPOT;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;
using GHI.Premium.Net;

namespace HiveSenseV2 {
	/// <summary>
	/// Handler for the Wifi-RS21 module
	/// </summary>
	class WifiHandler {

		WiFi_RS21 wifiModule;
		TimeManager timemanager;

		GT.Timer timerWifiJoin;

		/// <summary>
		/// Activates the wifi module and attempts to join a network.<br />
		/// A timer is started that will trigger attempts to join the network if later disconnected at any point
		/// </summary>
		/// <param name="wifiModule">The wifi RS21 module</param>
		public WifiHandler( WiFi_RS21 wifiModule, TimeManager tm) {
			this.wifiModule = wifiModule;
			this.timemanager = tm;

			// these two calls becasue GHI say so
			wifiModule.UseDHCP();
			NetworkInterfaceExtension.AssignNetworkingStackTo( wifiModule.Interface );
			joinNetwork();

			// Timer to retry if failed on startup, or network goes down.
			timerWifiJoin = new GT.Timer( 600000 ); //10-mins
			timerWifiJoin.Tick += new GT.Timer.TickEventHandler( timerWifiJoin_Tick );
			timerWifiJoin.Start();

			this.wifiModule.Interface.WirelessConnectivityChanged += new WiFiRS9110.WirelessConnectivityChangedEventHandler( wirelessConnectivityChanged );
		}

		/// <summary>
		/// Searches all available networks in range, attmpting to join the first
		/// that matches the config-specified SSID
		/// </summary>
		/// <remarks>Strongest signals are found first, so the best connection is always used</remarks>
		public void joinNetwork() {
			// search for all available networks
			WiFiNetworkInfo[] scanResults = wifiModule.Interface.Scan();
			//search for the desired network
			Debug.Print( "Searching for SSID" );
			foreach(WiFiNetworkInfo result in scanResults) {
				if(result.SSID == Config.desiredSSID) {
					attemptJoin( result );
					break;
				}
			}
		}

		/// <summary>
		/// Try to join a wifi network
		/// </summary>
		/// <param name="network">desired network</param>
		void attemptJoin( WiFiNetworkInfo network ) {
			try {
				wifiModule.Interface.Join( network, Config.wifiPass );
				Debug.Print( "Network joined" );
			} catch(NetworkInterfaceExtensionException e) {
				Debug.Print( "Join error: " + ((NetworkInterfaceExtensionException.ErrorCode) (e.errorCode)).ToString() );
			}
		}

		void timerWifiJoin_Tick( GT.Timer timer ) {
			//poll in case of connection loss so device can (re)join
			if(!wifiModule.IsNetworkConnected) {
				joinNetwork();
			}
		}

		void wirelessConnectivityChanged( object sender, WiFiRS9110.WirelessConnectivityEventArgs e ) {
			if(e.IsConnected) {
				Debug.Print( "NETWORK UP." );
				// After connecting, get the true time from the web for synching system clock
				timemanager.syncTime();
			} else {
				Debug.Print( "NETWORK DOWN." );
			}
		}

		/// <summary>
		/// Searches for all wifi networks and prints full debug information
		/// </summary>
		void networkScanDebug() {
			WiFiNetworkInfo[] scanResults = wifiModule.Interface.Scan();
			Debug.Print( "Number of results: " + scanResults.Length );

			foreach(WiFiNetworkInfo result in scanResults) {
				Debug.Print( "****" + result.SSID + "****" );
				Debug.Print( "RSSI = " + result.RSSI ); //signal strength
				Debug.Print( "ChannelNumber = " + result.ChannelNumber );
				Debug.Print( "networkType = " + result.networkType );
				Debug.Print( "PhysicalAddress = " + Utility.GetMACAddress( result.PhysicalAddress ) );
				Debug.Print( "SecMode = " + result.SecMode );
			}
			Debug.Print( "Network search fininshed" );
		}


		/// <summary>
		///Get the MAC (physical) address of the connected wifi RS21 module
		/// </summary>
		/// <returns>formatted MAC address</returns>
		string getWifiChipMACAddress() {
			return Utility.GetMACAddress( wifiModule.Interface.NetworkInterface.PhysicalAddress );
		}

	}
}
