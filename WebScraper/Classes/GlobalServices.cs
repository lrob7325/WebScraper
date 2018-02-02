using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using WebScraper.Models;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace WebScraper.Global
{
    public static class GlobalServices
    {
        private static HomeModel _model;
        public static HomeModel Model
        {
            get
            {
                try
                {
                    if (_model == null)
                    {
                        _model = new HomeModel();
                        _model.State = States;
                        _model.Cities = theDictionary;
                    }
                }
                catch (Exception ex)
                {
                    _model = new HomeModel();
                }

                return _model;

            }
        }

        private static DataTable _table;
        public static DataTable dtCoordinates
        {
            get
            {
                try
                {
                    if (_table == null)
                    {
                        _table = new DataTable();

                        _table.TableName = "Coordinates";

                        _table.Columns.Add(new DataColumn { ColumnName = "City", ReadOnly = true });
                        _table.Columns.Add(new DataColumn { ColumnName = "State", ReadOnly = true });
                        _table.Columns.Add(new DataColumn { ColumnName = "Zipcode", ReadOnly = true });
                        _table.Columns.Add(new DataColumn { ColumnName = "Latitude", ReadOnly = true });
                        _table.Columns.Add(new DataColumn { ColumnName = "Longitude", ReadOnly = true });

                        var contents = File.ReadLines(AppDomain.CurrentDomain.BaseDirectory + @"\Resources\uscitiesv1.3.csv");

                        int count = 0;

                        foreach (var line in contents)
                        {
                            count++;
                            if (count.Equals(1))
                            { continue; }

                            string[] items = line.Split(',');

                            DataRow row = _table.NewRow();

                            row["City"] = items[0];
                            row["State"] = items[2];
                            row["Zipcode"] = items[6];
                            row["Latitude"] = items[7];
                            row["Longitude"] = items[8];

                            _table.Rows.Add(row);
                        }

                        _table.AcceptChanges();
                    }
                }
                catch (Exception ex)
                {
                    _table = new DataTable();
                }

                return _table;
            }
        }

        private static List<string> _states;
        public static List<string> States
        {
            get
            {
                try
                {
                    if (_states == null)
                    {
                        _states = (from d in dtCoordinates.AsEnumerable()
                                   select d.Field<string>("State")).ToList().Distinct().ToList().OrderBy(y => y).ToList();
                    }
                }
                catch (Exception ex)
                {
                    _states = new List<string>();
                }

                return _states;
            }
        }

        private static Dictionary<string, List<string>> _dictionary;
        public static Dictionary<string, List<string>> theDictionary
        {
            get
            {
                try
                {
                    if (_dictionary == null)
                    {
                        _dictionary = new Dictionary<string, List<string>>();

                        foreach (string s in States)
                        {
                            List<string> cities = (from d in dtCoordinates.AsEnumerable()
                                                   where d.Field<string>("State") == s
                                                   select d.Field<string>("City")).ToList().Distinct().ToList().OrderBy(y => y).ToList();

                            _dictionary.Add(s, cities);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _dictionary = new Dictionary<string, List<string>>();
                }

                return _dictionary;
            }
        }

        private static Dictionary<string, Thread> _thread;
        public static Dictionary<string, Thread> lstThread
        {
            get
            {
                if (_thread == null)
                { _thread = new Dictionary<string, Thread>(); }

                return _thread;
            }
        }


        private static Dictionary<string, List<WeatherData>> _jobs;
        public static Dictionary<string, List<WeatherData>> lstJobs
        {
            get
            {
                if (_jobs == null)
                {
                    _jobs = new Dictionary<string, List<WeatherData>>();
                }

                return _jobs;
            }
        }
    }
}