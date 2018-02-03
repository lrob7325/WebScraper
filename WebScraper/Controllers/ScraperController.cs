using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebScraper.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using WebScraper.Global;
using HtmlAgilityPack;
using System.Threading.Tasks;
using System.Threading;

namespace WebScraper.Controllers
{
    public class ScraperController : ApiController
    {
        #region Properties
        private string curJobID;


        #endregion

        /// <summary>
        /// Loops through every state for their cities and creates a thread
        /// </summary>
        /// <param name="state"></param>
        /// <param name="method"></param>
        public void Get(string state, string method, string dummy, string test)
        {
            Thread thread = new Thread(() =>
                ScrapeAll(state)

            );
            thread.Start();
        }
        public void ScrapeAll(string state)
        {
            List<string> cities = GlobalServices.theDictionary.Where(x => x.Key == state).First().Value;

            foreach (var city in cities)
            {
                api credentials = new api { state = state, city = city };

                Thread thread = new Thread(() =>
                    RunJob(credentials)
                    );

                GlobalServices.lstThread.Add("scrapeall|" + city + ", " + state, thread);
                thread.Start();
            }
        }

        public void Get(string state, string city, string jobID)
        {
            curJobID = jobID;
            api credentials = new api { state = state, city = city, jobID = jobID };
                Thread thread = new Thread(() =>
                    RunJob(credentials)
                    );

                GlobalServices.lstThread.Add(jobID + "|" + city + ", " + state, thread);
                thread.Start();
        }

        public object Get(string jobID, string method)
        {
            Thread theThread = GlobalServices.lstThread.Where(x => x.Key.Split('|')[0] == jobID).First().Value;

            return theThread.ThreadState == ThreadState.Stopped ? "Completed" : theThread.ThreadState.ToString();
        }

        /// <summary>
        /// Gets the status of all threads that are currently running
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        // GET api/<controller>/5
        [HttpGet]
        public async Task<object> Get(string state)//(HttpRequestMessage value)
        {
            return await GetStatus(state);
        }

