using System;
using Microsoft.SPOT;

using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;

namespace HiveSenseV2 {
	/// <summary>
	/// Handler for the SD card module
	/// </summary>
	class SdHandler {

		const string hivesenseDirectory = "hivesense\\";

		readonly static string configFilePath = hivesenseDirectory + "config.xml";

		/// <summary>
		/// Temporary data store (buffer) for when no network is avaialble. Flushed to web when network (re)joined
		/// </summary>
		public readonly static string datalogFilePathBuffer = hivesenseDirectory + "datalog.csv";
		/// <summary>
		/// Never overwritten or read - just for purposes of keeping data in one place for the permanent record
		/// </summary>
		public readonly static string datalogFilePathPerm = hivesenseDirectory + "datalogAlways.csv";

		/// <summary>
		/// Date fields to save in the permanent log
		/// </summary>
		/// <remarks>These will appear as date headings for the first line of the logfile</remarks>
		private static readonly string[] dateVariables = { "year", "month", "day", "hour", "minute", "second" };

		/// <summary>
		/// Whether the temporary sensor data buffer logfile exists
		/// </summary>
		/// <remarks>This log is deleted after succesful http transmission of logged data</remarks>
		public bool datalogBufferExists { get; set; }

		SDCard sdCard;
		APIconnector api;
		SensorsHandler sensorHandle;

		/// <summary>
		/// Sets up the SDcard module and if present, configures the card's filesystem
		/// so the data logs are present.<br />
		/// If present, the config file will be read and the settings loaded into memory
		/// </summary>
		/// <param name="sdCard">SDcard module</param>
		public SdHandler( SDCard sdCard, SensorsHandler sh) {
			this.sdCard = sdCard;
			this.sensorHandle = sh;

			sdCard.SDCardMounted += new SDCard.SDCardMountedEventHandler( sdCard_SDCardMounted );
			sdCard.SDCardUnmounted += new SDCard.SDCardUnmountedEventHandler( sdCard_SDCardUnmounted );
			//Card may already be in device, in which case mounted event not called 
			if(sdCard.IsCardMounted) {
				cardSetup();
				Debug.Print( "SD card found" );
			} else {
				Debug.Print( "No SD card found" );
			}
		}

		void sdCard_SDCardUnmounted( SDCard sender ) {
			Debug.Print( "SD unmount event" );
		}

		void sdCard_SDCardMounted( SDCard sender, GT.StorageDevice SDCard ) {
			Debug.Print( "SD mount event" );
			cardSetup();
		}

		/// <summary>
		/// Tries to write an <c>array</c> of data fields to the SDcard as a CSV, prepending datetime information
		/// </summary>
		/// <param name="csv">Data fields to be CSV-ified</param>
		/// <param name="filename">Name of the file to which to write</param>
		/// <param name="currTime">Timestamp to use as the line's prependary information</param>
		public void writeDataLineToSDcard( string[] csv, string filename, DateTime currTime ) {
			if(!sdCard.IsCardMounted) {
				Debug.Print( "SD card not mounted. Cannot proceed" );
				return;
			}
			try {
				System.IO.FileStream sdOut = sdCard.GetStorageDevice().Open( filename, System.IO.FileMode.Append, System.IO.FileAccess.Write );
				//Attach date information to the line to be written
				string fullCsvLine = currTime.ToString( "yyyy,MM,dd,HH,mm,ss," ) + Utility.csvJoin( csv ) + "\n";
				byte[] buffer = System.Text.Encoding.UTF8.GetBytes( fullCsvLine );
				sdOut.Write( buffer, 0, buffer.Length );
				Debug.Print( fullCsvLine + "data written" );
				sdOut.Close();
				sdOut.Dispose();
			} catch {
				Debug.Print( "SDcard write FAIL" );
			}
		}

