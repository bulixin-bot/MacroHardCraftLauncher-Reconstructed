using System;
using System.Threading;
using Login.NetEase.DummyService.Entity;
using Login.NetEase.RPCServer;
using Newtonsoft.Json;
using text;

namespace Login.NetEase.DummyService;

internal class DummyClient
{
	public int _processID;

	public string UserID;

	public string AimUserID;

	private Thread thread;

	private RPCPort rPCPort;

	private DummyUserList dummyU;

	public DummyClient(string uid, RecvMessageEntity recvMessageEntity, DummyUserList dummyUserList, int time, string title)
	{
		new Thread((ThreadStart)delegate
		{
			AimUserID = uid;
			bool is_start = false;
			dummyU = dummyUserList;
			string jsonString = JsonConvert.SerializeObject(dummyUserList);
			UserID = recvMessageEntity.uid.ToString();
			rPCPort = new RPCPort();
			rPCPort.RegisterReceiveCallBack(0, RecvPid);
			thread = new Thread((ThreadStart)delegate
			{
				try
				{
					for (int i = 0; i < time; i++)
					{
						Thread.Sleep(60000);
					}
					is_start = true;
					app.InvokeCmdAsync("taskkill /pid " + _processID + " -t -f");
					rPCPort.NormalExit();
					for (int j = 0; dummyU.UserList.Length > j; j++)
					{
						DummySrv.strings.Add(dummyU.UserList[j]);
					}
					app.chatConnection.SendUIDMessage("自动停止服务！", recvMessageEntity.uid.ToString());
					if (DummySrv.CloseDummyTask(UserID, AimUserID))
					{
						Function.ClientLog("[DummyTask] Free Dummy Task.", ConsoleColor.Yellow);
					}
					Console.WriteLine("已关闭线程");
				}
				catch (Exception ex2)
				{
					Console.WriteLine(ex2.ToString());
				}
			});
			thread.Start();
			app.InvokeCmd("start LobbyRoomDummy.exe --uid " + uid + " --userlist " + Function.ReplaceString(jsonString) + " --port " + rPCPort.LauncherControlPort + " --title " + title);
			if (!is_start)
			{
				try
				{
					rPCPort.NormalExit();
					for (int num = 0; dummyU.UserList.Length > num; num++)
					{
						DummySrv.strings.Add(dummyU.UserList[num]);
					}
					if (DummySrv.CloseDummyTask(UserID, AimUserID))
					{
						Function.ClientLog("[DummyTask] Free Dummy Task.", ConsoleColor.Yellow);
					}
					Console.WriteLine("已关闭线程");
					thread.Abort();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
				}
			}
		}).Start();
	}

	public void Close(RecvMessageEntity recvMessageEntity)
	{
		try
		{
			Console.WriteLine("正在结束线程");
			try
			{
				thread.Abort();
			}
			catch
			{
			}
			rPCPort.NormalExit();
			for (int i = 0; dummyU.UserList.Length > i; i++)
			{
				DummySrv.strings.Add(dummyU.UserList[i]);
			}
			if (DummySrv.CloseDummyTask(UserID, AimUserID))
			{
				Function.ClientLog("[DummyTask] Free Dummy Task.", ConsoleColor.Yellow);
			}
			app.InvokeCmdAsync("taskkill /pid " + _processID + " -t -f");
			Console.WriteLine("taskkill /pid " + _processID + " -t -f");
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}
	}

	private void RecvPid(byte[] array)
	{
		_processID = BitConverter.ToInt32(array, 0);
		Console.WriteLine("已获取pid" + _processID);
	}
}