        private async Task<List<string>> GetStatus(string state)
        {
            string data = string.Empty;
            List<string> html = new List<string>();

            try
            {

                if (state.Equals("0"))
                {
                    html.Add("No jobs are running");
                    return html;
                }

                var tmpThread = GlobalServices.lstThread.Where(x => x.Key.Contains(state)).ToList()
                    .OrderByDescending(a=>a.Value.ThreadState.ToString())
                    .ThenBy(b=>b.Key.Split('|')[1]).ToList();

                foreach (var d in tmpThread)
                {
                    html.Add("<br>" + d.Key.Split('|')[1] + " Status: " + (d.Value.ThreadState == ThreadState.Stopped ? "Completed" : d.Value.ThreadState.ToString()));
                }
                if (html.Count.Equals(0))
                { html.Add("No jobs are running"); }

                return html;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return html;
        }
        /// <summary>
        /// Retrieves all cities for the state the given state
        /// </summary>
        /// <param name="st"></param>
        /// <returns></returns>
        private List<string> GetCities(string st)
        {
            return GlobalServices.theDictionary.Where(x => x.Key == st).First().Value.ToList();
        }

        /// <summary>
        /// Depending on the method being used, the api will retieve will receive a list of cities status and results
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        // POST api/<controller>
        [HttpPost]
        public async Task<object> Post(HttpRequestMessage value)
        {
            return await PostJob(value);
        }

        private async Task<object> PostJob(HttpRequestMessage value)
        {
            JArray result = new JArray();
            JResult jresult = new JResult();
            string data = string.Empty;

            try
            {
                data = value.Content.ReadAsStringAsync().Result;

                if (String.IsNullOrEmpty(data))
                { return null; }

                api credentials = JsonConvert.DeserializeObject<api>(data);

                switch (credentials.method)
                {
                    //Resets lists
                    case "reset":
                        GlobalServices.lstJobs.Clear();
                        GlobalServices.lstThread.Clear();
                        return string.Empty;
                        //Gets cities
                    case "states":
                        return GetCities(credentials.state);                        
                        //Gets results
                    case "results":
                        //List<WeatherData> response = GlobalServices.lstJobs.Where(x => x.Key == credentials.jobID).First().Value;
                        string html = "";
                        var newList = GlobalServices.lstJobs.ToList();
                        foreach (var job in newList)
                        {
                            int counter = 1;
                            foreach (var weather in job.Value)
                            {
                                if (counter.Equals(1))
                                {
                                    html += "<tr><td><b><u>" + job.Key.Split('|')[1] + "</u></b></td></tr>";
                                }

                                html += "<tr><td><b>" + weather.period + ":</b> " + weather.shortDesc + "</td></tr>";

                                if (job.Value.Count.Equals(counter))
                                {
                                    html += "<br /><br /><tr><td>&nbsp;</td></tr><tr><td>&nbsp;</td></tr><br /><br />";
                                }

                                counter++;
                            }
                        }

                        return html;// response;
                    default:
                        break;
                }               

            }
            catch (Exception ex)
            {
                jresult.Status = "500";
                jresult.Method = System.Reflection.MethodBase.GetCurrentMethod().Name;
                jresult.Message = "Internal Server Error: " + ex.Message;
                jresult.Object = new object();
            }

            return result;
        }

        /// <summary>
        /// Runs the scrapehtml method for threading in another function
        /// </summary>
        /// <param name="credentials"></param>
        private void RunJob(api credentials)
        {
            try
            {
                List<WeatherData> tmpList = ScrapeHtml(credentials);

                GlobalServices.lstJobs.Add(curJobID +"|" + credentials.city + ", " + credentials.state, tmpList);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Scrapes html for weather data
        /// </summary>
        /// <param name = "data" ></ param >
        /// < param name="dt"></param>
        /// <returns></returns>
        private List<WeatherData> ScrapeHtml(api credentials)
        {
            List<WeatherData> lstData = new List<WeatherData>();

            try
            {
                Coordinates gps = GetCoordinates(credentials);

                HtmlWeb web = new HtmlWeb();
                HtmlDocument htmlDoc = web.Load("http://forecast.weather.gov/MapClick.php?lat=" + gps.lat + "&lon=" + gps.lng);

                if (htmlDoc.DocumentNode != null)
                {
                    var htmlNodes = htmlDoc.DocumentNode.SelectNodes(@"//*[@id=""detailed-forecast-body""]").First()
                        .ChildNodes.Where(x => x.Name != "#text");

                    foreach (var n in htmlNodes)
                    {
                        WeatherData weather = new WeatherData();
                        weather.jobID = curJobID;
                        weather.status = "Running";

                        int subCount = 0;
                        foreach (var sub in n.ChildNodes)
                        {
                            if (subCount.Equals(0))
                            {
                                weather.period = sub.InnerText;
                            }
                            else
                            {
                                weather.shortDesc = sub.InnerText;
                            }
                            subCount++;
                        }

                        lstData.Add(weather);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return lstData;
        }

        /// <summary>
        /// Gets coordinates to use in the url
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private Coordinates GetCoordinates(api credentials)
        {
            Coordinates gps = new Coordinates();

            try
            {
                gps.state = credentials.state;
                gps.city = credentials.city;

                DataRow row = (from d in GlobalServices.dtCoordinates.AsEnumerable()
                               where d.Field<string>("State") == gps.state
                               && d.Field<string>("City") == gps.city
                               select d).FirstOrDefault();
                if (row != null)
                {
                    gps.lat = row["Latitude"].ToString();
                    gps.lng = row["Longitude"].ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return gps;
        }
    }

    public class JResult
    {
        public string Status { get; set; }
        public string Method { get; set; }
        public string Message { get; set; }
        public object Object { get; set; }
    }

    /// <summary>
    /// Model for deserializing data coming from the ajax call
    /// </summary>
    public class api
    {
        public string state { get; set; }
        public string city { get; set; }
        public string jobID { get; set; }
        public string method { get; set; }
    }

}