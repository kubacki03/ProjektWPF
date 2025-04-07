using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjektWPF.Services
{
    /// <summary>
    /// Serwis do zbierania danych o wykorzystaniu zasobów komputera
    /// Wymaga referencji do System.Diagnostics
    /// </summary>
    public class ComputerDataService
    {
        /// <summary>
        /// Pobiera aktualne wykorzystanie procesora w procentach
        /// </summary>
        /// <returns>Procentowe wykorzystanie procesora (0-100)</returns>
        public static async Task<float> GetCpuUsage()
        {
            using (PerformanceCounter cpuCounter = new PerformanceCounter(
                "Processor",    // Kategoria licznika
                "% Processor Time", // Nazwa licznika
                "_Total"))      // Nazwa instancji (łączne zużycie dla wszystkich rdzeni)
            {
                // Inicjalizacja licznika - pierwsze odczytanie wartości
                cpuCounter.NextValue();

                // Oczekiwanie 1 sekundy aby uzyskać dokładny odczyt
                await Task.Delay(1000);

                // Pobranie rzeczywistego wykorzystania procesora
                return cpuCounter.NextValue();
            }
        }

        /// <summary>
        /// Pobiera ilość dostępnej pamięci RAM
        /// </summary>
        /// <returns>Dostępna pamięć w megabajtach (MB)</returns>
        public static float GetAvailableMemory()
        {
            using (PerformanceCounter ramCounter = new PerformanceCounter(
                "Memory",        // Kategoria licznika
                "Available MBytes")) // Nazwa licznika
            {
                // Bezpośredni odczyt dostępnej pamięci
                return ramCounter.NextValue();
            }
        }
    }
}