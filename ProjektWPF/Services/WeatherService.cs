using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using static System.Net.WebRequestMethods;

namespace ProjektWPF.Services
{
    /// <summary>
    /// Serwis do pobierania danych pogodowych i lokalizacji
    /// </summary>
    class WeatherService
    {
        // Współrzędne geograficzne
        int lat;
        int lon;

        /// <summary>
        /// Pobiera przybliżoną lokalizację użytkownika na podstawie adresu IP
        /// </summary>
        public async Task GetLocationAsync()
        {
            using HttpClient client = new HttpClient();
            // Wykonanie zapytania do zewnętrznego API geolokalizacji
            string response = await client.GetStringAsync("http://ip-api.com/json/");
            JObject json = JObject.Parse(response);

            // Parsowanie współrzędnych z odpowiedzi JSON
            double latitude = (double)json["lat"];
            double longitude = (double)json["lon"];

            Console.WriteLine($"Latitude: {latitude}, Longitude: {longitude}");
            // Konwersja do int może powodować utratę precyzji (możliwy błąd zaokrąglenia)
            this.lat = (int)latitude;
            this.lon = (int)longitude;
        }

        /// <summary>
        /// Pobiera i formatuje dane pogodowe dla aktualnej lokalizacji
        /// </summary>
        /// <returns>Sformatowany string z temperaturą i prędkością wiatru</returns>
        public async Task<string> GetWeather()
        {
            // Najpierw pobierz lokalizację
            await GetLocationAsync();

            // Budowanie URL do API OpenWeatherMap
            string url = $"https://api.openweathermap.org/data/2.5/weather?lat={this.lat}&lon={this.lon}&appid={Environment.GetEnvironmentVariable("WEATHER_API")}";

            using HttpClient client = new HttpClient();
            // Pobranie danych pogodowych
            string response = await client.GetStringAsync(url);
            JObject data = JObject.Parse(response);

            // Parsowanie i konwersja jednostek
            double temp = (double)data["main"]["temp"];  // Temperatura w Kelvinach
            double windSpeed = (double)data["wind"]["speed"];  // Prędkość wiatru w m/s

            // Konwersja jednostek
            temp = temp - 273.15;  // Kelvin na Celsiusz
            windSpeed = windSpeed * 3.6;  // m/s na km/h

            // Formatowanie wyniku
            return $"Temperatura {temp.ToString("F1")}°C\nWiatr {windSpeed.ToString("F1")} km/h";
        }
    }
}