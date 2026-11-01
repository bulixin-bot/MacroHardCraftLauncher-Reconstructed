using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Login.NetEase;
using Login.NetEase.DummyService;
using Login.NetEase.RPCServer;
using text;

internal class Ocr
{
	public static string GetOcr(string imageBase64)
	{
		int usablePort = RPCPort.GetUsablePort();
		if (usablePort == -1)
		{
			return null;
		}
		Function.ClientLog("[4399SDK]ptlogin captcha Port:" + usablePort);
		TcpListener tcpListener = new TcpListener(new IPEndPoint(Dns.GetHostAddresses("127.0.0.1")[0], usablePort));
		tcpListener.Start();
		if (FLockServer.DisplayWindow)
		{
			app.InvokeCmdAsync("start OcrProxy.dll " + usablePort);
		}
		else
		{
			app.InvokeCmdAsync("OcrProxy.dll " + usablePort);
		}
		TcpClient tcpClient = tcpListener.AcceptTcpClient();
		NetworkStream stream = tcpClient.GetStream();
		BinaryReader binaryReader = new BinaryReader(stream);
		BinaryWriter binaryWriter = new BinaryWriter(stream);
		byte[] bytes = Encoding.ASCII.GetBytes(imageBase64);
		bytes = BitConverter.GetBytes((uint)bytes.Length).Concat(bytes).ToArray();
		binaryWriter.Write(bytes);
		uint count = binaryReader.ReadUInt32();
		byte[] bytes2 = binaryReader.ReadBytes((int)count);
		tcpClient.Close();
		tcpListener.Stop();
		return Encoding.ASCII.GetString(bytes2);
	}
}

