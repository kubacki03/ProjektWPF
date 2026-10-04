using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjektWPF.Services
{
    public class ComputerDataService
    {
        public static async Task<float> GetCpuUsage()
        {
            using (PerformanceCounter cpuCounter = new PerformanceCounter(
                "Processor",
                "% Processor Time",
                "_Total"))
            {
                cpuCounter.NextValue();

                await Task.Delay(1000);

                return cpuCounter.NextValue();
            }
        }

        public static float GetAvailableMemory()
        {
            using (PerformanceCounter ramCounter = new PerformanceCounter(
                "Memory",
                "Available MBytes"))
            {
                return ramCounter.NextValue();
            }
        }
    }
}