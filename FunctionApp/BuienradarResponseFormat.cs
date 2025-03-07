using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuienradarResponseFormat
{
    // Generated with https://json2csharp.com/

    public class BuienradarResponse
    {
        [JsonProperty("$id")]
        public string id { get; set; }
        public Buienradar buienradar { get; set; }
        public Actual actual { get; set; }
        public Forecast forecast { get; set; }
    }

    public class Actual
    {
        [JsonProperty("$id")]
        public string id { get; set; }
        public string actualradarurl { get; set; }
        public DateTime sunrise { get; set; }
        public DateTime sunset { get; set; }
        public List<Stationmeasurement> stationmeasurements { get; set; }
    }

    public class Buienradar
    {
        [JsonProperty("$id")]
        public string id { get; set; }
        public string copyright { get; set; }
        public string terms { get; set; }
    }

    public class Fivedayforecast
    {
        [JsonProperty("$id")]
        public string id { get; set; }
        public DateTime day { get; set; }
        public string mintemperature { get; set; }
        public string maxtemperature { get; set; }
        public int mintemperatureMax { get; set; }
        public int mintemperatureMin { get; set; }
        public int maxtemperatureMax { get; set; }
        public int maxtemperatureMin { get; set; }
        public int rainChance { get; set; }
        public int sunChance { get; set; }
        public string windDirection { get; set; }
        public int wind { get; set; }
        public double mmRainMin { get; set; }
        public double mmRainMax { get; set; }
        public string weatherdescription { get; set; }
        public string iconurl { get; set; }
        public string fullIconUrl { get; set; }
    }

    public class Forecast
    {
        [JsonProperty("$id")]
        public string id { get; set; }
        public Weatherreport weatherreport { get; set; }
        public Shortterm shortterm { get; set; }
        public Longterm longterm { get; set; }
        public List<Fivedayforecast> fivedayforecast { get; set; }
    }

    public class Longterm
    {
        [JsonProperty("$id")]
        public string id { get; set; }
        public DateTime startdate { get; set; }
        public DateTime enddate { get; set; }
        public string forecast { get; set; }
    }

    public class Shortterm
    {
        [JsonProperty("$id")]
        public string id { get; set; }
        public DateTime startdate { get; set; }
        public DateTime enddate { get; set; }
        public string forecast { get; set; }
    }

    public class Stationmeasurement
    {
        [JsonProperty("$id")]
        public string id { get; set; }
        public int stationid { get; set; }
        public string stationname { get; set; }
        public double lat { get; set; }
        public double lon { get; set; }
        public string regio { get; set; }
        public DateTime timestamp { get; set; }
        public string weatherdescription { get; set; }
        public string iconurl { get; set; }
        public string fullIconUrl { get; set; }
        public string graphUrl { get; set; }
        public string winddirection { get; set; }
        public double temperature { get; set; }
        public double groundtemperature { get; set; }
        public double feeltemperature { get; set; }
        public double windgusts { get; set; }
        public double windspeed { get; set; }
        public int windspeedBft { get; set; }
        public double humidity { get; set; }
        public double precipitation { get; set; }
        public double sunpower { get; set; }
        public double rainFallLast24Hour { get; set; }
        public double rainFallLastHour { get; set; }
        public int winddirectiondegrees { get; set; }
        public double? airpressure { get; set; }
        public double? visibility { get; set; }
    }

    public class Weatherreport
    {
        [JsonProperty("$id")]
        public string id { get; set; }
        public DateTime published { get; set; }
        public string title { get; set; }
        public  string summary { get; set; }
        public string text { get; set; }
        public string author { get; set; }
        public string authorbio { get; set; }
    }
}