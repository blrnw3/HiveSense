using System;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.SPOT;


namespace HiveSenseV1 {
	/// <summary>
	/// Special handler for interacting with the Xively RESTful API<br />
	/// CSV format was chosen because of the lack of XML/JSON libraries on .NET MF for Gadgeteer,
	/// and beacuse the data to be sent is fairly straightforward.
	/// </summary>
	class Xively {

		const string APIkey = "eyh87ZNEOFkvgbCDKshBCoHOreghaA57q0nefAO5s6GRfjBh";
		/// <summary>
		/// URL to which the data should be PUT
		/// WARNING: DO NOT use HTTPS - it doesn't work on Gadgeteer! (works fine on native Windows console application).
		/// </summary>
		static string RESTurl = "http://api.xively.com/v2/feeds/1693757499.csv";
		static string RESTurl2 = "http://hivesensenodejs.azurewebsites.net/posty";


		/// <summary>
		/// Xively Channels - one for each measurement property
		/// </summary>
		private static readonly string[] channelNames = {
				"AmbientTemp", "Humidity", "IsMoving", "Light", "Temperature" };

		
		/// <summary>
		/// PUTs buffered data to the API
		/// </summary>
		/// <param name="dataLines">full csv data lines, one for each time interval</param>
		/// <returns><c>true</c> on success (complete transmission), <c>false</c> on failure</returns>
		public static bool sendHistoricalData( string[] dataLines ) {
			var compiledData = new System.Text.StringBuilder(dataLines.Length * dataLines[0].Length);
			string[] data = new string[channelNames.Length];
			string[] fields;
			string timestamp;

			int validCnt = 0;
			foreach(string dataLine in dataLines) {
				if(dataLine != null && dataLine.Length > 10) {
					fields = dataLine.Split( ',' );
					Debug.Print( "currmin: " + fields[4] );
					timestamp = fields[0] + '-' + fields[1] + '-' + fields[2] + 'T' +
						fields[3] + ':' + fields[4] + ':' + fields[5] + "Z,";
					for(int i = 0; i < data.Length; i++) {
						data[i] = fields[i + 6];
					}
					if(fields[0] != "1970") { //default year when not time-synced
						compiledData.Append( dataLineToXivelyFormat( data, timestamp ) );
						validCnt++;
					}
				}
			}
			return (validCnt > 0) ?  Send( compiledData.ToString(), RESTurl ) : true;

		}

		/// <summary>
		/// PUTs the current sensor data to the API
		/// </summary>
		/// <param name="data">all current, clean data</param>
		public static void sendCurrentData( string[] data ) {
			string sendData = dataLineToXivelyFormat( data, "" );
			Send( sendData, RESTurl );
			Send( sendData, RESTurl2 );

		}

		/// <summary>
		/// Get data into Xively-compatible CSV format
		/// </summary>
		/// <param name="data">raw, but clean, data</param>
		/// <param name="timestamp">timestamp of the data - use empty string to ignore this</param>
		/// <returns>compatible compiled data</returns>
		static string dataLineToXivelyFormat( string[] data, string timestamp ) {
			//bool useTimestamp = (timestamp != "");
			string compiledData = "";
			//string time;
			for(int i = 0; i < channelNames.Length; i++) {
			//	time = useTimestamp ? 
				compiledData += channelNames[i] + ", " + timestamp + data[i] + "\n";
			}
			return compiledData;
		}


		/// <summary>
		/// Tries to perform the actual PUT transmission<br />
		/// Heavily borrowed from https://www.ghielectronics.com/community/codeshare/entry/477
		/// </summary>
		static bool Send( string compiledData, string url ) {
			try {
				using(var request = CreateRequest( compiledData, url )) {
					request.Timeout = 5000; // 5 seconds
					// send request and receive response
					using(var response =
					(HttpWebResponse) request.GetResponse()) {
						HandleResponse( response );
					}
				}
				return true;
			} catch {
				Debug.Print( "HERE BE XIVELY ERRORS" );
				return false;
			}
		}


		/// <summary>
		/// Makes the HTTP request with suitable headers for the RESTful API<br />
		/// Heavily borrowed from https://www.ghielectronics.com/community/codeshare/entry/477
		/// </summary>
		static HttpWebRequest CreateRequest( string sample, string url ) {
			byte[] buffer = Encoding.UTF8.GetBytes( sample );
			var request = (HttpWebRequest) WebRequest.Create( url );

			// request line
			request.Method = "PUT";

			// request headers
			request.UserAgent = "HiveSenseV1 - Gadgeteer UCL";
			request.ContentLength = buffer.Length;
			request.ContentType = "text/csv";
			request.Headers.Add( "X-ApiKey", APIkey );

			// request body
			using(Stream stream = request.GetRequestStream()) {
				stream.Write( buffer, 0, buffer.Length );
			}

			Debug.Print( "request created and sent" );
			return request;
		}

		/// <summary>
		/// Gets the server's response code
		/// </summary>
		/// <param name="response">http response</param>
		static void HandleResponse( HttpWebResponse response ) {
			Debug.Print( "Xively status code: " + response.StatusCode );
		}
	}
}