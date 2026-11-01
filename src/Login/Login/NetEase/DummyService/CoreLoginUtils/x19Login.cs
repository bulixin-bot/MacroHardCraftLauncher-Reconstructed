using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using FeverToSauth;
using Login.Native;
using Login.PEEntity.PEAuthentication;
using Login.loginauth;
using MCStudio.Network.Http;
using Mark;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Noya.LocalServer.Common.Extensions;
using Noya.LocalServer.Common.Utilities;
using WPFLauncher.Network.Launcher;
using text;
using text.loginauth;

namespace Login.NetEase.DummyService.CoreLoginUtils;

internal class x19Login
{
	public int Offset = 2;

	public int Rounds = 9;

	private Thread UpdateAuth;

	public Action<byte[]> UpdateCallBack;

	private readonly HttpClient client = new HttpClient();

	public string UID;

	public string PEUID;

	public byte[] MD5Token;

	public static bool delete { get; set; }

	public static AuthenticationResponse AuthenticationEntity { get; set; }

	public string sa_data { get; set; } = app.SimpleRead("sa_data.json");

	public string sa_data_PE { get; set; } = app.SimpleRead("sa_data_pe.json");

	public string server_id { get; set; }

	public string room_id_public { get; set; }

	public bool istrue { get; set; }

	public string LoginSRCToken { get; set; }

	public string LoginDToken { get; set; }

	public string player_info { get; set; }

	public string AuthResponse { get; set; }

	public string coreobt { get; set; } = "https://x19obtcore.nie.netease.com:8443";

	public string apigatewayobt { get; set; } = "https://x19apigatewayobt.nie.netease.com";

	public string apigatewayobtPE { get; set; } = "https://g79apigatewayobt.minecraft.cn";

	public static string engineVersion { get; set; } = app.Temp.engineVersion;

	public static string patchVersion { get; set; } = app.Temp.patchVersion;

	public static string PECoreobt { get; set; } = "https://g79obtcore.minecraft.cn:8443";

	public string LibMinecraftPE { get; set; } = app.Temp.libminecraftpe;

	public string Patch { get; set; } = app.Temp.patch;

	public x19Login()
	{
		client.DefaultRequestHeaders.ExpectContinue = false;
		client.DefaultRequestHeaders.Add("User-Agent", "WPFLauncher/0.0.0.0");
	}

	public int Login(string Login_otp)
	{
		if (Login_otp.StartsWith("SDK4399"))
		{
			Login_otp = FeverAuth.SDK4399ToSauth(Login_otp);
		}
		_ = string.Empty;
		LoginotpResposne loginotpResposne = new LoginotpResposne();
		new AuthenticationResponse();
		if (Login_otp == "")
		{
			Console.WriteLine("未检测到cookie");
			Environment.Exit(0);
		}
		StringContent content = new StringContent(Login_otp, Encoding.UTF8, "application/json");
		string result = client.PostAsync(coreobt + "/login-otp", content).Result.Content.ReadAsStringAsync().Result;
		try
		{
			loginotpResposne = JsonConvert.DeserializeObject<LoginotpResposne>(result);
		}
		catch
		{
			Console.WriteLine("请求失败" + result);
			return -1;
		}
		if (loginotpResposne.code != 0)
		{
			Console.WriteLine("登录失败，请检查cookie：" + Regex.Unescape(result));
			return -1;
		}
		string authenticationOtpJson = GetAuthenticationOtpJson("1.14.6.45947", loginotpResposne.entity.aid, loginotpResposne.entity.otp_token, Login_otp);
		ByteArrayContent byteArrayContent = new ByteArrayContent(x19Crypt.HttpEncrypt(Encoding.UTF8.GetBytes(authenticationOtpJson)));
		byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		string keyStr;
		string text = (AuthResponse = CoreNative.HttpDecrypt(client.PostAsync(coreobt + "/authentication-otp", byteArrayContent).Result.Content.ReadAsByteArrayAsync().Result, out keyStr));
		string text3 = text;
		try
		{
			AuthenticationEntity = JsonConvert.DeserializeObject<AuthenticationResponse>(text3);
			UID = AuthenticationEntity.entity.entity_id;
			LoginSRCToken = AuthenticationEntity.entity.token;
			MD5Token = HashHelper.SafeCompleteMD5(Encoding.UTF8.GetBytes(LoginSRCToken));
		}
		catch
		{
			Console.WriteLine("出错了？请切换账号后重试！");
			Console.WriteLine("错误信息：" + Regex.Unescape(text3));
			Thread.Sleep(5000);
			return -1;
		}
		Console.Clear();
		Console.WriteLine("登录成功！");
		UpdateLogin_();
		GetName();
		Console.WriteLine("UID:" + UID);
		return 0;
	}

