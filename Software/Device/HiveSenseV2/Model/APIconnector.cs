using System;
using System.Text;
using Microsoft.SPOT;


namespace HiveSenseV2 {
	/// <summary>
	/// Handler for interacting with a web API
	/// </summary>
	class APIconnector {

		private HttpHandler feedPutter;
		private HttpHandler imgPoster;

		SensorsHandler sensorHandle;

		/// <summary>
		/// Create a connection to the API for binary and numeric data
		/// </summary>
		/// <param name="APIurl">URL to which the data should be sent<br />
		/// WARNING: DO NOT use HTTPS - it doesn't work on Gadgeteer! (works fine on native Windows console application).
		///</param>
		/// <param name="sh"></param>
		public APIconnector(string APIurl, SensorsHandler sh) {
			feedPutter = new HttpHandler( APIurl + "/feed", "PUT", "application/json" );
			imgPoster = new HttpHandler( APIurl + "/image", "PUT", "application/octet-stream" );
			sensorHandle = sh;
		}

		/// <summary>
		/// Sends buffered data to the API
		/// </summary>
		/// <param name="dataLines">full csv data lines, one for each data point</param>
		/// <returns><c>true</c> on success (complete transmission), <c>false</c> on failure</returns>
		public bool sendHistoricalData( string[] dataLines ) {
			var compiledData = new System.Text.StringBuilder( dataLines.Length * dataLines[0].Length );
			string[] data = new string[sensorHandle.getChannelNames().Length];
			string[] fields;
			string timestamp;

			int validCnt = 0;

			foreach(string dataLine in dataLines) {
				if(dataLine != null && dataLine.Length > 10) { //Check for valid line
					fields = dataLine.Split( ',' );
					//parse datetime info
					var fieldsNum = new int[6];
					for(int i = 0; i < fieldsNum.Length; i++) {
						fieldsNum[i] = Int32.Parse( fields[i] );
					}
					//Create timestamp in ISO format
					timestamp = new DateTime( fieldsNum[0], fieldsNum[1], fieldsNum[2],
						fieldsNum[3], fieldsNum[4], fieldsNum[5] ).ToString();

					for(int i = 0; i < data.Length; i++) {
						data[i] = fields[i + 6];
					}

					//Reject data point if not time-synced
					if(fields[0] != "1970") {
						compiledData.Append( dataLineToAPIFormat( data, timestamp ) + "," );
						validCnt++;
					}
				}
			}
			return (validCnt > 0) ?
				feedPutter.Send( jsonWrap(compiledData.ToString().Substring(0, compiledData.Length-1) ))
				: true;
		}

		/// <summary>
		/// Transmitts the current image to the API
		/// </summary>
		/// <param name="pic">binary img data</param>
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
		/// <returns>API-compatible compiled data</returns>
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

	}
}
