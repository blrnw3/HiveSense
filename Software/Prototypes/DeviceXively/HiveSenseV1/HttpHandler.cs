using System;
using System.Text;
using System.Net;
using System.IO;
using Microsoft.SPOT;

namespace HiveSenseV1 {
	/// <summary>
	/// Handler for generic HTTP requests - currently only POST is supported
	/// </summary>
	class HttpHandler {
		const string POST_URL = "http://nw3weather.co.uk/CP_Solutions/MscProject/get_image.php";

		/// <summary>
		/// POST a chunk of binary data to a URL, with timeout
		/// </summary>
		/// <param name="pic">bin dat</param>
		public static void Send( byte[] pic ) {
			try {
				var request = CreateRequest( pic );
				request.Timeout = 5000; // 5 seconds
				// send request and receive response
				using(var response =
				(HttpWebResponse) request.GetResponse()) {
					HandleResponse( response );
				}
			} catch {
				Debug.Print( "POSTING ERROR" );
			}
		}

		static HttpWebRequest CreateRequest( byte[] pic ) {

			var	request = (HttpWebRequest) WebRequest.Create( POST_URL );

			request.Method = "POST";
			// request headers
			request.UserAgent = "HiveSenseV1 - Gadgeteer UCL";
			request.ContentLength = pic.Length;
			request.ContentType = "pplication/octet-stream"; //binary MIME-type
			//Add a custom header for server-side security purposes
			request.Headers.Add( "hiveSenseSecureKey", "blr2013ucl" );

			// request body
			using(System.IO.Stream stream = request.GetRequestStream()) {
				stream.Write( pic, 0, pic.Length );
			}

			return request;
		}

		static void HandleResponse( HttpWebResponse response ) {
			Debug.Print( "POSTcam status code: " + response.StatusCode );
			//Stream dataStream = response.GetResponseStream();
			//StreamReader reader = new StreamReader( dataStream );
			//string responseFromServer = reader.ReadToEnd();
			//Debug.Print( "POSTcam response: " + responseFromServer );
			//// Clean up the streams.
			//reader.Close();
			//dataStream.Close();
			//response.Close();
		}

	}
}