	public static string Base64Encode(byte[] plainText)
	{
		return Convert.ToBase64String(plainText);
	}

	public int PELogin(string Login_otp)
	{
		try
		{
			if (Login_otp.StartsWith("SDK4399"))
			{
				Login_otp = FeverAuth.SDK4399ToSauth(Login_otp);
			}
			string guid = GetGuid();
			string message = engineVersion + LibMinecraftPE + patchVersion + Patch + guid;
			PEAURequest pEAURequest = new PEAURequest();
			pEAURequest.sa_data = sa_data_PE;
			pEAURequest.engine_version = engineVersion;
			pEAURequest.patch_version = patchVersion;
			pEAURequest.message = message;
			pEAURequest.sauth_json = JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(Login_otp)["sauth_json"].ToString());
			pEAURequest.seed = guid;
			pEAURequest.sign = Base64Encode(CoreSign.PESignCount(message));
			StringContent content = new StringContent(x19Crypt.HttpEncrypt_g79v12(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(pEAURequest))).ToHex(), Encoding.UTF8, "application/json");
			string result = client.PostAsync(PECoreobt + "/pe-authentication", content).Result.Content.ReadAsStringAsync().Result;
			AuthResponse = x19Crypt.HttpDecrypt_g79v12_(result.HexToBytes());
			try
			{
				AuthenticationEntity = JsonConvert.DeserializeObject<AuthenticationResponse>(AuthResponse);
				UID = AuthenticationEntity.entity.entity_id;
				LoginSRCToken = AuthenticationEntity.entity.token;
				MD5Token = HashHelper.SafeCompleteMD5(Encoding.UTF8.GetBytes(LoginSRCToken));
			}
			catch (Exception ex)
			{
				Console.WriteLine("出错了？请切换账号后重试！");
				Console.WriteLine("登录响应：" + Regex.Unescape(AuthResponse));
				Console.WriteLine("错误信息：" + ex.ToString());
				return -1;
			}
			Console.WriteLine("登录成功！");
			UpdateLogin_();
			GetName();
			Console.WriteLine("UID:" + UID);
			return 0;
		}
		catch (Exception ex2)
		{
			Function.ClientError("[Login] " + ex2.ToString());
			return -1;
		}
	}

	public static string GetGuid()
	{
		return Guid.NewGuid().ToString();
	}

	public void GetName()
	{
		string value = x19Crypt.ComputeDynamicToken("/user-detail", "", LoginSRCToken);
		HttpContent httpContent = new StringContent("");
		httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		httpContent.Headers.Add("user-token", value);
		httpContent.Headers.Add("user-id", UID);
		string text = Regex.Unescape(client.PostAsync(apigatewayobt + "/user-detail", httpContent).Result.Content.ReadAsStringAsync().Result);
		player_info = text;
	}

	public void UpdateLogin_()
	{
		delete = true;
		try
		{
			UpdateAuth = new Thread((ThreadStart)delegate
			{
				while (delete)
				{
					Thread.Sleep(300000);
					UpdateAuthotp(coreobt);
				}
			});
			UpdateAuth.Start();
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}
	}

	public string postAPIPC(string requests, string url)
	{
		try
		{
			string value = x19Crypt.ComputeDynamicToken(url, requests, LoginSRCToken);
			HttpContent httpContent = new StringContent(requests);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			string result = client.PostAsync(apigatewayobt + url, httpContent).Result.Content.ReadAsStringAsync().Result;
			Console.WriteLine(Regex.Unescape(result));
			return result;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return null;
		}
	}

	public void Close()
	{
		try
		{
			UpdateAuth.Abort();
		}
		catch (Exception)
		{
		}
		try
		{
			Delete();
		}
		catch (Exception)
		{
		}
	}

	public void Delete()
	{
		string text = JsonConvert.SerializeObject(new AuthenticationDelete
		{
			user_id = UID
		});
		string value = x19Crypt.ComputeDynamicToken("/authentication/delete", text, LoginSRCToken);
		HttpContent httpContent = new StringContent(text);
		httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		httpContent.Headers.Add("user-token", value);
		httpContent.Headers.Add("user-id", UID);
		_ = client.PostAsync(app.Temp.url + "/authentication/delete", httpContent).Result.Content.ReadAsStringAsync().Result;
	}

	public string postAPIPE(string requests, string url)
	{
		try
		{
			string value = x19Crypt.ComputeDynamicToken(url, requests, LoginSRCToken);
			HttpContent httpContent = new StringContent(requests);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			string result = client.PostAsync(apigatewayobtPE + url, httpContent).Result.Content.ReadAsStringAsync().Result;
			Console.WriteLine(Regex.Unescape(result));
			return result;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return null;
		}
	}

	public string x19POST(string host, string requests, string url)
	{
		try
		{
			string value = x19Crypt.ComputeDynamicToken(url, requests, LoginSRCToken);
			HttpContent httpContent = new StringContent(requests);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			string result = client.PostAsync(host + url, httpContent).Result.Content.ReadAsStringAsync().Result;
			Console.WriteLine(Regex.Unescape(result));
			return result;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return null;
		}
	}

	public string GetAuthenticationOtpJson(string versionvaluas, string aid, string otp_token, string Sauth_json)
	{
		try
		{
			AuthoptSauth authoptSauth = new AuthoptSauth();
			VersionEntity versionEntity = new VersionEntity();
			AuthenticationEntity authenticationEntity = new AuthenticationEntity
			{
				sa_data = sa_data
			};
			authoptSauth = JsonConvert.DeserializeObject<AuthoptSauth>(Sauth_json);
			authenticationEntity.sauth_json = authoptSauth.sauth_json;
			versionEntity.version = versionvaluas;
			authenticationEntity.version = versionEntity;
			authenticationEntity.aid = aid;
			authenticationEntity.otp_token = otp_token;
			return JsonConvert.SerializeObject(authenticationEntity);
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return null;
		}
	}

	public string UpdateAuthotp(string url)
	{
		try
		{
			_ = string.Empty;
			string authenticationUpdate = GetAuthenticationUpdate(UID);
			string value = x19Crypt.ComputeDynamicToken("/authentication/update", authenticationUpdate, LoginSRCToken);
			HttpContent httpContent = new ByteArrayContent(x19Crypt.HttpEncrypt(Encoding.ASCII.GetBytes(authenticationUpdate)));
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			string keyStr;
			string text = CoreNative.HttpDecrypt(client.PostAsync(url + "/authentication/update", httpContent).Result.Content.ReadAsByteArrayAsync().Result, out keyStr);
			LoginSRCToken = JsonConvert.DeserializeObject<JObject>(text)["entity"]["token"].ToString();
			MD5Token = HashHelper.SafeCompleteMD5(Encoding.UTF8.GetBytes(LoginSRCToken));
			return text;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return null;
		}
	}

	public string GetAuthenticationUpdate(string uid)
	{
		return JsonConvert.SerializeObject(new AuthenticationEntity
		{
			entity_id = uid
		});
	}
}

