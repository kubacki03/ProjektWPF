using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ProjektWPF.Services
{
    /// <summary>
    /// Serwis do obsługi operacji na plikach JSON przechowujących dane użytkowników
    /// </summary>
    public class FileService
    {
        /// <summary>
        /// Dodaje lub aktualizuje dane użytkowników w pliku JSON
        /// </summary>
        /// <param name="data">Słownik zawierający pary ID użytkownika i dane do zapisania</param>
        /// <remarks>
        /// Format pliku: { "12345": "dane_uzytkownika", "67890": "inne_dane" }
        /// </remarks>
        public void AddLocalUserToFile(Dictionary<long, string> data)
        {
            string filePath = "data.json";
            Dictionary<long, string> allData;

            // Sprawdzenie istnienia pliku i wczytanie istniejących danych
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                allData = JsonSerializer.Deserialize<Dictionary<long, string>>(json);
            }
            else
            {
                allData = new Dictionary<long, string>();
            }

            // Aktualizacja lub dodanie nowych wpisów
            foreach (var item in data)
            {
                allData[item.Key] = item.Value;  // Nadpisuje istniejące wartości dla tych samych kluczy
            }

            // Serializacja z formatowaniem dla czytelności
            string updatedJson = JsonSerializer.Serialize(
                allData,
                new JsonSerializerOptions { WriteIndented = true }
            );

            // Zapis całej zawartości do pliku
            File.WriteAllText(filePath, updatedJson);
        }

        /// <summary>
        /// Wczytuje dane z pliku JSON
        /// </summary>
        /// <returns>Słownik z danymi użytkowników lub pusty słownik jeśli plik nie istnieje</returns>
        public Dictionary<long, string> LoadData()
        {
            string filePath = "data.json";

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<Dictionary<long, string>>(json);
                return data ?? new Dictionary<long, string>();  // Zabezpieczenie przed null
            }

            return new Dictionary<long, string>();
        }
    }
}