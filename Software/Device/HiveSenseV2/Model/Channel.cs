using System;
using Microsoft.SPOT;

namespace HiveSenseV2 {
	/// <summary>
	/// Stores properties for a single measurement variable (temp, hum, light...)
	/// </summary>
	class Channel {

		/// <summary>
		/// Short unique identifier
		/// </summary>
		public string name { get; protected set; }
		/// <summary>
		/// Number of decimal places to keep for the values
		/// </summary>
		public int precision { get; protected set; }

		public double currentValue { get; set; }

		/// <summary>
		/// Initialise a measurement Channel
		/// </summary>
		public Channel( string name, int precision ) {
			this.name = name;
			this.precision = precision;
		}

	}
}
