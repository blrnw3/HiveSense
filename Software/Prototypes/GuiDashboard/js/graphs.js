/**
 /**
 * Graphing Module for HiveSense Web Application
 * Graphing API Source: https://github.com/flot/flot/blob/master/API.md
 * Some code comes from examples published by the API makers.
 */

//var d = [[0, 315.71], [10000000, 345], [20000000, 324]];
//var e = [[0, 215.71], [10000000, 245], [20000000, 224]];

var Graphs = new function() {

	function cleanDataSeries(series, spikeThreshold) {
		if(series === undefined) {
			return [];
		}
		var cleanData = [];
		for(var i = 0; i < series.length; i++) {
			if(i > 0 && Math.abs( series[i].value - series[i-1].value ) > spikeThreshold) {
				series[i] = series[i-1];
			}
			cleanData[i] = [new Date(series[i].at).getTime(), series[i].value];
		}
		return cleanData;
	}

	this.plotMainGraph = function(data) {
		//console.log(cleanDataSeries(data["temp1"], 10));
		plotDashboardGraph(
			[ {
				data: cleanDataSeries(data["temp1"], 1000),
			color: "#f32",
				label: "T-in"
		},
			{
				data: cleanDataSeries(data["humi"], 500),
			color: "#3f2",
				label: "Humi"
			} ],
			"main",
			["#ddd", "#fff"]
		);
	};

	this.plotTempGraph = function(dat1, dat2) {
		//console.log(cleanDataSeries(dat1, 10));
		plotDashboardGraph(
			[ {
				data: cleanDataSeries(dat1, 100),
				color: "#f32",
				label: "T-in"
				},
			{
				data: cleanDataSeries(dat2, 100),
				color: "#c74",
				label: "T-out"
			} ],
			"temp",
			["#fdd", "#fff"]
		);
	};

	this.plotHumiGraph = function(dat) {
		plotDashboardGraph(
			[ {
				data: cleanDataSeries(dat, 500),
				color: "#3f2",
				label: "Humi"
			} ],
			"humi",
			["#dfd", "#fff"]
		);

	};
	this.plotLightGraph = function(dat) {
		plotDashboardGraph(
			[ {
				data: cleanDataSeries(dat, 800),
				color: "#ff2",
				label: "Light"
			} ],
			"light",
			["#ffa", "#dd9"]
		);
	};

	function plotDashboardGraph(data, id, gradient) {

		$.plot("#sensor-graph-"+id,
			data,
			{//options
				xaxis: {
					mode: "time", // null or "time"
					timeformat: "%H",
					minTickSize: [1, 'hour'] // number or array, e.g. [1, "hour"] for time mode
				},
				series: {
					points: {
						show: false,
						radius: 3
					},
					lines: {
						lineWidth: 2,
						show: true
					}
				},
				legend: {
					show: true,
					position: "nw", //ne,nw,se,sw
					backgroundOpacity: 0.3 //0-1
				},
				grid: {
					backgroundColor: { colors: gradient },
					borderColor: "#a99"
				}
			}
		);
	};

	this.plotGenericGraphShowingAllOptionsOfFlotLibrary = function(dat1, dat2) {

		$.plot("#sensor-graph-temp",
			[//data
				{
					data: cleanDataSeries(dat1),
					color: "#f32",
					label: "T-in",
		//			lines: specific lines options
		//			bars: specific bars options
		//			points: specific points options
		//			xaxis: number
		//			yaxis: number
					clickable: true,
					hoverable: true
		//			shadowSize: number
		//			highlightColor: color or number
				},
				{
					data: cleanDataSeries(dat2),
					color: "#c74",
					label: "T-out"
				}

			],
			{//options
				xaxis: {
		//			show: null or true/false
		//			position: "bottom" or "top" or "left" or "right"
		//			color: null or color spec
		//			font: null or font spec object

					mode: "time", // null or "time"
					timeformat: "%H",
		//			timezone: null, "browser" or timezone (only makes sense for mode: "time")
		//			monthNames: null or array of size 12 of strings
		//			dayNames: null or array of size 7 of strings
		//
		//			min: null or number
		//			max: null or number
		//			autoscaleMargin: null or number
		//
		//			transform: null or fn: number -> number
		//			inverseTransform: null or fn: number -> number
		//
		//			ticks: 4 //null or number or ticks array or (fn: axis -> ticks array)
		//			tickSize: number or array
					minTickSize: [1, 'hour'] // number or array, e.g. [1, "hour"] for time mode
		//			tickFormatter: (fn: number, object -> string) or string
		//			tickDecimals: null or number
		//			tickLength: null or number
		//			alignTicksWithAxis: null or number
		//			tickColor: null or color spec
		//
		//			labelWidth: null or number
		//			labelHeight: null or number
		//			reserveSpace: null or true
				},
				series: {
					points: {
						show: false,
						radius: 3
		//				symbol: "circle" or function
					},
					lines: {
						lineWidth: 2,
						show: true
		//				steps: boolean
					}
					//lines, points, bars: {
						//show: boolean
						//lineWidth: number
						//fill: boolean or number
						//fillColor: null or color/gradient
					//}
		//
		//			lines, bars: {
		//				zero: boolean
		//			}
		//
		//			bars: {
		//				barWidth: number
		//				align: "left", "right" or "center"
		//				horizontal: boolean
		//			}
		//
		//			shadowSize: number
		//			highlightColor: color or number
				},
				legend: {
					show: true,
		//			labelFormatter: null or (fn: string, series object -> string)
		//			labelBoxBorderColor: color
		//			noColumns: number
					position: "nw", //ne,nw,se,sw
		//			margin: number of pixels or [x margin, y margin]
		//			backgroundColor: null or color
					backgroundOpacity: 0.3 //0-1
		//			container: null or jQuery object/DOM element/jQuery expression
		//			sorted: null/false, true, "ascending", "descending", "reverse", or a comparator
				},
				grid: {
		//			show: boolean
		//			aboveData: boolean
		//			color: color
					backgroundColor: { colors: ["#fee", "#eef"] }
		//			margin: number or margin object
		//			labelMargin: number
		//			axisMargin: number
		//			markings: array of markings or (fn: axes -> array of markings)
		//			borderWidth: number or object with "top", "right", "bottom" and "left" properties with different widths
		//			borderColor: color or null or object with "top", "right", "bottom" and "left" properties with different colors
		//			minBorderMargin: number or null
		//			clickable: boolean
		//			hoverable: boolean
		//			autoHighlight: boolean
		//			mouseActiveRadius: number
				},
				interaction: {
					//redrawOverlayInterval: number or -1
				}
			}
		);
	};
};
