using System;
using System.Text;
using Microsoft.SPOT;

namespace HiveSenseV1 {
	/// <summary>
	/// Useful generic methods for this project
	/// </summary>
	class Utility {

		/// <summary>
		/// Converts a raw phyiscal/MAC address into a clean <c>string</c> representation<br />
		/// Source: modified from GHI documentation
		/// </summary>
		/// <param name="physicalAddress">raw</param>
		/// <returns>clean</returns>
		public static string GetMACAddress( byte[] physicalAddress ) {
			string mac = "";
			for(int i = 0; i < 4; i++) {
				mac += ByteToHex( physicalAddress[i] ) + '-';
			}
			return mac + ByteToHex( physicalAddress[5] );
		}

		/// <summary>
		/// Converts a byte to a hex string<br />
		/// Source: verbatim copy from GHI documentation
		/// </summary>
		/// <param name="number">byte</param>
		/// <returns>hex</returns>
		private static string ByteToHex( byte number ) {
			string hex = "0123456789ABCDEF";
			return new string( new char[] { hex[(number & 0xF0) >> 4], hex[number & 0x0F] } );
		}

		/// <summary>
		/// Convert an array of strings to a csv line (a sort of 'implode' function)
		/// </summary>
		/// <param name="ss">values to join</param>
		/// <returns>values joined with commas</returns>
		public static string csvJoin( string[] ss ) {
			string csv = "";
			foreach(string s in ss) {
				csv += s + ',';
			}
			return csv.Substring( 0, csv.Length - 1 );
		}

	

	}
}
