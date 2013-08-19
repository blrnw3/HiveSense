$(document).ready(starter);
console.log("I am ready");

// Closure pattern for using private members (others exist:
// http://enterprisejquery.com/2010/10/how-good-c-habits-can-encourage-bad-javascript-habits-part-1/),
// but this is my favourite (good for devs coming from classical OOP languages):
// http://stackoverflow.com/questions/881515/javascript-namespace-declaration
var JavaScriptOOPdemo = new function() {
	this.publicFn = function() {
		console.log("I am private");
	};
	var privateFn = function() {
		console.log("I am private");
	};
	var privateVar = 5;
	this.publicVar = 45;
};

// Utility methods
var Util = new function() {
	/**
	 * Converts a quantity in raw seconds to one in seconds, minutes, hours, or days,
	 * depending on the value, to give a more readable output
	 * @param {type} seconds
	 * @returns {String}
	 */
	this.prettyTimeAgo = function(seconds) {
		seconds = seconds / 1000;
		if(seconds < 100) {
			return Math.round(seconds) + ' s';
		}
		var diff = Math.round( seconds / 60 );
		if(diff < 100) {
			return diff + ' mins';
		} else if(diff < 3000) {
			return Math.round(diff / 60) + ' hours';
		} else {
			return Math.round(diff / 60 / 24) + ' days';
		}
	};


	this.signedNumber = function(num) {
		var sign = (num < 0) ? '' : '+';
		var n = new Number(num);
		return sign + n.toFixed(1);
	};

	/**
	 * Convert from degrees C to degrees F
	 * @param {type} celcius value in degC
	 * @param {type} isAbsolute whether the value is absolute (e.g. difference between two temperatures)
	 * @returns {Number} degF value formatted to 1dp
	 */
	this.CtoFdegrees = function(celcius, isAbsolute) {
		var excess = isAbsolute ? 0 : 32;
		return new Number(celcius * 9 / 5 + excess).toFixed(1);
	};

};

//var db = new WindowsAzure.MobileServiceClient(
//    "https://hivesense.azure-mobile.net/",
//    "vIrGaIHVxHZICXFUHBzaPhAonCdrgx46"
//);

//Actually, Model object (or singleton class)
var Model = new function() {
	this.xivelyFeedID = 1693757499;
	this.xivelyAPIkey = "597SF7dgmQt6V5H4uf9KGmNzA52Z28KYCGHl7fkBZ8sJlc1i";

	this.sensorValues = [];
	this.sensorValuesRecent = [];
	this.sensorNames = ['temp1', 'temp2', 'temp3', 'light', 'motion', 'humi'];

	this.XivelyMappings = {
		AmbientTemp: 'temp1',
		Temperature: 'temp2',
		Light: 'light',
		IsMoving: 'motion',
		Humidity: 'humi'
	};

	this.trendArrowUnicodes = {
		level: '&#x25ac;',
		down: '&#x25bc;',
		up: '&#x25b2;'
	};

	this.thresholdsHigh = {
		humi: 75,
		temp: 40,
		light: 20
	};
	this.thresholdsLow = {
		temp: 10,
		lastMovement: 1000
	};

	this.pages = [ "home", "graphs", "history", "about" ];

	this.UPDATE_RATE_SENSORS = 30; // in secs
	this.UPDATE_RATE_HISTORY = 300; // in secs
	this.UPDATE_RATE_WEATHER = 900; // in secs

	this.localWeatherLocation = "London";

	this.currTime = 0;
	var OLD_DATA_THRESHOLD = 300; // In seconds

	function diffTime(unixTime) {
		var d = new Date();
		return d.getTime() - unixTime;
	};

	this.updated_ago = function() {
		return Util.prettyTimeAgo( diffTime(Model.currTime) );
	};

	this.isOld = function() {
		return diffTime(Model.currTime) > OLD_DATA_THRESHOLD * 1000;
	};

	this.getTrend = function(name) {
		var change = Model.sensorValues[name] - Model.sensorValuesRecent[name];
		if(change === 0) {
			return "level";
		} else {
			return (change > 0) ? "up" : "down";
		}
	};

	this.getAlarm = function(name, level) {
		if(level === "high") {
			return Model.sensorValues[name] > Model.thresholdsHigh[name];
		} else {
			return Model.sensorValues[name] < Model.thresholdsLow[name];
		}
	};
	var lastMoveTime = Number.MAX_VALUE;
	this.getMovementAlarm = function() {
		return (lastMoveTime / 1000) < Model.thresholdsLow.lastMovement;
	};

	this.isUnitMetric = true;

	this.convert = function(value, type) {
		var unitT = Model.isUnitMetric ? 'C' : 'F';
		switch(type) {
			case "temp1":
			case "temp2":
			case "temp3":
				return (Model.isUnitMetric ? new Number(value).toFixed(1) :
					Util.CtoFdegrees(value, type === "temp3")) + " " + unitT;
			case "humi":
			case "light":
				return Math.round(value) + " %";
				break;
			case "motion":
				return (value == 0) ? "Stationary" : "Moving!";
				break;
		}
		return value;
	};

	this.getLastMotion = function(dataSeries) {
		if(dataSeries === undefined) {
			return "";
		}
		for(var i = dataSeries.length -1; i >= 0; i--) {
			if(dataSeries[i].value == 1)
				break;
		}
		//console.log(i + " gives " + dataSeries[i]);
		if(i === -1) {
			return "No recent motion";
		} else {
			lastMoveTime = diffTime( Date.parse(dataSeries[i].at) );
			return "Last motion: " + Util.prettyTimeAgo(lastMoveTime) + " ago";
		}
	};

	this.getLocalWeather = function(callback) {
		console.log("wx get pt 2");
		var wxAPIsrc = '/ext/wxgrab';
		$.get(wxAPIsrc + "?place=" + Model.localWeatherLocation,
			function(data) {
				console.log("wx get pt 3");
				var backup = {
					weather: "Unknown",
					temp: -99,
					time: "HH:mm BST"
				};
				var result = (data.weather === undefined) ? backup : data;
				console.log(result);
				callback(result);
			},
			"json"
		);
	};

};

