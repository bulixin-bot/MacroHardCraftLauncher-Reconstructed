using System;
using System.Threading;
using ConsoleAppLogin.NetEase;
using Login.NetEase.DummyService.Entity;
using Login.NetEase.RPCServer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using text;

namespace Login.NetEase.DummyService;

internal class LockServer
{
	public class SPLock
	{
		public SPLock(string ip, string port, string spfile)
		{
			app.InvokeCmdAsync("start FastBuilder.exe --uid " + Http.UID + " --sid 4654415063569776560:LobbyGame --name " + app.displayName + " --version " + app.Temp.Version + " --engineVersion " + app.Temp.engineVersion + " --url " + AuthServerUrl + " --authurl https://g79authobt.nie.netease.com --ip " + ip + " --port " + port + " --typ sp --spfile " + spfile + " --token " + CppGameM.GetLoginToken());
		}
	}

	public class SPLockLine
	{
		public SPLockLine(string ip, string port)
		{
			app.InvokeCmdAsync("start FastBuilder.exe --uid " + Http.UID + " --sid 4654415063569776560:LobbyGame --name " + app.displayName + " --version " + app.Temp.Version + " --engineVersion " + app.Temp.engineVersion + " --url " + AuthServerUrl + " --authurl https://g79authobt.nie.netease.com --ip " + ip + " --port " + port + " --typ linesp --token " + CppGameM.GetLoginToken());
		}
	}

	public class LockServerv7
	{
		public LockServerv7(string ip, string port)
		{
			app.InvokeCmdAsync("start FastBuilder.exe --uid " + Http.UID + " --sid 4654415063569776560:LobbyGame --name " + app.displayName + " --version " + app.Temp.Version + " --engineVersion " + app.Temp.engineVersion + " --url " + AuthServerUrl + " --authurl https://g79authobt.nie.netease.com --ip " + ip + " --port " + port + " --typ bfv2 --token " + CppGameM.GetLoginToken());
		}
	}

	public class LockServerv8
	{
		public static int w = 64;

		public static int h = 64;

		public LockServerv8(string ip, string port)
		{
			app.InvokeCmdAsync("start FastBuilder.exe --uid " + Http.UID + " --sid 4654415063569776560:LobbyGame --name " + app.displayName + " --version " + app.Temp.Version + " --engineVersion " + app.Temp.engineVersion + " --url " + AuthServerUrl + " --authurl https://g79authobt.nie.netease.com --ip " + ip + " --port " + port + " --typ join --Height " + h + " --Width " + w + " --token " + CppGameM.GetLoginToken());
		}

		public static void LocalProxy(string ip, string port, string sid, string localport)
		{
			app.InvokeCmdAsync("start Proxy.exe --uid " + Http.UID + " --sid " + sid + " --name " + app.displayName + " --version " + app.Temp.Version + " --engineVersion " + app.Temp.engineVersion + " --patchVersion " + app.Temp.patchVersion + " --url http://127.0.0.1:24791 --authurl https://g79authobt.nie.netease.com --ip " + ip + " --port " + port + " --localport " + localport + " --g79 --token " + CppGameM.GetLoginToken());
		}

		public static void LocalProxype(string ip, string port, string sid, string localport)
		{
			app.InvokeCmdAsync("start Proxy.exe --uid " + Http.UID + " --sid " + sid + " --name " + app.displayName + " --version " + app.Temp.Version + " --engineVersion " + app.Temp.engineVersion + " --url http://127.0.0.1:24791 --authurl https://g79authobt.nie.netease.com --ip " + ip + " --port " + port + " --localport " + localport + " --token " + CppGameM.GetLoginToken());
		}

		public static void SetWH(int wh)
		{
			w = wh;
			h = wh;
		}
	}

	public static string AuthServerUrl = "http://127.0.0.1:24791";

	public static string WebServerUrl = "http://127.0.0.1:24798";

	private int _processID;

	public static string password;

