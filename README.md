# WebScraper
Scraping weather forecasts

The user can get the 7 day forecast by selecting a state and a city and clicking "Scrape"

If the user wants all states for the respective state that's selected, they can click "Scrape All Cities"

The browser automatically checks the status of the requests every seconds.

By selecting 'Results" the user can get the forecast of all the requests made.

"Reset" resets the browser for a fresh run

The application doesn't use a database or flat files. It uses dictionaries as well as list of strings and a class called WeatherData

The application interfaces with the nuget package HtmlAgilityPack to assist with scraping

www.forecast.weather.gov is the url used for scraping
