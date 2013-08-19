using System;
using Microsoft.SPOT;

using GT = Gadgeteer;
using Gadgeteer.Modules.Seeed;
using Gadgeteer.Modules.GHIElectronics;

namespace HiveSenseV2 {
	/// <summary>
	/// Handler for all monitoring sensor modules (temp, light etc.)
	/// </summary>
	class SensorsHandler {

		/**
		 * Sensor modules (note well that there may be more than one channel per sensor)
		 **/
		LightSensor lightSensor;
		Accelerometer accelSensor;
		TemperatureHumidity tempHumiSensor;
		Barometer temp2Sensor;
		Camera camera;

		/**
		* Sensor Channels (individual numeric variables) - one for each measurement property/variable to use<br />
		* Names are primarily used as identifiers for the API, but are also used by the data point logfiles
		**/
		Channel t1 = new Channel( "temp1", 1 );
		Channel t2 = new Channel( "temp2", 1 );
		Channel tdiff = new Channel( "tempdiff", 1 );
		Channel hum = new Channel( "humi", 0 );
		Channel light = new Channel( "light", 0 );
		Channel motion = new Channel( "motion", 0 );

		Channel[] channels;

		//Binary sensor data
		byte[] image;

		/// <summary>
		/// Attaches event handlers to all the modules, for measurements gathering purposes
		/// </summary>
		/// <param name="li">Lightsensor module</param>
		/// <param name="ac">Accelerometer module</param>
		/// <param name="th">Temp-Humi module</param>
		/// <param name="ba">Pressure-Temp module</param>
		/// <param name="ca">Camera module</param>
		public SensorsHandler( LightSensor li, Accelerometer ac, TemperatureHumidity th, Barometer ba, Camera ca ) {
			lightSensor = li;
			accelSensor = ac;
			tempHumiSensor = th;
			temp2Sensor = ba;
			camera = ca;

			channels = new Channel[] { t1, t2, tdiff, hum, light, motion };

			//setup event handlers for reading sensor modules
			tempHumiSensor.MeasurementComplete += new TemperatureHumidity.MeasurementCompleteEventHandler( temperatureHumidity_MeasurementComplete );
			temp2Sensor.MeasurementComplete += new Barometer.MeasurementCompleteEventHandler( barometer_MeasurementComplete );
			accelSensor.EnableThresholdDetection( Config.movementSensitivity, true, true, true, false, false, true );
			accelSensor.ThresholdExceeded += new Accelerometer.ThresholdExceededEventHandler( accelerometer_ThresholdExceeded );
			camera.PictureCaptured += new Camera.PictureCapturedEventHandler( pictureCaptured );
		}

		/// <summary>
		/// Gets the name of each channel, in the order that they are compiled into datapoints
		/// </summary>
		/// <returns>all names</returns>
		public string[] getChannelNames() {
			string[] names = new string[channels.Length];
			for(int i = 0; i < channels.Length; i++) {
				names[i] = channels[i].name;
			}
			return names;
		}

		/// <summary>
		/// Request or retrieve measurements from all sensors and store cleaned values in
		/// class members
		/// </summary>
		/// <returns>The cleaned class memebers representing each sensor's datum</returns>
		public string[] readNumericSensors() {
			tempHumiSensor.RequestMeasurement();
			temp2Sensor.RequestMeasurement();
			light.currentValue = lightSensor.ReadLightSensorPercentage();
			tdiff.currentValue = t2.currentValue - t1.currentValue;

			return getDataPt();
		}

		private string[] getDataPt() {
			var cleanData = new String[channels.Length];
			for(int i = 0; i < channels.Length; i++) {
				Channel ch = channels[i];
				cleanData[i] = Utility.roundToDp(ch.currentValue, ch.precision);
			}
			//motion is event driven, so reset to avoid over-reporting
			motion.currentValue = 0;
			return cleanData;
		}

		public byte[] readBinarySensors() {
			camera.TakePicture();
			return image;
		}

		void accelerometer_ThresholdExceeded( Accelerometer sender ) {
			Debug.Print( "Hive is moving!" );
			motion.currentValue = 1;
		}

		void barometer_MeasurementComplete( Barometer sender, Barometer.SensorData sensorData ) {
			double pres = sensorData.Pressure;
			double temp2 = sensorData.Temperature;
			//Debug.Print( "Pressure: " + pres );
			//Debug.Print( "TempBaro: " + t2 );
			t2.currentValue = temp2;
		}

		void temperatureHumidity_MeasurementComplete( TemperatureHumidity sender, double temperature, double relativeHumidity ) {
			//Debug.Print( "Temp: " + temperature );
			//Debug.Print( "Humi: " + relativeHumidity );
			t1.currentValue = temperature;
			hum.currentValue = (int) relativeHumidity;
		}

		/// <summary>
		/// Handler for when a picture is taken: attempts to POST it to the web
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="picture">raw pic data</param>
		void pictureCaptured( Camera sender, GT.Picture picture ) {
			byte[] cameraSnapshot = picture.PictureData;
			if(cameraSnapshot != null) {
				image = cameraSnapshot;
			} else {
				Debug.Print( "Null pic data" );
			}
		}

	}
}
