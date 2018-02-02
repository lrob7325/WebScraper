using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebScraper.Models
{
    public class HomeModel
    {
        public List<string> State { get; set; }
        public Dictionary<string, List<string>> Cities { get; set; }
    }

    public class Coordinates
    {
        public string city { get; set; }
        public string state { get; set; }
        public string lat { get; set; }

        public string lng { get; set; }
    }

    public class WeatherData
    {
        public string jobID { get; set; }
        public string status { get; set; }
        public string desc { get; set; }
        public string period { get; set; }
        public string shortDesc { get; set; }
        public string temp { get; set; }
    }
}