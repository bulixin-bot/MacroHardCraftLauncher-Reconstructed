using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace DllInjector;

internal class Program
{
	private class Options
	{
		public string ProcessName { get; set; } = "";

		public int? Pid { get; set; }

		public string DllPath { get; set; } = "";

		public bool AutoSelect { get; set; }

		public bool Silent { get; set; }

		public bool ShowHelp { get; set; }

		public bool ListProcesses { get; set; }
	}

	private const uint PROCESS_ALL_ACCESS = 2035711u;

	private const uint MEM_COMMIT = 4096u;

	private const uint MEM_RESERVE = 8192u;

	private const uint PAGE_READWRITE = 4u;

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern nint OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool CloseHandle(nint hObject);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern nint VirtualAllocEx(nint hProcess, nint lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool WriteProcessMemory(nint hProcess, nint lpBaseAddress, byte[] lpBuffer, uint nSize, out nint lpNumberOfBytesWritten);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern nint GetProcAddress(nint hModule, string lpProcName);

	[DllImport("kernel32.dll")]
	private static extern nint GetModuleHandle(string lpModuleName);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern nint CreateRemoteThread(nint hProcess, nint lpThreadAttributes, uint dwStackSize, nint lpStartAddress, nint lpParameter, uint dwCreationFlags, out nint lpThreadId);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern uint GetLastError();

	private static void Main(string[] args)
	{
		Options options = ParseArguments(args);
		if (options.ShowHelp || args.Length == 0)
		{
			ShowHelp();
		}
		else if (!IsRunningAsAdmin())
		{
			if (options.Silent)
			{
				Console.Error.WriteLine("ERROR: 需要管理员权限");
				Environment.ExitCode = 2;
			}
			else
			{
				Console.WriteLine("正在请求管理员权限...");
				RestartAsAdmin(args);
			}
		}
		else
		{
			Environment.ExitCode = ExecuteInjection(options);
		}
	}

	private static Options ParseArguments(string[] args)
	{
		Options options = new Options();
		for (int i = 0; i < args.Length; i++)
		{
			switch (args[i].ToLower())
			{
			case "-h":
			case "/?":
			case "--help":
				options.ShowHelp = true;
				break;
			case "-n":
			case "--name":
				if (i + 1 < args.Length)
				{
					options.ProcessName = args[++i];
				}
				break;
			case "-p":
			case "--pid":
			{
				if (i + 1 < args.Length && int.TryParse(args[++i], out var result))
				{
					options.Pid = result;
				}
				break;
			}
			case "-d":
			case "--dll":
				if (i + 1 < args.Length)
				{
					options.DllPath = args[++i];
				}
				break;
			case "-a":
			case "--auto":
				options.AutoSelect = true;
				break;
			case "-s":
			case "--silent":
				options.Silent = true;
				break;
			case "-l":
			case "--list":
				options.ListProcesses = true;
				break;
			}
		}
		return options;
	}

	private static void ShowHelp()
	{
		Console.WriteLine("\r\nDLL Injector - 命令行 DLL 注入工具\r\n\r\n用法:\r\n  DllInjector.exe [选项]\r\n\r\n选项:\r\n  -n, --name <进程名>     按进程名注入（支持部分匹配）\r\n  -p, --pid <PID>         按进程 ID 注入\r\n  -d, --dll <DLL路径>     指定要注入的 DLL 路径\r\n  -a, --auto              自动选择第一个匹配的进程\r\n  -s, --silent            静默模式（不显示输出，仅返回错误码）\r\n  -l, --list              列出所有进程\r\n  -h, --help              显示此帮助信息\r\n\r\n示例:\r\n  DllInjector.exe -n minecraft -d C:\\1.dll\r\n  DllInjector.exe -n minecraft -d C:\\1.dll -a\r\n  DllInjector.exe -p 1234 -d C:\\1.dll\r\n  DllInjector.exe -l | findstr minecraft\r\n\r\n返回码:\r\n  0  注入成功\r\n  1  参数错误\r\n  2  需要管理员权限\r\n  3  未找到目标进程\r\n  4  DLL 文件不存在\r\n  5  注入失败\r\n");
	}

	private static int ExecuteInjection(Options options)
	{
		if (options.ListProcesses)
		{
			foreach (Process item in (from p in Process.GetProcesses()
				orderby p.ProcessName
				select p).ToList())
			{
				try
				{
					Console.WriteLine($"{item.Id,8} {item.ProcessName}");
				}
				catch
				{
				}
			}
			return 0;
		}
		if (string.IsNullOrEmpty(options.ProcessName) && !options.Pid.HasValue)
		{
			Console.Error.WriteLine("ERROR: 必须指定进程名 (-n) 或 PID (-p)");
			return 1;
		}
		if (string.IsNullOrEmpty(options.DllPath))
		{
			options.DllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "1.dll");
			if (!options.Silent)
			{
				Console.WriteLine("使用默认 DLL 路径: " + options.DllPath);
			}
		}
		if (!File.Exists(options.DllPath))
		{
			Console.Error.WriteLine("ERROR: DLL 文件不存在: " + options.DllPath);
			return 4;
		}
		List<Process> list = new List<Process>();
		if (options.Pid.HasValue)
		{
			try
			{
				Process processById = Process.GetProcessById(options.Pid.Value);
				list.Add(processById);
			}
			catch
			{
				Console.Error.WriteLine($"ERROR: 找不到 PID {options.Pid.Value}");
				return 3;
			}
		}
		else if (!string.IsNullOrEmpty(options.ProcessName))
		{
			list = (from p in Process.GetProcesses()
				where p.ProcessName.Contains(options.ProcessName, StringComparison.OrdinalIgnoreCase)
				select p).ToList();
			if (list.Count == 0)
			{
				Console.Error.WriteLine("ERROR: 未找到包含 '" + options.ProcessName + "' 的进程");
				return 3;
			}
		}
		Process process;
		if (list.Count > 1 && !options.AutoSelect)
		{
			if (options.Silent)
			{
				Console.Error.WriteLine("ERROR: 找到多个匹配进程，请使用 -a 自动选择或指定 PID");
				return 1;
			}
			Console.WriteLine($"找到 {list.Count} 个匹配的进程：");
			for (int num = 0; num < list.Count; num++)
			{
				try
				{
					Console.WriteLine($"  [{num + 1}] {list[num].ProcessName} (PID: {list[num].Id})");
				}
				catch
				{
				}
			}
			Console.Write("请选择编号 (1-{0}): ", list.Count);
			if (!int.TryParse(Console.ReadLine(), out var result) || result < 1 || result > list.Count)
			{
				Console.Error.WriteLine("ERROR: 无效的选择");
				return 1;
			}
			process = list[result - 1];
		}
		else
		{
			process = list.First();
		}
		if (!options.Silent)
		{
			try
			{
				Console.WriteLine($"目标进程: {process.ProcessName} (PID: {process.Id})");
			}
			catch
			{
			}
			Console.WriteLine("DLL 路径: " + options.DllPath);
			Console.WriteLine("正在注入...");
		}
		if (InjectDll(process.Id, options.DllPath, options.Silent))
		{
			if (!options.Silent)
			{
				Console.WriteLine("✅ 注入成功！");
			}
			return 0;
		}
		uint lastError = GetLastError();
		if (!options.Silent)
		{
			Console.Error.WriteLine($"❌ 注入失败！错误码: {lastError} (0x{lastError:X8})");
			switch (lastError)
			{
			case 5u:
				Console.Error.WriteLine("  提示: 拒绝访问，请确保以管理员身份运行");
				break;
			case 87u:
				Console.Error.WriteLine("  提示: 参数错误，可能是架构不匹配 (x86/x64)");
				break;
			}
		}
		return 5;
	}

