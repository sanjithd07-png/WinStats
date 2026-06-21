using System;
using System.Linq;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;

namespace WinStats
{
    public class HardwareMonitor
    {
        // P/Invoke to use the exact same Windows API that Task Manager uses
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        private Computer _computer;

        public HardwareMonitor()
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = false // Shutting off LHM's buggy memory polling
            };
            _computer.Open();
        }

        public void Update()
        {
            foreach (var hardware in _computer.Hardware)
            {
                hardware.Update();
            }
        }

        public string GetCpuStats()
        {
            var cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
            if (cpu == null) return "CPU N/A";

            var load = cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load)?.Value ?? 0;
            var temp = cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature)?.Value ?? 0;

            return $"CPU  {Math.Round(load)}%   {Math.Round(temp)}°C";
        }

        public string GetGpuStats()
        {
            var gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia || h.HardwareType == HardwareType.GpuAmd);
            if (gpu == null) return "GPU N/A";

            var load = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load)?.Value ?? 0;

            return $"GPU  {Math.Round(load)}%";
        }

        public string GetRamStats()
        {
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                // This mirrors Task Manager exactly: (Total Usable Physical - Available) / Total Usable Physical
                double total = memStatus.ullTotalPhys;
                double available = memStatus.ullAvailPhys;
                double used = total - available;

                double percentage = (used / total) * 100;
                return $"RAM  {Math.Round(percentage)}%";
            }

            return "RAM N/A";
        }
    }
}