<?php

$placeName = isset($_GET['place']) ? $_GET['place'] : 'London';

$xml = getXml('http://api.wunderground.com/api/46272bfe75051ab1/conditions/q/UK/' . $placeName . '.xml');
//$xmlTE = microtime(true);
if ($xml !== false) { //grab the data if available
	if (isset($xml->error)) {
		fail();
	}
	$weather = (string) $xml->current_observation->weather;
	$time = (int) $xml->current_observation->observation_epoch;
	$temp = (int) $xml->current_observation->temp_c;

	echo json_encode(array($time, $weather, $temp));
} else {
	fail();
}
//$xmlTE2 = microtime(true);


function fail() {
	die(json_encode(false));
}

/**
 * Safely accesses a url (using a timeout) and gets the contents
 * @param string $url to parse
 * @param int $timeout [=5] in seconds
 * @return full contents string, or false on failure
 */
function urlGrab($url, $timeout = 5) {
	$ctx = stream_context_create( array( 'http'=> array('timeout' => $timeout) ) );
	file_get_contents( $url, false, $ctx );
}

/**
  Get an XML document over http with a short timeout
 * @param string $url
 * @return boolean true on success, false otherwise
 * @author Modified http://stackoverflow.com/questions/4867086/timing-out-a-script-portion-and-allowing-the-rest-to-continue
 */
function getXml($url) {
	return simplexml_load_string( urlGrab($url) );
}


?>