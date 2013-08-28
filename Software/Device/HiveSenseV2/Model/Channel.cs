using System;
using Microsoft.SPOT;

namespace HiveSenseV2 {
	class Channel {

		public string name { get; protected set; }
		public int precision { get; protected set; }

		public double currentValue { get; set; }

		public Channel( string name, int precision ) {
			this.name = name;
			this.precision = precision;
		}

	}
}