	public LockServer(string[] user, string rid)
	{
		string jsonString = JsonConvert.SerializeObject(new DummyUserList
		{
			UserList = user
		});
		app.InvokeCmd("start LobbyRoomDummy.exe --roomid " + rid + " --userlist " + Function.ReplaceString(jsonString));
		for (int i = 0; user.Length > i; i++)
		{
			DummySrv.strings.Add(user[i]);
		}
		Console.WriteLine("已关闭线程");
	}

	public LockServer(string ip, string port)
	{
		app.InvokeCmdAsync("start FastBuilder.exe --uid " + Http.UID + " --sid 4654415063569776560:LobbyGame --name " + app.displayName + " --version " + app.Temp.Version + " --engineVersion " + app.Temp.engineVersion + " --url " + AuthServerUrl + " --authurl https://g79authobt.nie.netease.com --ip " + ip + " --port " + port + " --typ bf --token " + CppGameM.GetLoginToken());
	}

	private void RecvPid(byte[] array)
	{
		_processID = BitConverter.ToInt32(array, 0);
		Console.WriteLine("已获取pid" + _processID);
	}

	public LockServer(string ip, string port, string sid)
	{
		app.start_stop = true;
		new Thread((ThreadStart)delegate
		{
			while (app.start_stop)
			{
				RPCPort rPCPort = new RPCPort();
				rPCPort.RegisterReceiveCallBack(0, RecvPid);
				bool b = true;
				bool b2 = true;
				Thread thread = new Thread((ThreadStart)delegate
				{
					Thread.Sleep(3000);
					Console.WriteLine("超时");
					if (b)
					{
						app.InvokeCmdAsync("taskkill /pid " + _processID + " -t -f");
						rPCPort.NormalExit();
						b2 = false;
					}
				});
				thread.Start();
				app.InvokeCmd("FastBuilder.exe --uid " + Http.UID + " --sid " + sid + " --name " + app.displayName + " --version " + app.Temp.Version + " --engineVersion " + app.Temp.engineVersion + " --patchVersion " + app.Temp.patchVersion + " --url " + AuthServerUrl + " --authurl https://g79authobt.minecraft.cn --ip " + ip + " --port " + port + " --LauncherPort " + rPCPort.LauncherControlPort + " --g79 --typ bf --token " + CppGameM.GetLoginToken());
				if (b2)
				{
					b = false;
					rPCPort.NormalExit();
					try
					{
						thread.Abort();
					}
					catch
					{
					}
				}
			}
		}).Start();
	}

	public LockServer(string sid)
	{
		app.start_stop = true;
		new Thread((ThreadStart)delegate
		{
			while (app.start_stop)
			{
				try
				{
					RPCPort rPCPort = new RPCPort();
					rPCPort.RegisterReceiveCallBack(0, RecvPid);
					bool b = true;
					bool b2 = true;
					JObject jObject = JsonConvert.DeserializeObject<JObject>(Http.x19post("https://g79mclobt.minecraft.cn", "/rental-server-world-enter/get", "{\"server_id\": \"" + sid + "\", \"pwd\": \"" + password + "\"}"));
					string text = jObject["entity"]["mcserver_host"].ToString();
					string text2 = jObject["entity"]["mcserver_port"].ToString();
					Thread thread = new Thread((ThreadStart)delegate
					{
						Thread.Sleep(3000);
						Console.WriteLine("超时");
						if (b)
						{
							app.InvokeCmdAsync("taskkill /pid " + _processID + " -t -f");
							rPCPort.NormalExit();
							b2 = false;
						}
					});
					thread.Start();
					app.InvokeCmd("FastBuilder.exe --uid " + Http.UID + " --sid " + sid + ":RentalGame --name " + app.displayName + " --version " + app.Temp.Version + " --engineVersion " + app.Temp.engineVersion + " --patchVersion " + app.Temp.patchVersion + " --url " + AuthServerUrl + " --authurl https://g79authobt.minecraft.cn --ip " + text + " --port " + text2 + " --LauncherPort " + rPCPort.LauncherControlPort + " --g79 --typ bf --left --token " + CppGameM.GetLoginToken());
					if (b2)
					{
						b = false;
						rPCPort.NormalExit();
						try
						{
							thread.Abort();
						}
						catch
						{
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
				}
			}
		}).Start();
	}
}

