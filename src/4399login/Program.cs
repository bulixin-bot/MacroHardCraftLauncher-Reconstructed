using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Netease4399Cli;

internal static class Program
{
	private static async Task<int> Main(string[] args)
	{
		try
		{
			Console.OutputEncoding = Encoding.UTF8;
		}
		catch
		{
		}
		if (args.Length == 0 || IsHelp(args[0]))
		{
			PrintUsage();
			return (args.Length == 0) ? 2 : 0;
		}
		bool flag = false;
		List<string> list = new List<string>();
		foreach (string text in args)
		{
			if ((text == "-v" || text == "--verbose") ? true : false)
			{
				flag = true;
			}
			else
			{
				list.Add(text);
			}
		}
		if (list.Count < 2)
		{
			PrintUsage();
			return 2;
		}
		if (flag)
		{
			Netease4399Login.SetVerbose(verbose: true);
		}
		string username = list[0];
		string password = list[1];
		string captchaId = ((list.Count > 2) ? list[2] : null);
		string captcha = ((list.Count > 3) ? list[3] : null);
		Login4399Result login4399Result = await Netease4399Login.LoginAsync(username, password, captchaId, captcha);
		if (login4399Result.Success && login4399Result.SauthJson != null)
		{
			Console.WriteLine(login4399Result.SauthJson);
			return 0;
		}
		if (login4399Result.CaptchaRequired)
		{
			string text2 = Guid.NewGuid().ToString("N").ToUpperInvariant();
			string text3 = "https://ptlogin.4399.com/ptlogin/captcha.do?captchaId=" + text2;
			Console.Error.WriteLine("[captcha_required]");
			Console.Error.WriteLine("4399 要验证码。请打开下面的地址看图，然后带上验证码重跑：");
			Console.Error.WriteLine("  URL       : " + text3);
			Console.Error.WriteLine("  重跑示例  : 4399login <account> <password> " + text2 + " <你看到的验证码>");
			if (!string.IsNullOrEmpty(login4399Result.ErrorMessage))
			{
				Console.Error.WriteLine("  4399 原话 : " + login4399Result.ErrorMessage);
			}
			TryOpenUrl(text3);
			return 3;
		}
		if (login4399Result.SmsVerificationRequired)
		{
			Console.Error.WriteLine("[sms_required]");
			Console.Error.WriteLine("网易这边触发了短信风控。用手机发送下面的内容到指定号码，等 10 秒后原样重跑：");
			Console.Error.WriteLine("  短信内容  : " + login4399Result.SmsContent);
			Console.Error.WriteLine("  发送到    : " + string.Join(" 或 ", login4399Result.SmsNumbers));
			if (!string.IsNullOrEmpty(login4399Result.ErrorMessage))
			{
				Console.Error.WriteLine("  4399 原话 : " + login4399Result.ErrorMessage);
			}
			return 4;
		}
		Console.Error.WriteLine("[failed]");
		Console.Error.WriteLine(login4399Result.ErrorMessage ?? "未知错误");
		return 1;
	}

	private static bool IsHelp(string s)
	{
		switch (s)
		{
		case "-h":
		case "--help":
		case "/?":
			return true;
		default:
			return false;
		}
	}

	private static void TryOpenUrl(string url)
	{
		try
		{
			Process.Start(new ProcessStartInfo(url)
			{
				UseShellExecute = true
			});
		}
		catch
		{
		}
	}

	private static void PrintUsage()
	{
		Console.Error.WriteLine("4399login - 用 4399 账密换网易 sauth_json");
		Console.Error.WriteLine();
		Console.Error.WriteLine("用法:");
		Console.Error.WriteLine("  4399login <account> <password> [captchaId] [captcha] [-v|--verbose]");
		Console.Error.WriteLine();
		Console.Error.WriteLine("参数:");
		Console.Error.WriteLine("  account      4399 账号（用户名或邮箱）");
		Console.Error.WriteLine("  password     4399 密码");
		Console.Error.WriteLine("  captchaId    可选，4399 要求验证码时程序会打印这个值");
		Console.Error.WriteLine("  captcha      可选，验证码图片里看到的字符");
		Console.Error.WriteLine("  -v           打开调试日志（打到 stderr）");
		Console.Error.WriteLine();
		Console.Error.WriteLine("输出:");
		Console.Error.WriteLine("  成功时 stdout 输出一行：{\"sauth_json\":\"{...}\"}");
		Console.Error.WriteLine();
		Console.Error.WriteLine("退出码:");
		Console.Error.WriteLine("  0  成功");
		Console.Error.WriteLine("  1  失败");
		Console.Error.WriteLine("  2  参数错误");
		Console.Error.WriteLine("  3  需要验证码（按提示重跑）");
		Console.Error.WriteLine("  4  需要短信风控（按提示重跑）");
	}
}

