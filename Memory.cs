using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace ZexMemory
{
	#region Memory
	public class Zex
	{
		[DllImport("kernel32.dll")]
		private static extern void GetSystemInfo(out Zex.SYSTEM_INFO lpSystemInfo);
		[DllImport("kernel32.dll")]
		public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
		[DllImport("kernel32")]
		public static extern bool IsWow64Process(IntPtr hProcess, out bool lpSystemInfo);
		[DllImport("kernel32.dll")]
		private static extern bool VirtualProtectEx(IntPtr hProcess, UIntPtr lpAddress, IntPtr dwSize, Zex.MemoryProtection flNewProtect, out Zex.MemoryProtection lpflOldProtect);
		[DllImport("kernel32.dll")]
		private static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesWritten);
		[DllImport("kernel32.dll")]
		private static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesRead);
		[DllImport("kernel32.dll")]
		public static extern int CloseHandle(IntPtr hObject);
		[DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
		public static extern UIntPtr Native_VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress, out Zex.MEMORY_BASIC_INFORMATION64 lpBuffer, UIntPtr dwLength);
		[DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
		public static extern UIntPtr Native_VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress, out Zex.MEMORY_BASIC_INFORMATION32 lpBuffer, UIntPtr dwLength);
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);
		[DllImport("kernel32.dll")]
		private static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] IntPtr lpBuffer, UIntPtr nSize, out ulong lpNumberOfBytesRead);
        public string LoadCode(string name, string file)
        {
            if (!string.IsNullOrEmpty(file))
            {
                var builder = new StringBuilder(1024);
                GetPrivateProfileString("codes", name, "", builder, (uint)builder.Capacity, file);
                return builder.ToString();
            }
            return name;
        }

        public byte[] Read(string code, int length, string file = "")
        {
            byte[] buffer = new byte[length];
            UIntPtr address = GetCode(code, file, 8);
            if (ReadProcessMemory(pHandle, address, buffer, (UIntPtr)length, IntPtr.Zero))
                return buffer;
            return null;
        }

        public Task<IEnumerable<long>> AoBScan(long start, long end, string search, bool readable, bool writable, bool executable, string file = "")
		{
			return Task.Run<IEnumerable<long>>(delegate ()
			{
				List<MemoryRegionResult> list = new List<MemoryRegionResult>();
				string text = this.LoadCode(search, file);
				string[] array = text.Split(new char[]
				{
					' '
				});
				byte[] aobPattern = new byte[array.Length];
				byte[] mask = new byte[array.Length];
				for (int i = 0; i < array.Length; i++)
				{
					string text2 = array[i];
					bool flag = text2 == "??" || (text2.Length == 1 && text2 == "?");
					if (flag)
					{
						mask[i] = 0;
						array[i] = "0x00";
					}
					else
					{
						bool flag2 = char.IsLetterOrDigit(text2[0]) && text2[1] == '?';
						if (flag2)
						{
							mask[i] = 240;
							array[i] = text2[0].ToString() + "0";
						}
						else
						{
							bool flag3 = char.IsLetterOrDigit(text2[1]) && text2[0] == '?';
							if (flag3)
							{
								mask[i] = 15;
								array[i] = "0" + text2[1].ToString();
							}
							else
							{
								mask[i] = byte.MaxValue;
							}
						}
					}
				}
				for (int j = 0; j < array.Length; j++)
				{
					aobPattern[j] = ((byte)(Convert.ToByte(array[j], 16) & mask[j]));
				}
                Zex.SYSTEM_INFO system_INFO = default(Zex.SYSTEM_INFO);
                Zex.GetSystemInfo(out system_INFO);
				UIntPtr minimumApplicationAddress = system_INFO.minimumApplicationAddress;
				UIntPtr maximumApplicationAddress = system_INFO.maximumApplicationAddress;
				bool flag4 = start < (long)minimumApplicationAddress.ToUInt64();
				if (flag4)
				{
					start = (long)minimumApplicationAddress.ToUInt64();
				}
				bool flag5 = end > (long)maximumApplicationAddress.ToUInt64();
				if (flag5)
				{
					end = (long)maximumApplicationAddress.ToUInt64();
				}
				Debug.WriteLine(string.Concat(new string[]
				{
					"[DEBUG] memory scan starting... (start:0x",
					start.ToString(this.MSize()),
					" end:0x",
					end.ToString(this.MSize()),
					" time:",
					DateTime.Now.ToString("h:mm:ss tt"),
					")"
				}));
				UIntPtr uintPtr = new UIntPtr((ulong)start);
                Zex.MEMORY_BASIC_INFORMATION memory_BASIC_INFORMATION = default(Zex.MEMORY_BASIC_INFORMATION);
				while (this.VirtualQueryEx(this.pHandle, uintPtr, out memory_BASIC_INFORMATION).ToUInt64() != 0UL && uintPtr.ToUInt64() < (ulong)end && uintPtr.ToUInt64() + (ulong)memory_BASIC_INFORMATION.RegionSize > uintPtr.ToUInt64())
				{
					bool flag6 = memory_BASIC_INFORMATION.State == 4096U;
					flag6 &= (memory_BASIC_INFORMATION.BaseAddress.ToUInt64() < maximumApplicationAddress.ToUInt64());
					flag6 &= ((memory_BASIC_INFORMATION.Protect & 256U) == 0U);
					flag6 &= ((memory_BASIC_INFORMATION.Protect & 1U) == 0U);
					flag6 &= (memory_BASIC_INFORMATION.Type == this.MEM_PRIVATE || memory_BASIC_INFORMATION.Type == this.MEM_IMAGE);
					bool flag7 = flag6;
					if (flag7)
					{
						bool flag8 = (memory_BASIC_INFORMATION.Protect & 2U) > 0U;
						bool flag9 = (memory_BASIC_INFORMATION.Protect & 4U) > 0U || (memory_BASIC_INFORMATION.Protect & 8U) > 0U || (memory_BASIC_INFORMATION.Protect & 64U) > 0U || (memory_BASIC_INFORMATION.Protect & 128U) > 0U;
						bool flag10 = (memory_BASIC_INFORMATION.Protect & 16U) > 0U || (memory_BASIC_INFORMATION.Protect & 32U) > 0U || (memory_BASIC_INFORMATION.Protect & 64U) > 0U || (memory_BASIC_INFORMATION.Protect & 128U) > 0U;
						flag8 &= readable;
						flag9 &= writable;
						flag10 &= executable;
						flag6 &= (flag8 || flag9 || flag10);
					}
					bool flag11 = !flag6;
					if (flag11)
					{
						uintPtr = new UIntPtr(memory_BASIC_INFORMATION.BaseAddress.ToUInt64() + (ulong)memory_BASIC_INFORMATION.RegionSize);
					}
					else
					{
						MemoryRegionResult item2 = new MemoryRegionResult
						{
							CurrentBaseAddress = uintPtr,
							RegionSize = memory_BASIC_INFORMATION.RegionSize,
							RegionBase = memory_BASIC_INFORMATION.BaseAddress
						};
						uintPtr = new UIntPtr(memory_BASIC_INFORMATION.BaseAddress.ToUInt64() + (ulong)memory_BASIC_INFORMATION.RegionSize);
						bool flag12 = list.Count > 0;
						if (flag12)
						{
							MemoryRegionResult memoryRegionResult = list[list.Count - 1];
							bool flag13 = (ulong)memoryRegionResult.RegionBase + (ulong)memoryRegionResult.RegionSize == (ulong)memory_BASIC_INFORMATION.BaseAddress;
							if (flag13)
							{
								list[list.Count - 1] = new MemoryRegionResult
								{
									CurrentBaseAddress = memoryRegionResult.CurrentBaseAddress,
									RegionBase = memoryRegionResult.RegionBase,
									RegionSize = memoryRegionResult.RegionSize + memory_BASIC_INFORMATION.RegionSize
								};
								continue;
							}
						}
						list.Add(item2);
					}
				}
				ConcurrentBag<long> bagResult = new ConcurrentBag<long>();
				Parallel.ForEach<MemoryRegionResult>(list, delegate (MemoryRegionResult item, ParallelLoopState parallelLoopState, long index)
				{
					long[] array2 = this.CompareScan(item, aobPattern, mask);
					foreach (long item3 in array2)
					{
						bagResult.Add(item3);
					}
				});
				Debug.WriteLine("[DEBUG] memory scan completed. (time:" + DateTime.Now.ToString("h:mm:ss tt") + ")");
				return (from c in bagResult.ToList<long>()
						orderby c
						select c).AsEnumerable<long>();
			});
		}
		public string MSize()
		{
			bool is64Bit = this.Is64Bit;
			string result;
			if (is64Bit)
			{
				result = "x16";
			}
			else
			{
				result = "x8";
			}
			return result;
		}
		public void CloseProcess()
		{
			IntPtr intPtr = this.pHandle;
			bool flag = false;
			if (!flag)
			{
                Zex.CloseHandle(this.pHandle);
				this.theProc = null;
			}
		}
		private bool _is64Bit;
		public bool Is64Bit
		{
			get
			{
				return this._is64Bit;
			}
			private set
			{
				this._is64Bit = value;
			}
		}
		private unsafe long[] CompareScan(MemoryRegionResult item, byte[] aobPattern, byte[] mask)
		{
			bool flag = mask.Length != aobPattern.Length;
			if (flag)
			{
				throw new ArgumentException("aobPattern.Length != mask.Length");
			}
			IntPtr intPtr = Marshal.AllocHGlobal((int)item.RegionSize);
			ulong num;
            Zex.ReadProcessMemory(this.pHandle, item.CurrentBaseAddress, intPtr, (UIntPtr)((ulong)item.RegionSize), out num);
			int num2 = 0 - aobPattern.Length;
			List<long> list = new List<long>();
			do
			{
				num2 = this.FindPattern((byte*)intPtr.ToPointer(), (int)num, aobPattern, mask, num2 + aobPattern.Length);
				bool flag2 = num2 >= 0;
				if (flag2)
				{
					list.Add((long)((ulong)item.CurrentBaseAddress + (ulong)((long)num2)));
				}
			}
			while (num2 != -1);
			Marshal.FreeHGlobal(intPtr);
			return list.ToArray();
		}
		private unsafe int FindPattern(byte* body, int bodyLength, byte[] pattern, byte[] masks, int start = 0)
		{
			int num = -1;
			bool flag = bodyLength <= 0 || pattern.Length == 0 || start > bodyLength - pattern.Length || pattern.Length > bodyLength;
			int result;
			if (flag)
			{
				result = num;
			}
			else
			{
				for (int i = start; i <= bodyLength - pattern.Length; i++)
				{
					bool flag2 = (body[i] & masks[0]) == (pattern[0] & masks[0]);
					if (flag2)
					{
						bool flag3 = true;
						for (int j = 1; j <= pattern.Length - 1; j++)
						{
							bool flag4 = (body[i + j] & masks[j]) == (pattern[j] & masks[j]);
							if (!flag4)
							{
								flag3 = false;
								break;
							}
						}
						bool flag5 = !flag3;
						if (!flag5)
						{
							num = i;
							break;
						}
					}
				}
				result = num;
			}
			return result;
		}
		public UIntPtr VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress, out Zex.MEMORY_BASIC_INFORMATION lpBuffer)
		{
			bool flag = this.Is64Bit || IntPtr.Size == 8;
			UIntPtr result;
			if (flag)
			{
                Zex.MEMORY_BASIC_INFORMATION64 memory_BASIC_INFORMATION = default(Zex.MEMORY_BASIC_INFORMATION64);
				UIntPtr uintPtr = Zex.Native_VirtualQueryEx(hProcess, lpAddress, out memory_BASIC_INFORMATION, new UIntPtr((uint)Marshal.SizeOf(memory_BASIC_INFORMATION)));
				lpBuffer.BaseAddress = memory_BASIC_INFORMATION.BaseAddress;
				lpBuffer.AllocationBase = memory_BASIC_INFORMATION.AllocationBase;
				lpBuffer.AllocationProtect = memory_BASIC_INFORMATION.AllocationProtect;
				lpBuffer.RegionSize = (long)memory_BASIC_INFORMATION.RegionSize;
				lpBuffer.State = memory_BASIC_INFORMATION.State;
				lpBuffer.Protect = memory_BASIC_INFORMATION.Protect;
				lpBuffer.Type = memory_BASIC_INFORMATION.Type;
				result = uintPtr;
			}
			else
			{
                Zex.MEMORY_BASIC_INFORMATION32 memory_BASIC_INFORMATION2 = default(Zex.MEMORY_BASIC_INFORMATION32);
				UIntPtr uintPtr = Zex.Native_VirtualQueryEx(hProcess, lpAddress, out memory_BASIC_INFORMATION2, new UIntPtr((uint)Marshal.SizeOf(memory_BASIC_INFORMATION2)));
				lpBuffer.BaseAddress = memory_BASIC_INFORMATION2.BaseAddress;
				lpBuffer.AllocationBase = memory_BASIC_INFORMATION2.AllocationBase;
				lpBuffer.AllocationProtect = memory_BASIC_INFORMATION2.AllocationProtect;
				lpBuffer.RegionSize = (long)((ulong)memory_BASIC_INFORMATION2.RegionSize);
				lpBuffer.State = memory_BASIC_INFORMATION2.State;
				lpBuffer.Protect = memory_BASIC_INFORMATION2.Protect;
				lpBuffer.Type = memory_BASIC_INFORMATION2.Type;
				result = uintPtr;
			}
			return result;
		}
		public static void notify(string message)
		{
			Process.Start(new ProcessStartInfo("cmd.exe", $"/c start cmd /C \"color b && title Error && echo {message} && timeout /t 5\"")
			{
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false
			});
			Environment.Exit(0);
		}
		
        public UIntPtr GetCode(string name, string path = "", int size = 8)
        {
            if (Is64Bit && size == 8)
                size = 16;
            return Is64Bit ? Get64BitCode(name, path, size) : ResolveCode(name, path, size, false);
        }
        private UIntPtr ResolveCode(string name, string path, int size, bool is64Bit)
        {
            string input = string.IsNullOrEmpty(path) ? name : LoadCode(name, path);
            if (!input.Contains("+") && !input.Contains(","))
            {
                return is64Bit
                    ? new UIntPtr(Convert.ToUInt64(input, 16))
                    : new UIntPtr(Convert.ToUInt32(input, 16));
            }

            string[] parts = input.Split('+');
            IntPtr baseAddress = modules.ContainsKey(parts[0]) ? modules[parts[0]] : mainModule.BaseAddress;
            string[] offsets = parts[1].Split(',');

            ulong address = (ulong)baseAddress.ToInt64();
            byte[] buffer = new byte[size];

            foreach (var offsetStr in offsets)
            {
                long offset = Convert.ToInt64(offsetStr.Replace("0x", ""), 16);
                var ptr = new UIntPtr(address);
                if (!ReadProcessMemory(pHandle, ptr, buffer, (UIntPtr)size, IntPtr.Zero))
                    return UIntPtr.Zero;

                address = is64Bit
                    ? BitConverter.ToUInt64(buffer, 0) + (ulong)offset
                    : BitConverter.ToUInt32(buffer, 0) + (uint)offset;
            }
            return new UIntPtr(address);
        }

        public UIntPtr Get64BitCode(string name, string path = "", int size = 16)
        {
            return ResolveCode(name, path, size, true);
        }
        public bool WriteMemory(string code, string type, string value, string file = "", Encoding encoding = null)
        {
            UIntPtr address = GetCode(code, file, 8);
            if (address == UIntPtr.Zero || pHandle == IntPtr.Zero)
                return false;

            byte[] data;
            string t = type.ToLower();

            if (t == "float")
                data = BitConverter.GetBytes(float.Parse(value));
            else if (t == "int")
                data = BitConverter.GetBytes(int.Parse(value));
            else if (t == "long")
                data = BitConverter.GetBytes(long.Parse(value));
            else if (t == "double")
                data = BitConverter.GetBytes(double.Parse(value));
            else if (t == "byte")
                data = new byte[] { Convert.ToByte(value, 16) };
            else if (t == "2bytes")
            {
                short val = Convert.ToInt16(value);
                data = new byte[] { (byte)(val & 0xFF), (byte)(val >> 8) };
            }
            else if (t == "bytes")
            {
                var tokens = value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                data = tokens.Select(s => Convert.ToByte(s, 16)).ToArray();
            }
            else if (t == "string")
                data = (encoding ?? Encoding.UTF8).GetBytes(value);
            else
                throw new NotSupportedException("Unknown type: " + type);

            return WriteProcessMemory(pHandle, address, data, (UIntPtr)data.Length, IntPtr.Zero);
        }
        public bool IsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        public bool OpenProcess(int pid)
        {
            if (!IsAdmin())
            {
                notify("You are NOT running as administrator!");
            }

            try
            {
                theProc = Process.GetProcessById(pid);
                if (!theProc.Responding) return false;
                pHandle = OpenProcess(0x1F0FFF, true, pid);
                if (pHandle == IntPtr.Zero) return false;

                mainModule = theProc.MainModule;
                GetModules();

                bool isWow64;
                IsWow64Process(pHandle, out isWow64);
                Is64Bit = Environment.Is64BitOperatingSystem && !isWow64;
                return true;
            }
            catch
            {
                return false;
            }
        }
        public void GetModules()
		{
			bool flag = this.theProc == null;
			if (!flag)
			{
				this.modules.Clear();
				foreach (object obj in this.theProc.Modules)
				{
					ProcessModule processModule = (ProcessModule)obj;
					bool flag2 = !string.IsNullOrEmpty(processModule.ModuleName) && !this.modules.ContainsKey(processModule.ModuleName);
					if (flag2)
					{
						this.modules.Add(processModule.ModuleName, processModule.BaseAddress);
					}
				}
			}
		}
		public Task<IEnumerable<long>> AoBScan2(string search, bool writable = false, bool executable = false, string file = "")
		{
			return this.AoBScan(0L, long.MaxValue, search, writable, executable, file);
		}
		public Task<IEnumerable<long>> AoBScan(long start, long end, string search, bool writable, bool executable, string file = "")
		{
			return this.AoBScan(start, end, search, true, writable, executable, file);
		}
		public bool ChangeProtection(string code, Zex.MemoryProtection newProtection, out Zex.MemoryProtection oldProtection, string file = "")
		{
			UIntPtr code2 = this.GetCode(code, file, 8);
			bool flag = code2 == UIntPtr.Zero || this.pHandle == IntPtr.Zero;
			bool result;
			if (flag)
			{
				oldProtection = (Zex.MemoryProtection)0U;
				result = false;
			}
			else
			{
				result = Zex.VirtualProtectEx(this.pHandle, code2, (IntPtr)(this.Is64Bit ? 8 : 4), newProtection, out oldProtection);
			}
			return result;
		}
		private Dictionary<string, IntPtr> modules = new Dictionary<string, IntPtr>();
		private ProcessModule mainModule;
		public Process theProc = null;
		private uint MEM_PRIVATE = 131072U;
		private uint MEM_IMAGE = 16777216U;
		public IntPtr pHandle;
		[Flags]
		public enum ThreadAccess
		{
			TERMINATE = 1,
			SUSPEND_RESUME = 2,
			GET_CONTEXT = 8,
			SET_CONTEXT = 16,
			SET_INFORMATION = 32,
			QUERY_INFORMATION = 64,
			SET_THREAD_TOKEN = 128,
			IMPERSONATE = 256,
			DIRECT_IMPERSONATION = 512
		}
		public struct MEMORY_BASIC_INFORMATION32
		{
			public UIntPtr BaseAddress;

			public UIntPtr AllocationBase;

			public uint AllocationProtect;

			public uint RegionSize;

			public uint State;

			public uint Protect;

			public uint Type;
		}
		public struct MEMORY_BASIC_INFORMATION64
		{
			public UIntPtr BaseAddress;

			public UIntPtr AllocationBase;

			public uint AllocationProtect;

			public uint __alignment1;

			public ulong RegionSize;

			public uint State;

			public uint Protect;

			public uint Type;

			public uint __alignment2;
		}
		[Flags]
		public enum MemoryProtection : uint
		{
			Execute = 16U,
			ExecuteRead = 32U,
			ExecuteReadWrite = 64U,
			ExecuteWriteCopy = 128U,
			NoAccess = 1U,
			ReadOnly = 2U,
			ReadWrite = 4U,
			WriteCopy = 8U,
			GuardModifierflag = 256U,
			NoCacheModifierflag = 512U,
			WriteCombineModifierflag = 1024U
		}
		public struct SYSTEM_INFO
		{
			public ushort processorArchitecture;

			private ushort reserved;

			public uint pageSize;

			public UIntPtr minimumApplicationAddress;

			public UIntPtr maximumApplicationAddress;

			public IntPtr activeProcessorMask;

			public uint numberOfProcessors;

			public uint processorType;

			public uint allocationGranularity;

			public ushort processorLevel;

			public ushort processorRevision;
		}
		public struct MEMORY_BASIC_INFORMATION
		{
			public UIntPtr BaseAddress;

			public UIntPtr AllocationBase;

			public uint AllocationProtect;

			public long RegionSize;

			public uint State;

			public uint Protect;

			public uint Type;
		}
		private int x;

	}
	#endregion
	#region Beyond-region-result
	internal struct MemoryRegionResult
	{
		public UIntPtr CurrentBaseAddress { get; set; }

		public long RegionSize { get; set; }

		public UIntPtr RegionBase { get; set; }
	}
	#endregion
}
