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

        // GET api/<controller>/5
        [HttpGet]
        public object Get(string state)//(HttpRequestMessage value)
        {
            string data = string.Empty;
            List<string> html = new List<string>();

            try
            {
                //data = value.RequestUri.AbsoluteUri.Replace("%22","");

                //int idx = data.IndexOf("state:");
                //string state = data.Substring(idx + 6, 2).Replace(":","").Replace(",","");
                                       
                if (state.Equals("0"))
                {
                    html.Add("No jobs are running");
                    return html;
                }

                var tmpThread = GlobalServices.lstThread.Where(x => x.Key.Contains(state)).ToList();
                foreach (var d in tmpThread)
                {
                    html.Add("<br>" + d.Key.Split('|')[1] + " Status: " + (d.Value.ThreadState == ThreadState.Stopped ? "Completed" : d.Value.ThreadState.ToString()));
                }
                if (html.Count.Equals(0))
                { html.Add("No jobs are running"); }

                return html;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return html;
        }

        private List<string> GetCities(string st)
        {
            return GlobalServices.theDictionary.Where(x => x.Key == st).First().Value.ToList();
        }

        // POST api/<controller>
        [HttpPost]
        public object Post(HttpRequestMessage value)
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
                    case "states":
                        return GetCities(credentials.state);
                    case "status":
                        Thread theThread = GlobalServices.lstThread.Where(x => x.Key.Split('|')[0] == credentials.jobID).First().Value;

                        return theThread.ThreadState == ThreadState.Stopped ? "Completed" : theThread.ThreadState.ToString();
                    case "statusall":
                        List<string> html = new List<string>();
                        if(credentials.state.Equals("0"))
                        {
                            html.Add("No jobs are running");
                            return html;
                        }

                        var tmpThread = GlobalServices.lstThread.Where(x => x.Key.Contains(credentials.state)).ToList();
                        foreach (var d in tmpThread)
                        {
                            html.Add("<br>" + d.Key.Split('|')[1] + " Status: " + (d.Value.ThreadState == ThreadState.Stopped ? "Completed" : d.Value.ThreadState.ToString()));
                        }
                        if (html.Count.Equals(0))
                        { html.Add("No jobs are running"); }

                        return html;
                    case "results":
                        List<WeatherData> response = GlobalServices.lstJobs.Where(x => x.Key == credentials.jobID).First().Value;
                        return response;
                    default:
                        break;
                }

                curJobID = credentials.jobID;

                Thread thread = new Thread(() =>
                    RunJob(credentials)
                    );

                GlobalServices.lstThread.Add(credentials.jobID + "|" + credentials.city + ", " + credentials.state , thread);
                thread.Start();

                jresult.Status = "200";
                jresult.Method = "POST";
                jresult.Message = curJobID;
                jresult.Object = new object();
                result.Add(JObject.Parse(JsonConvert.SerializeObject(jresult)));

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

        private void RunJob(api credentials)
        {
            try
            {
                List<WeatherData> tmpList = ScrapeHtml(credentials);

                GlobalServices.lstJobs.Add(curJobID, tmpList);
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

    public class api
    {
        public string state { get; set; }
        public string city { get; set; }
        public string jobID { get; set; }
        public string method { get; set; }
    }

}