	private static bool IsRunningAsAdmin()
	{
		try
		{
			using WindowsIdentity ntIdentity = WindowsIdentity.GetCurrent();
			return new WindowsPrincipal(ntIdentity).IsInRole(WindowsBuiltInRole.Administrator);
		}
		catch
		{
			return false;
		}
	}

	private static void RestartAsAdmin(string[] args)
	{
		string text = Process.GetCurrentProcess().MainModule?.FileName ?? Environment.ProcessPath;
		if (string.IsNullOrEmpty(text))
		{
			Console.WriteLine("无法获取程序路径，请手动以管理员身份运行。");
			Console.ReadKey();
			return;
		}
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = text,
				Arguments = string.Join(" ", args.Select((string a) => "\"" + a + "\"")),
				UseShellExecute = true,
				Verb = "runas",
				WorkingDirectory = Environment.CurrentDirectory
			});
			Console.WriteLine("已请求管理员权限，请在弹出的 UAC 对话框中点击 '是'。");
		}
		catch (Exception ex)
		{
			Console.WriteLine("提权失败: " + ex.Message);
			Console.WriteLine("请手动以管理员身份运行本程序。");
		}
		Console.WriteLine("按任意键退出当前进程...");
		Console.ReadKey();
	}

	private static bool InjectDll(int pid, string dllPath, bool silent)
	{
		nint num = OpenProcess(2035711u, bInheritHandle: false, pid);
		if (num == IntPtr.Zero)
		{
			if (!silent)
			{
				Console.WriteLine($"[-] 无法打开进程，错误码: {GetLastError()}");
			}
			return false;
		}
		try
		{
			byte[] bytes = Encoding.ASCII.GetBytes(dllPath + "\0");
			nint num2 = VirtualAllocEx(num, IntPtr.Zero, (uint)bytes.Length, 12288u, 4u);
			if (num2 == IntPtr.Zero)
			{
				if (!silent)
				{
					Console.WriteLine("[-] 在目标进程中分配内存失败");
				}
				return false;
			}
			if (!WriteProcessMemory(num, num2, bytes, (uint)bytes.Length, out var lpNumberOfBytesWritten))
			{
				if (!silent)
				{
					Console.WriteLine("[-] 写入内存失败");
				}
				return false;
			}
			nint procAddress = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
			if (procAddress == IntPtr.Zero)
			{
				if (!silent)
				{
					Console.WriteLine("[-] 获取 LoadLibraryA 地址失败");
				}
				return false;
			}
			nint num3 = CreateRemoteThread(num, IntPtr.Zero, 0u, procAddress, num2, 0u, out lpNumberOfBytesWritten);
			if (num3 == IntPtr.Zero)
			{
				return false;
			}
			Thread.Sleep(1000);
			CloseHandle(num3);
			return true;
		}
		finally
		{
			CloseHandle(num);
		}
	}
}

