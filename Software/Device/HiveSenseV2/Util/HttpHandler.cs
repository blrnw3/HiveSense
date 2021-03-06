using System;
using System.Text;
using System.Net;
using System.IO;
using Microsoft.SPOT;

namespace HiveSenseV2 {
	/// <summary>
	/// Handler for making generic HTTP POST/PUT requests
	/// </summary>
	/// <remarks>The response body is deliberately ignored</remarks>
	class HttpHandler {

		string url;
		string methodType;
		string MimeType;

		/// <summary>
		/// Time to allow for an HTTP request to complete<br />
		/// Based on the requirement that updates do not overlap
		/// </summary>
		int timeOutPeriod;

		/// <summary>
		/// Creates a handler
		/// </summary>
		/// <param name="url">URL to which requests are made</param>
		/// <param name="method">HTTP method (PUT, POST etc.)</param>
		/// <param name="mime">HTTP Media type (JSON, CSV etc.)</param>
		public HttpHandler( string url, string method, string mime ) {
			this.url = url;
			this.methodType = method;
			this.MimeType = mime;

			timeOutPeriod = 500 * Config.updateRate;
		}

		/// <summary>
		/// Send a chunk of binary data to a URL, with timeout
		/// </summary>
		/// <param name="data">data to be sent to the server</param>
		public void Send( byte[] data ) {
			sendBinary( data );
		}

		/// <summary>
		/// Send a chunk of UTF-8 data to a URL, with timeout
		/// </summary>
		/// <param name="data">text to be sent to the server</param>
		public bool Send( string data ) {
			return sendBinary( Encoding.UTF8.GetBytes(data) );
		}

		/// <summary>
		/// Heavily borrowed from https://www.ghielectronics.com/community/codeshare/entry/477
		/// </summary>
		/// <param name="data"></param>
		private bool sendBinary( byte[] data ) {
			try {
				string result = "";
				var request = CreateRequest( data );
				request.Timeout = timeOutPeriod;
				// send request and receive response
				using(var response =
				(HttpWebResponse) request.GetResponse()) {
					result = HandleResponse( response );
				}
				return Int32.Parse(result) < 500; //No server errors
			} catch {
				Debug.Print( "Server error" );
				return false;
			}
		}

		/// <summary>
		/// Makes the HTTP request with suitable headers for the RESTful API<br />
		/// Parts copied from https://www.ghielectronics.com/community/codeshare/entry/477
		/// </summary>
		private HttpWebRequest CreateRequest( byte[] data ) {

			var request = (HttpWebRequest) WebRequest.Create( url );

			request.Method = methodType;
			// request headers
			request.UserAgent = "HiveSense - automatic beehive monitoring with .NET Gadgeteer";
			request.ContentLength = data.Length;
			request.ContentType = MimeType;
			//Add a custom header for server-side security purposes
			request.Headers.Add( "X-hiveSenseSecureKey", Config.APIsecurityKey );

			// request body
			using(System.IO.Stream stream = request.GetRequestStream()) {
				stream.Write( data, 0, data.Length );
			}

			return request;
		}

		/// <summary>
		/// Gets just the status code from the response, ignoring everything else
		/// </summary>
		private string HandleResponse( HttpWebResponse response ) {
			string statCode = response.StatusCode.ToString();
			Debug.Print( "Status code: " + statCode );
			return statCode;
		}

	}
}
