using System;

namespace HiveSenseV2 {
	/// <summary>
	/// Very basic XML file reader
	/// </summary>
	/// <remarks>Necessitated by the abscence of such functionality in the core .NET MF libraries </remarks>
	class Xml {

		private string xml;

		/// <summary>
		/// Creates an XML reader from a pre-read file
		/// </summary>
		/// <param name="rawXml">the UTF-8 file data</param>
		public Xml( byte[] rawXml ) {
			var xmlBuild = new System.Text.StringBuilder( rawXml.Length );
			foreach(byte b in rawXml) {
				xmlBuild.Append( (char) b );
			}
			xml = xmlBuild.ToString();
		}

		/// <summary>
		/// Gets the value contained within a named element/tag/node pair
		/// </summary>
		/// <param name="elementName">Name of the element</param>
		/// <returns>The element's value, or an empty <c>string</c> on failure to find the element</returns>
		public string getElement( string elementName ) {

			string startTag = "<" + elementName + ">";
			string endTag = "</" + elementName + ">";

			int start = xml.IndexOf( startTag ) + startTag.Length;
			int end = xml.IndexOf( endTag );

			if(start <= 0 || end <= 0) {
				return "";
			}

			return xml.Substring( start, end - start ).Trim();
		}
	}
}
