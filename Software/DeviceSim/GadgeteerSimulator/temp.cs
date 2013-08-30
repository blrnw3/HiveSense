
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using CommandLine;

namespace GadgeteerSimulator {

/// <summary>
/// Simulator for testing the API by sending data points (live sim or from a log)
/// Source of HTTP Send and CreateRequest methods: MSDN C# documentation
/// Source of Command-line arg parsing: http://commandline.codeplex.com/
/// </summary>
class Program2 {

	private static readonly string[] channelNames = {
		"temp1", "humi", "motion", "light", "temp2", "mass", "tempdiff" };

	const string APIkey = "blr2013ucl";

	static string RESTurlPublic = "http://hivesensenodejs.azurewebsites.net";
	static string RESTurlLocal = "http://localhost:1337";
	static string RESTurl;

	static bool bigRandom;
	static Random r = new Random();

	static void Main(string[] args) {
		var options = new Options();

		if (CommandLine.Parser.Default.ParseArguments(args, options)) {

			RESTurl = options.isLocal ? RESTurlLocal : RESTurlPublic;

			if (options.isImage) {
				RESTurl += "/image";
				Send(getImageBytesForPOST(), RESTurl);
			} else if (options.save) {
				bigRandom = true;
				saveDataPoints(options);
			} else {
				RESTurl += "/feed";
				bigRandom = false;

				if (options.isHistory) {
					compileMultipleDatapoints();
				} else {
					int wait = options.interval;
					int limit = options.number;
					int cnt = 1;

					Console.Out.WriteLine("Beginning hivesense Gadgeteer" +
						" simulation.\nSystem will auto-send a total of " +
						limit + " random data points every " + wait + "s");

					while (cnt <= limit) {
						Console.Out.WriteLine("Transmitting random data point "
							+ cnt + " of " + limit);
						compileSingleDataPoint();
						if (cnt < limit) {
							Console.Out.WriteLine("waiting " + wait + "s");
							System.Threading.Thread.Sleep(wait * 1000);
						}
						cnt++;
					}
					Console.Out.WriteLine("All posts sent");
				}
			}
		} else {
			Console.Out.WriteLine("WTF?");
		}
	}

	

	static string randomSensorValue(int minInt, int maxInt, int precision) {
		double timestamp = System.DateTime.UtcNow.Hour * 60 +
			System.DateTime.UtcNow.Minute;
		double baseValue = minInt + (maxInt - minInt) / 1440.0 * timestamp;
		//Increase by small randomly-decided fraction and clean up.
		int smallness = bigRandom ? 8 : 25;
		return Math.Round(baseValue * 
			(r.NextDouble() / smallness + 1), precision).ToString();
	}

	static string[] getRandomDatapoint() {
		var fargs = new string[channelNames.Length];
		fargs[0] = randomSensorValue(20, 42, 1);
		fargs[1] = randomSensorValue(30, 90, 0);
		//random boolean, biased towards false
		fargs[2] = Math.Round(r.NextDouble() - 0.45).ToString();
		fargs[3] = randomSensorValue(0, 50, 0);
		fargs[4] = randomSensorValue(9, 33, 1);
		fargs[5] = randomSensorValue(89, 99, 1);
		fargs[6] = (Double.Parse(fargs[0]) - Double.Parse(fargs[4])).ToString();
		return fargs;
	}

	static void compileSingleDataPoint() {
		sendTextyPost(dataLineToAPIFormat(getRandomDatapoint(), ""));
	}
	static void compileMultipleDatapoints() {
		sendTextyPost(getHistoricalData(@"..\..\datalog.csv"));
	}

	public static byte[] getImageBytesForPOST() {
		var pic = Image.FromFile(@"C:\Users\Ben LR\Downloads\test.jpg");
		ImageConverter converter = new ImageConverter();
		return (byte[]) converter.ConvertTo(pic, typeof(byte[]));
	}
	
	private static void saveDataPoints(Options o) {
		string[] date = o.date.Split('/');
		var start = new DateTime(Int32.Parse(date[0]),
			Int32.Parse(date[1]), Int32.Parse(date[2]));

		int[] dates = new int[6];
		string[] rdp;
		int cnt = 0;

		while (cnt < o.number) {
			string line = "";
			rdp = getRandomDatapoint();

			dates[0] = start.Year;
			dates[1] = start.Month;
			dates[2] = start.Day;
			dates[3] = start.Hour;
			dates[4] = start.Minute;
			dates[5] = start.Second;

			for (int i = 0; i < dates.Length; i++) {
				line += dates[i] + ",";
			}

			for (int i = 0; i < rdp.Length; i++) {
				line += rdp[i];
				if (i < rdp.Length - 1) {
					line += ",";
				}
			}

			start = start.AddSeconds(o.interval);
			cnt++;
			Console.Out.WriteLine(line);
		}
	}
}

/// <summary>
/// Command line options for the simulator
/// Modified from docs of: http://commandline.codeplex.com/
/// </summary>
class Options2 {
	[Option('l', "local", HelpText = "Use localhost rather than WWW")]
	public bool isLocal { get;	set; }

	[Option('i', "interval", DefaultValue = 60, HelpText = "Send freq in s.")]
	public int interval { get; set; }

	[Option('n', "number", DefaultValue = 60, HelpText = "No. points to sim.")]
	public int number {	get; set; }

	[Option('h', "history", HelpText = "Load points from log (no live sim).")]
	public bool isHistory {	get; set; }

	[Option('b', "image", HelpText = "Send an image.")]
	public bool isImage { get; set; }

	[Option('s', "save", HelpText = "Save points to text file (no transmit).")]
	public bool save { get; set; }

	[Option('d', "date", HelpText = "Start date for saving data points")]
	public string date { get; set; }
}

}