﻿using AotForms;
using System.Runtime.InteropServices;
using System.Text;

internal static class InternalMemory
{
    [DllImport("AotBst.dll")]
    static extern nint CPU(nint pVM, uint cpuId);

    [DllImport("AotBst.dll")]
    static extern int InternalRead(nint pVM, ulong address, nint buffer, uint size);

    [DllImport("AotBst.dll")]
    static extern int Cast(nint pVCpu, ulong address, out ulong physAddress);

    [DllImport("AotBst.dll")]
    static extern int InternalWrite(nint pVM, ulong address, nint buffer, uint size);

    static nint pVMAddr;
    static nint cpuAddr;

    internal static Dictionary<ulong, CacheEntry> Cache = new Dictionary<ulong, CacheEntry>();

    internal class CacheEntry
    {
        public ulong PhysAddress { get; set; }
        public DateTime LastAccessed { get; set; }
        public DateTime Expiration { get; set; }
    }

    internal static void Initialize(nint pVM)
    {
        pVMAddr = pVM;
        cpuAddr = CPU(pVM, 0);
        Cache = new Dictionary<ulong, CacheEntry>();
    }

    internal static bool Convert(ulong address, out ulong phys)
    {
        phys = 0;

        if (Cache.TryGetValue(address, out var cachedEntry))
        {
            if (DateTime.Now < cachedEntry.Expiration)
            {
                cachedEntry.LastAccessed = DateTime.Now;
                phys = cachedEntry.PhysAddress;
                return true;
            }
            else
            {
                Cache.Remove(address);
            }
        }

        cpuAddr = CPU(pVMAddr, 0);
        var status = Cast(cpuAddr, address, out phys);

        if (status == 0 && !Config.NoCache)
        {
            TimeSpan expirationTime = CalculateDynamicExpirationTime(address);

            Cache[address] = new CacheEntry
            {
                PhysAddress = phys,
                LastAccessed = DateTime.Now,
                Expiration = DateTime.Now.Add(expirationTime)
            };
            return true;
        }

        return false;
    }

    private static TimeSpan CalculateDynamicExpirationTime(ulong address)
    {
        if (Cache.TryGetValue(address, out var entry))
        {
            double accessFrequency = (DateTime.Now - entry.LastAccessed).TotalSeconds;

            if (accessFrequency < 1) return TimeSpan.FromSeconds(10);
            if (accessFrequency < 5) return TimeSpan.FromSeconds(5);
        }

        return TimeSpan.FromSeconds(1);
    }

    internal static unsafe bool Read<T>(ulong address, out T data) where T : struct
    {
        data = default;
        var result = Convert(address, out address);
        if (!result) return false;

        T buffer = default;
        var bufferReference = __makeref(buffer);
        var size = (uint)Marshal.SizeOf<T>();

        var status = InternalRead(pVMAddr, address, *(nint*)&bufferReference, size);
        data = buffer;
        return status == 0;
    }

    internal static unsafe bool ReadArray<T>(ulong address, ref T[] array) where T : struct
    {
        var result = Convert(address, out address);
        if (!result) return false;

        var size = (uint)((ulong)Marshal.SizeOf(array[0]) * (ulong)array.Length);
        var typedReference = __makeref(array[0]);

        var status = InternalRead(pVMAddr, address, *(nint*)&typedReference, size);
        return status == 0;
    }

    internal static string ReadString(ulong address, int size, bool unicode = true)
    {
        var stringBytes = new byte[size];

        var read = ReadArray(address, ref stringBytes);

        if (!read) return "";

        var readString = unicode ? Encoding.Unicode.GetString(stringBytes) : Encoding.Default.GetString(stringBytes);

        if (readString.Contains('\0'))
            readString = readString.Substring(0, readString.IndexOf('\0'));

        return readString;
    }

    internal static unsafe void Write<T>(ulong address, T value) where T : struct
    {
        var result = Convert(address, out address);
        if (!result) return;

        var size = (uint)Marshal.SizeOf<T>();
        var bufferReference = __makeref(value);

        InternalWrite(pVMAddr, address, *(nint*)&bufferReference, size);
    }
}