var View = new function() {
	this.isInactive = false;
	this.LEDclass = 'success';

	this.deactivate = function() {
		$('#updated-led').removeClass('badge-success');
		$('#updated-led').addClass('.badge-important');
		View.isInactive = true;
		View.LEDclass = 'important';
		return true;
	};
	this.activate = function() {
		$('#updated-led').removeClass('.badge-important');
		$('#updated-led').addClass('.badge-success');
		View.isInactive = false;
		View.LEDclass = 'success';
		return;
	};

	this.flashTime = function() {
		$('#updated-led').toggleClass('badge-'+View.LEDclass + ' badge-warning');
	};

	this.updateSensorBlocks = function() {
		for(var i = 0; i < Model.sensorNames.length; i++) {
			var name = Model.sensorNames[i];
			$("#sensor-value-" + name).html( Model.convert( Model.sensorValues[name], name ) );
			if(Model.sensorValuesRecent[name] !== undefined && name !== "motion") {
				//console.log("trends being set");
				var trend = Model.getTrend(name);
				var e = $("#sensor-trend-" + name);
				e.attr("class", "trend-arrow arrow-" + trend);
				e.html(Model.trendArrowUnicodes[trend]);
			}
		}
		$("#sensor-value-temp3").html( Model.convert( Util.signedNumber(Model.sensorValues["temp3"]), "temp1") );
	};

	function getLED(isBad) {
		var colour = isBad ? "Red" : "Green";
		return 'img/LED_' +colour+'.png';
	}

	this.updateAlarms = function() {
		var keysH = Object.keys(Model.thresholdsHigh);
		for(var i = 0; i < keysH.length; i++) {
			$("#alarm-"+keysH[i]+"-high").attr('src', getLED(Model.getAlarm(keysH[i], "high")));
		}
		$("#alarm-temp-low").attr('src', getLED( Model.getAlarm("temp", "low") ));
		$("#alarm-moving").attr('src', getLED( Model.getMovementAlarm() ));
	};

	this.updateCamera = function() {
		$('#camera').attr('src', 'http://nw3weather.co.uk/CP_Solutions/MscProject/camLatest.bmp?' + Model.currTime);
	};
	this.updateTime = function() {
		$('#updated-date').html($.format.date(Model.currTime, "HH:mm:ss UTC, ddd dd MMMM yyyy"));
	};
	this.updateAgo = function() {
		$('#updated-ago').html( Model.updated_ago() );
	};
	this.updateLastMotion = function(value) {
		$('#sensor-trend-motion').html( value );
	};

	this.updateWeather = function(wx) {
		console.log("wx get pt 4");
		$('#weather-weather').html( wx.weather + ", " + Model.convert(wx.temp, "temp1") );
		$('#weather-time').html(wx.time);
	};

	function switchPage(target) {
//		$("#about").toggle();
		for(var i = 0; i < Model.pages.length; i++) {
			if(Model.pages[i] === target) {
				$("#"+target).show(0);
				$("#li-"+target).attr("class", "active");
			} else {
				$("#"+Model.pages[i]).hide(0);
				$("#li-"+Model.pages[i]).attr("class", "");
			}
		}
	};

	this.bindEvents = function() {
		console.log("binding events");
		$('#unit_EU').bind('click', function() {
			Model.isUnitMetric = true;
			View.updateSensorBlocks();
		});
		$('#unit_US').bind('click', function() {
			Model.isUnitMetric = false;
			View.updateSensorBlocks();
		});

		for(var i = 0; i < Model.pages.length; i++) {
			//Use closure to bind loop var (i) to each listener, i.e. keep i in scope for the clickListener function
			//Source: http://stackoverflow.com/questions/13227360/javascript-attach-events-in-loop?lq=1
			(function(i) {
				$("#li-"+Model.pages[i]).click( function() {
					//console.log(Model.pages[i] + " bado");
					switchPage(Model.pages[i]);
				});
			}(i));
		}

		$("[data-toggle='tooltip']").tooltip();
	};

};

