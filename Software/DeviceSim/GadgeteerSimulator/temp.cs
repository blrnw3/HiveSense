using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GadgeteerSimulator {
	class temp {

		double[] channels = new double[7];

		private static readonly string[] channelNames = {
				"temp1", "humi", "motion", "light", "temp2", "tempdiff", "mass" };

		void code() {	
			channels[6] = randomSensorValue( 80, 100, 1 );
		}

		private double randomSensorValue( int p, int p_2, int p_3 ) {
			throw new NotImplementedException();
		}
	}
}