		/// <summary>
		/// Transmitts data from a logfile to an HTTP server; this is done in manageable batches to reduce errors.<br />
		/// WARNING: The logfile buffer is deleted upon succesful transmission
		/// </summary>
		public void transmitSDloggedData() {
			if(!sdCard.IsCardMounted) {
				Debug.Print( "SD card not mounted. Cannot proceed" );
				return;
			}
			if(!datalogBufferExists) {
				Debug.Print( "No historical data to read from log" );
				return;
			}
			System.IO.FileStream sdIn = null;
			try {
				sdIn = sdCard.GetStorageDevice().OpenRead( datalogFilePathBuffer );
				Debug.Print( "Log len: " + sdIn.Length );

				if(sdIn.Length < 100) {
					Debug.Print( "No significant logged data to be sent" );
				} else {
					var line = new System.Text.StringBuilder(50);
					//Number of data points to transmit per update cycle, as determined by stress testing
					//Too many causes local memory problems, and may overwhelm the server
					int chunkSize = 100;
					string[] lines = new string[chunkSize];
					int pointer = 0;

					int b;
					while( pointer < chunkSize && ( b = sdIn.ReadByte()) != -1 ) {
						char next = (char) b;
						if(next == '\n' && line.Length > 1) {
							lines[pointer] = line.ToString();

							if(sensorHandle.getChannelNames().Length + 6 != line.ToString().Split(',').Length) {
								Debug.Print( "Buffer contains unexpected data - number of logged variables is not correct" );
								Debug.Print( "Deleting buffer now" );
								disposeOfSD( sdIn );
								deleteBufferLog();
								return;
							}

							pointer++;
							line.Clear();
						} else {
							line.Append( next );
						}
					}

					Debug.Print( "first line of buffer: " + lines[0] );
					Debug.Print( "last line available: " + lines[pointer-1] );
					if( api.sendHistoricalData(lines) ) {
					
						if(pointer == chunkSize) {
							//more lines in buffer so delete succesfully-transferred lines from buffer
							// through elaborate copying process
							string tempPath = hivesenseDirectory + "temp.csv";
							//write rest of buffer to temp
							writeBuffer( tempPath, sdIn );
							//delete entire buffer
							deleteBufferLog();
							//write temp to newly-empty buffer
							writeBuffer( datalogFilePathBuffer, sdCard.GetStorageDevice().OpenRead( tempPath ) );
							datalogBufferExists = true;
							//delete temp
							sdCard.GetStorageDevice().Delete( tempPath );
						} else {
							disposeOfSD( sdIn );
							deleteBufferLog();
						}
					}
				}

			} finally {
				if(sdIn != null) {
					disposeOfSD( sdIn );
				}
			}
		}

		private void writeBuffer( string bufferWrite, System.IO.FileStream bufferRead ) {
			using(System.IO.FileStream bufferOut = sdCard.GetStorageDevice().OpenWrite( bufferWrite )) {
				short bufferStreamSize = 1024;
				byte[] bb = new byte[bufferStreamSize];
				int bytesRead = 0;
				while((bytesRead = bufferRead.Read( bb, 0, bufferStreamSize )) != 0) {
					bufferOut.Write( bb, 0, bytesRead );
				}
				disposeOfSD( bufferOut );
				disposeOfSD( bufferRead );
			}
		}

		private void disposeOfSD(System.IO.FileStream sdIn) {
			sdIn.Close();
			sdIn.Dispose();
		}

		private void deleteBufferLog() {
			sdCard.GetStorageDevice().Delete( datalogFilePathBuffer );
			datalogBufferExists = false;
		}

		void cardSetup() {
			try {
				// create hivesense directory if non-extant
				string[] sdDirectories = sdCard.GetStorageDevice().ListDirectories( "\\" );
				bool hivesenseDirectoryExists = false;
				foreach(string dir in sdDirectories) {
					if(dir == hivesenseDirectory) {
						Debug.Print( dir );
						hivesenseDirectoryExists = true;
						break;
					}
				}
				if(!hivesenseDirectoryExists) {
					sdCard.GetStorageDevice().CreateDirectory( hivesenseDirectory );
				}

				//create logfile if not already present
				string[] sdFiles = sdCard.GetStorageDevice().ListFiles( hivesenseDirectory );
				bool datalogExists = false;
				foreach(string file in sdFiles) {
					Debug.Print( file );
					if(file.ToLower() == datalogFilePathPerm.ToLower()) {
						datalogExists = true;
					} else if(file.ToLower() == datalogFilePathBuffer.ToLower()) {
						datalogBufferExists = true;
					}
				}
				//write some helpful starter column headings into the permanent log
				if(!datalogExists) {
					sdCard.GetStorageDevice().WriteFile( datalogFilePathPerm,
						System.Text.Encoding.UTF8.GetBytes( Utility.csvJoin( dateVariables ) +
							"," + Utility.csvJoin( sensorHandle.getChannelNames() ) + '\n' ) 
					);
					Debug.Print( "Datalogpermanent not found. CREATED on SD at " + datalogFilePathPerm );
				} else {
					Debug.Print( "Datalogpermanent exists" );
				}

				getConfig();

			} catch {
				Debug.Print( "BAD SDcard. Remount needed" );
			}
		}

		void getConfig() {
			try {
				var xml = new Xml( sdCard.GetStorageDevice().ReadFile( configFilePath ) );
				Config.getXmlSettings( xml );
				api = new APIconnector( Config.APIendpoint, sensorHandle );

			} catch {
				Debug.Print( "SDcard read FAIL" );
			}
		}

	}
}
