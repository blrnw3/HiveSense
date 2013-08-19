<?php
echo time();
$noaaMetar = file('http://weather.noaa.gov/pub/data/observations/metar/stations/EGLL.TXT');
if($noaaMetar !== false) file_put_contents("METAR.txt", $noaaMetar[1]);
var_dump($noaaMetar);
?>