var Controller = new function() {
	var count = 0;

	this.boot = function() {
		View.bindEvents();
		Controller.runUpdater();
	};

	//Needs to be public for the setTimeout to be able to call it
	this.runUpdater = function() {

		if(count % Model.UPDATE_RATE_SENSORS === 0) {
			getNewData();
			getRecentHistory();
		}
		if(count % Model.UPDATE_RATE_WEATHER === 0) {
			View.updateWeather();
		}
		if(count % Model.UPDATE_RATE_HISTORY === 1) {
			getHistory();
		}

		View.updateAgo();
		count++;

		setTimeout('Controller.runUpdater()', 1000);
	};

	//Source of some Xively API: http://xively.github.io/xively-js/tutorial/
	function getNewData() {
		// Get datastream data from Xively
		View.flashTime();

		xively.feed.get(Model.xivelyFeedID, function(feed) {

			//console.log("assigning recent vals");
			for(var i = 0; i < feed.datastreams.length; i++) {
				var name = Model.XivelyMappings[feed.datastreams[i].id];
				if(true || Object.keys(Model.sensorValues).length > 0) {
					Model.sensorValuesRecent[name] = Model.sensorValues[name];
				}
				Model.sensorValues[name] = feed.datastreams[i].current_value;
			}
			Model.sensorValuesRecent["temp3"] = Model.sensorValues['temp3'];
			Model.sensorValues["temp3"] = Model.sensorValues['temp1'] - Model.sensorValues['temp2'];

//			console.log(Model.sensorValuesRecent);
//			console.log(Model.sensorValues);

			Model.currTime = Date.parse(feed.updated);

			View.updateSensorBlocks();
			View.updateAlarms();
			View.updateCamera();
			View.updateTime();
			View.updateAgo();

			//Make UI changes when the data dies or resurects
			if(Model.isOld()) {
				if(!View.isInactive) {
					View.deactivate();
				}
			} else if(View.isInactive) {
				View.activate();
			}

			View.flashTime();
		});

	};

	function buildDataSeries(jsonFeed) {
		var series = [];
		for(var i = 0; i < jsonFeed.datastreams.length; i++) {
			var name = Model.XivelyMappings[jsonFeed.datastreams[i].id];
			series[name] = jsonFeed.datastreams[i].datapoints;
		}
		return series;
	}
	function buildOptionsForDataFeed(interval, duration) {
		var options = {
			limit: 1000,
			interval: interval,
			duration: duration
			//end: new Date().toISOString()
		};
		return options;
	}

	function getRecentHistory() {
		xively.feed.history(Model.xivelyFeedID, buildOptionsForDataFeed(60, '3hours'), function(feed) {
			var series = buildDataSeries(feed);
			Graphs.plotTempGraph(series['temp1'], series['temp2']);
			Graphs.plotHumiGraph(series['humi']);
			Graphs.plotLightGraph(series['light']);
			View.updateLastMotion( Model.getLastMotion(series['motion']) );
		});
	}

	function getHistory() {
		xively.feed.history(Model.xivelyFeedID, buildOptionsForDataFeed(900, '2days'), function(feed) {
			console.log("Feed of past history coming up...");
//			console.log(buildDataSeries(feed));
			Graphs.plotMainGraph(buildDataSeries(feed));
		});
	}

};

//Classless 'booter' function
function starter() {
	xively.setKey(Model.xivelyAPIkey);
	Controller.boot();
}