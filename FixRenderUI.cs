using AotForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class FixRenderUI
    {
        [DllImport("psapi.dll")]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessWorkingSetSize(IntPtr procHandle, int minSize, int maxSize);

        private static DateTime lastRamClear = DateTime.MinValue;
        private const int CLEAR_CACHE_INTERVAL = 15; // giây

        internal static void Work()
        {
            Process lastProcess = null;
            bool lastState = false;

            while (true)
            {
                try
                {
                    Process hdProcess = Process.GetProcessesByName("HD-Player").FirstOrDefault();

                    if (hdProcess != null && !hdProcess.HasExited)
                    {
                        if (!hdProcess.Equals(lastProcess) || Config.FixCrashRender != lastState)
                        {
                            ApplyOrRevertOptimizations(hdProcess);
                            lastProcess = hdProcess;
                            lastState = Config.FixCrashRender;
                        }

                        if (Config.FixCrashRender && (DateTime.Now - lastRamClear).TotalSeconds >= CLEAR_CACHE_INTERVAL)
                        {
                            CleanMemory(hdProcess);
                            lastRamClear = DateTime.Now;
                        }
                    }
                }
                catch { /* Bỏ qua lỗi không quan trọng */ }

                Thread.Sleep(2000);
            }
        }

        private static void ApplyOrRevertOptimizations(Process process)
        {
            try
            {
                if (process.HasExited) return;

                if (Config.FixCrashRender)
                {
                    process.PriorityClass = ProcessPriorityClass.BelowNormal;

                    int coreCount = Environment.ProcessorCount;
                    long affinityMask = 0;
                    for (int i = coreCount / 2; i < coreCount; i++)
                        affinityMask |= 1L << i;

                    if (affinityMask > 0)
                        process.ProcessorAffinity = (IntPtr)affinityMask;
                }
                else
                {
                    process.PriorityClass = ProcessPriorityClass.Normal;

                    long allCoresMask = (1L << Environment.ProcessorCount) - 1;
                    process.ProcessorAffinity = (IntPtr)allCoresMask;
                }
            }
            catch { }
        }

        private static void CleanMemory(Process process)
        {
            try
            {
                if (process.HasExited) return;
                EmptyWorkingSet(process.Handle);
                SetProcessWorkingSetSize(process.Handle, -1, -1);
            }
            catch { }
        }
    }
}