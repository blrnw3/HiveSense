using System;
using System.Text;
using Microsoft.SPOT;


namespace HiveSenseV2 {
	/// <summary>
	/// Handler for interacting with an API
	/// </summary>
	class APIconnector {

		private HttpHandler feedPutter;
		private HttpHandler imgPoster;

		SensorsHandler sensorHandle;

		/// <summary>
		/// URL to which the data should be sent
		/// WARNING: DO NOT use HTTPS - it doesn't work on Gadgeteer! (works fine on native Windows console application).
		/// </summary>
		public APIconnector(string APIurl, SensorsHandler sh) {
			feedPutter = new HttpHandler( APIurl, "PUT", "text/csv" );
			imgPoster = new HttpHandler( APIurl + "?image", "POST", "application/octet-stream" );
			sensorHandle = sh;
		}

		/// <summary>
		/// Sends buffered data to the API
		/// </summary>
		/// <param name="dataLines">full csv data lines, one for each time interval</param>
		/// <returns><c>true</c> on success (complete transmission), <c>false</c> on failure</returns>
		public bool sendHistoricalData( string[] dataLines ) {
			var compiledData = new System.Text.StringBuilder( dataLines.Length * dataLines[0].Length );
			string[] data = new string[sensorHandle.getChannelNames().Length];
			string[] fields;
			string timestamp;

			int validCnt = 0;
			foreach(string dataLine in dataLines) {
				if(dataLine != null && dataLine.Length > 10) {
					fields = dataLine.Split( ',' );
					Debug.Print( "currmin: " + fields[4] );

					//Create timestamp in ISO format
					timestamp = fields[0] + '-' + fields[1] + '-' + fields[2] + 'T' +
						fields[3] + ':' + fields[4] + ':' + fields[5] + "Z";

					for(int i = 0; i < data.Length; i++) {
						data[i] = fields[i + 6];
					}

					if(fields[0] != "1970") { //default year when not time-synced
						compiledData.Append( dataLineToAPIFormat( data, timestamp ) + "," );
						validCnt++;
					}
				}
			}
			return (validCnt > 0) ?
				feedPutter.Send( jsonWrap(compiledData.ToString().Substring(0, compiledData.Length-1) ))
				: true;
			//return true;
		}

		public void sendImage( byte[] pic ) {
			imgPoster.Send( pic );
		}

		/// <summary>
		/// PUTs the current sensor data to the API
		/// </summary>
		/// <param name="data">all current, clean data</param>
		public bool sendCurrentData( string[] data ) {
			string sendData = jsonWrap( dataLineToAPIFormat( data, "" ) );
			return feedPutter.Send( sendData );
		}

		/// <summary>
		/// Get data into API-compatible JSON format<br />
		/// No native JSON builder exists for NETMF, so this very ugly and specific fn is necessary
		/// </summary>
		/// <param name="data">raw, but cleaned (of excess precision), data</param>
		/// <param name="timestamp">timestamp of the data - use empty string to ignore this</param>
		/// <returns>compatible compiled data</returns>
		private string dataLineToAPIFormat( string[] data, string timestamp ) {
			string compiledData = "{\"channels\":{";
			string[] channels = sensorHandle.getChannelNames();

			for(int i = 0; i < channels.Length; i++) {
				compiledData += "\"" + channels[i] + "\":\"" + data[i] + "\"";
				if(i < channels.Length - 1) {
					compiledData += ",";
				}
			}

			compiledData += "}";
			if(timestamp.Length > 0) {
				compiledData += ",\"datetime\":\"" + timestamp + "\"";
			}
			compiledData += "}";

			Debug.Print("compiled as " + compiledData );
			return compiledData;
		}

		private string jsonWrap( string jsonDatapoints ) {
			return "{\"datapoints\":[" + jsonDatapoints + "]}";
		}

		private string zerolead( string i ) {
			return (Int32.Parse( i ) < 10) ? '0' + i : i;
		}

	}
}
