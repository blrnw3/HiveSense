using System;
using Microsoft.SPOT;

using GT = Gadgeteer;
using Gadgeteer.Modules.Seeed;
using Gadgeteer.Modules.GHIElectronics;

namespace HiveSenseV1 {
	/// <summary>
	/// Handler for all monitoring sensor modules (temp, light etc.)
	/// </summary>
	class SensorsHandler {

		// sensor data
		double temp;
		int humi;
		double temp2; //from barometer module
		int light;
		bool hiveMoving; //accelerometer

		//Thresholds
		const int threshHighTemp2 = 50;
		const int threshLowTemp2 = -10;
	
		LightSensor lightSensor;
		Accelerometer accelSensor;
		TemperatureHumidity tempHumiSensor;
		Barometer temp2Sensor;

		/// <summary>
		/// Attaches event handlers to all the modules, for measurements gathering purposes
		/// </summary>
		/// <param name="li">Lightsensor module</param>
		/// <param name="ac">Accelerometer module</param>
		/// <param name="th">Temp-Humi module</param>
		/// <param name="ba">Pressure-Temp module</param>
		public SensorsHandler( LightSensor li, Accelerometer ac, TemperatureHumidity th, Barometer ba ) {
			lightSensor = li;
			accelSensor = ac;
			tempHumiSensor = th;
			temp2Sensor = ba;

			tempHumiSensor.MeasurementComplete += new TemperatureHumidity.MeasurementCompleteEventHandler( temperatureHumidity_MeasurementComplete );
			temp2Sensor.MeasurementComplete += new Barometer.MeasurementCompleteEventHandler( barometer_MeasurementComplete );
			//accelerometer.MeasurementComplete += new Accelerometer.MeasurementCompleteEventHandler( accelerometer_MeasurementComplete );
			accelSensor.EnableThresholdDetection( Config.movementSensitivity, true, true, true, false, false, true );
			accelSensor.ThresholdExceeded += new Accelerometer.ThresholdExceededEventHandler( accelerometer_ThresholdExceeded );
		}

		/// <summary>
		/// Request or retrieve measurements from all sensors and store cleaned values in
		/// class members
		/// </summary>
		/// <returns>The cleaned class memebers representing each sensor's datum</returns>
		public string[] readSensors() {
			tempHumiSensor.RequestMeasurement();
			temp2Sensor.RequestMeasurement();
			accelSensor.RequestMeasurement();

			double lightFull = lightSensor.ReadLightSensorPercentage();
			//Debug.Print( "light: " + lightFull );
			light = (int) lightFull;

			//Sleeping doesn't work, the temp-humi data still won't arrive in time
			//System.Threading.Thread.Sleep( 1000 );

			double isMoving = (hiveMoving ? 1 : 0);
			hiveMoving = false; //reset so movement is not over-reported

			var allData = new double[] { temp2, humi, isMoving, light, temp };
			var cleanData = new String[allData.Length];
			// eliminate inaccurate precision
			for(int i = 0; i < allData.Length; i++) {
				string datum = allData[i].ToString();
				cleanData[i] = datum.Substring( 0, System.Math.Min( Config.precision, datum.Length ) );
			}

			return cleanData;
		}

		void accelerometer_ThresholdExceeded( Accelerometer sender ) {
			Debug.Print( "Hive is moving!" );
			hiveMoving = true;
		}
		void barometer_MeasurementComplete( Barometer sender, Barometer.SensorData sensorData ) {
			double lol = sensorData.Pressure;
			double t2 = sensorData.Temperature;
			//Debug.Print( "Pressure: " + lol );
			//Debug.Print( "TempBaro: " + t2 );
			if(t2 < threshHighTemp2 && t2 > threshLowTemp2) {
				temp2 = t2;
			} else {
				Debug.Print( "Dodgy baro data rejected " + lol + " " + t2 );
			}
		}

		//	void accelerometer_MeasurementComplete( Accelerometer sender, Accelerometer.Acceleration acceleration ) {
		//Debug.Print( "Accel: " + acceleration.ToString() );
		//}

		void temperatureHumidity_MeasurementComplete( TemperatureHumidity sender, double temperature, double relativeHumidity ) {
			//Debug.Print( "Temp: " + temperature );
			//Debug.Print( "Humi: " + relativeHumidity );

			temp = temperature;
			humi = (int) relativeHumidity;

			//temp, hum always last to respond
		}

	}
}