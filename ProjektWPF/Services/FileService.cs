using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ProjektWPF.Services
{
    public class FileService
    {
        private const string FilePath = "data.json";

        public void AddLocalUserToFile(Dictionary<long, string> data)
        {
            var allData = LoadData();

            foreach (var item in data)
            {
                allData[item.Key] = item.Value;
            }

            SaveData(allData);
        }

        public void RemoveLocalUser(long userId)
        {
            var allData = LoadData();

            if (allData.Remove(userId))
            {
                SaveData(allData);
            }
        }

        public Dictionary<long, string> LoadData()
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                var data = JsonSerializer.Deserialize<Dictionary<long, string>>(json);
                return data ?? new Dictionary<long, string>();
            }

            return new Dictionary<long, string>();
        }

        private static void SaveData(Dictionary<long, string> data)
        {
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }
}
