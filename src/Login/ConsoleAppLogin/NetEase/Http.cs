using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using FeverToSauth;
using Login.JavaHelper;
using Login.Native;
using Login.NetEase;
using Login.NetEase.Entity;
using Login.NetEase.LinkService;
using Login.NeteaseEntity;
using Login.PEEntity;
using Login.PEEntity.PEAuthentication;
using Login.loginauth;
using Login.loginauth.authentication_v2;
using MCStudio.Network.Http;
using Mark;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Noya.LocalServer.Common.Extensions;
using WPFLauncher.Network.Launcher;
using WPFLauncher.Util;
using text;
using text.loginauth;

namespace ConsoleAppLogin.NetEase;

public class Http
{
	public static readonly HttpClient client = new HttpClient();

	public static bool g79 = false;

	public static HttpListener httpobj;

	public static bool EncryptionAuthentication = true;

	public static bool IsAuthServer = false;

	public static string AuthServerUrl { get; set; }

	public static string server_id { get; set; }

	public static string server_ip { get; set; }

	public static string room_id_public { get; set; }

	public static bool istrue { get; set; }

	private static string response_data { get; set; }

	public static string UID { get; set; }

	public static string LastLoginError { get; set; } = "";

	public static string PEUID { get; set; }

	public static string URL { get; set; } = "https://x19apigatewayobt.nie.netease.com";

	public static string LoginSRCToken { get; set; }

	public static string LoginDToken { get; set; }

	public static Thread Thread { get; private set; }

	public static string HostAccount { get; set; }

	public static string HostPassword { get; set; }

	public static void Login(string Login_otp)
	{
		g79 = false;
		if (Thread != null)
		{
			Close();
		}
		if (Login_otp.StartsWith("SDK4399"))
		{
			Login_otp = FeverAuth.SDK4399ToSauth(Login_otp);
		}
		if (Login_otp == null)
		{
			return;
		}
		Function.ClientLog("[Login] create Login type:x19");
		string keyStr = string.Empty;
		LoginotpResposne loginotpResposne = new LoginotpResposne();
		new AuthenticationResponse();
		if (Login_otp == "")
		{
			Console.WriteLine("未检测到cookie");
		}
		StringContent content = new StringContent(Login_otp, Encoding.UTF8, "application/json");
		string result = client.PostAsync(app.Temp.url + "/login-otp", content).Result.Content.ReadAsStringAsync().Result;
		try
		{
			loginotpResposne = JsonConvert.DeserializeObject<LoginotpResposne>(result);
		}
		catch
		{
			Console.WriteLine("请求失败" + result);
			Environment.Exit(0);
		}
		if (loginotpResposne.code != 0)
		{
			Console.WriteLine("登录失败，请检查cookie：" + result);
			Environment.Exit(0);
		}
		string authenticationOtpJson = app.GetAuthenticationOtpJson("1.14.6.45947", loginotpResposne.entity.aid, loginotpResposne.entity.otp_token, Login_otp);
		Function.ClientLog("[Login] Login request: " + authenticationOtpJson);
		ByteArrayContent byteArrayContent = new ByteArrayContent(CoreNative.HttpEncrypt("/authentication-otp", authenticationOtpJson, out keyStr));
		byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		string text = CoreNative.ParseLoginResponse(client.PostAsync(app.Temp.url + "/authentication-otp", byteArrayContent).Result.Content.ReadAsByteArrayAsync().Result, out keyStr);
		Function.ClientLog("[Login] response:" + text);
		try
		{
			AuthenticationResponse authenticationResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(text);
			UID = authenticationResponse.entity.entity_id;
			Function.ClientLog("[Login] Login UID:" + UID);
			LoginDToken = authenticationResponse.entity.token;
			Function.ClientLog("[Login] Login Dtoken:" + LoginDToken);
			try
			{
				LoginSRCToken = MclNetClient.GetDecryptToken(LoginDToken);
				Function.ClientLog("[Login] Login token:" + LoginSRCToken);
			}
			catch (Exception ex)
			{
				Function.ClientError("[Login] Compute SRCToken:" + ex.ToString());
				LoginSRCToken = LoginDToken;
			}
			try
			{
				sd.i("RELEASE", uint.Parse(UID));
			}
			catch (Exception ex2)
			{
				Function.ClientError("[Libary] setuid error:" + ex2.ToString());
			}
			PEUID = JsonConvert.DeserializeObject<PEUid>(x19post("https://x19apigatewayobt.nie.netease.com", "/user-official-account-info/check", "{\"entity_id\":\"" + UID + "\"}")).entity.entity_id;
		}
		catch (Exception ex3)
		{
			Console.WriteLine("出错了？请切换账号后重试！");
			Console.WriteLine("错误信息：" + ex3.ToString());
			Console.WriteLine("登录响应：" + Regex.Unescape(text));
			Close();
		}
		Console.WriteLine("登录成功！");
		GetName();
		Console.WriteLine("UID:" + UID);
		UpdateLogin();
		app.Temp.Version = Function.GetVersion();
		Function.delete = true;
	}

	public static string GetLoginToken(string Login_otp)
	{
		Function.ClientLog("[Login] create Login type:x19");
		if (Login_otp.StartsWith("SDK4399"))
		{
			Login_otp = FeverAuth.SDK4399ToSauth(Login_otp);
		}
		if (Login_otp != null)
		{
			string keyStr = string.Empty;
			LoginotpResposne loginotpResposne = new LoginotpResposne();
			new AuthenticationResponse();
			if (Login_otp == "")
			{
				Console.WriteLine("未检测到cookie");
			}
			StringContent content = new StringContent(Login_otp, Encoding.UTF8, "application/json");
			string result = client.PostAsync(app.Temp.url + "/login-otp", content).Result.Content.ReadAsStringAsync().Result;
			try
			{
				loginotpResposne = JsonConvert.DeserializeObject<LoginotpResposne>(result);
			}
			catch
			{
				Console.WriteLine("请求失败" + result);
			}
			if (loginotpResposne.code != 0)
			{
				Console.WriteLine("登录失败，请检查cookie：" + result);
			}
			string authenticationOtpJson = app.GetAuthenticationOtpJson("1.14.6.45947", loginotpResposne.entity.aid, loginotpResposne.entity.otp_token, Login_otp);
			Function.ClientLog("[Login] Login request: " + authenticationOtpJson);
			ByteArrayContent byteArrayContent = new ByteArrayContent(CoreNative.HttpEncrypt("/authentication-otp", authenticationOtpJson, out keyStr));
			byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			string result2 = CoreNative.HttpDecrypt(client.PostAsync(app.Temp.url + "/authentication-otp", byteArrayContent).Result.Content.ReadAsByteArrayAsync().Result, out keyStr);
			Console.WriteLine("登录成功！");
			Console.WriteLine("UID:" + UID);
			return result2;
		}
		return null;
	}

	private static bool VerifyHostAccount()
	{
		try
		{
			if (string.IsNullOrEmpty(HostAccount) || string.IsNullOrEmpty(HostPassword))
			{
				return false;
			}
			JObject jObject = new JObject
			{
				["QQ"] = HostAccount,
				["Password"] = HostPassword
			};
			using WebClient webClient = new WebClient();
			webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
			webClient.Encoding = Encoding.UTF8;
			string json = webClient.UploadString("https://mc.coms.autos/api/internal/auth", jObject.ToString());
			JObject jObject2 = JObject.Parse(json);
			return jObject2["Success"] != null && jObject2["Success"].Value<bool>();
		}
		catch
		{
			return false;
		}
	}

	public static void PE_Login(string sauth)
	{
		if (!VerifyHostAccount())
		{
			Console.WriteLine("[PE] 账号校验未通过，登录被拒绝");
			return;
		}
		g79 = true;
		if (sauth.StartsWith("SDK4399"))
		{
			sauth = FeverAuth.SDK4399ToSauthPE(sauth);
		}
		if (sauth == null)
		{
			return;
		}
		string guid = Function.GetGuid();
		Function.ClientLog("[Login] create Login type:g79 guid: " + guid);
		string text = app.Temp.engineVersion + app.Temp.libminecraftpe + app.Temp.patchVersion + app.Temp.patch + guid;
		Function.ClientLog("[Login] Login message: " + text);
		string text2 = Function.Base64Encode(CoreSign.PESignCount(text));
		Function.ClientLog("[Login] Login sign: " + text2);
		string text3 = JsonConvert.SerializeObject(new PEAURequest
		{
			sa_data = app.ReadJson("sa_data_pe.json"),
			engine_version = app.Temp.engineVersion,
			patch_version = app.Temp.patchVersion,
			message = text,
			sauth_json = JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(sauth)["sauth_json"].ToString()),
			seed = guid,
			sign = text2
		});
		Function.ClientLog("[Login] Login request: " + text3);
		string text4 = CloudCrypto.EncryptHex(Encoding.UTF8.GetBytes(text3), HostAccount, HostPassword);
		if (string.IsNullOrEmpty(text4))
		{
			Function.ClientError("[PE] 云端加密失败，登录中止");
			return;
		}
		StringContent stringContent = new StringContent(text4, Encoding.UTF8, "application/json");
		stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		string result = client.PostAsync(app.Temp.PE_url + "/pe-authentication", stringContent).Result.Content.ReadAsStringAsync().Result;
		string text5 = CloudCrypto.DecryptHex(result, HostAccount, HostPassword);
		if (string.IsNullOrEmpty(text5))
		{
			Function.ClientError("[PE] 云端解密失败，登录中止");
			return;
		}
		Function.ClientLog("[Login] response:" + text5);
		UpdateLogin();
		LoadToken(text5);
	}

	public static string PE_GetLoginResponse(string sauth)
	{
		if (sauth.StartsWith("SDK4399"))
		{
			sauth = FeverAuth.SDK4399ToSauth(sauth);
		}
		if (sauth != null)
		{
			string guid = Function.GetGuid();
			Function.ClientLog("[Login] create Login type:g79 guid: " + guid);
			string text = app.Temp.engineVersion + app.Temp.libminecraftpe + app.Temp.patchVersion + app.Temp.patch + guid;
			Function.ClientLog("[Login] Login message: " + text);
			string text2 = Function.Base64Encode(CoreSign.PESignCount(text));
			Function.ClientLog("[Login] Login sign: " + text2);
			string text3 = JsonConvert.SerializeObject(new PEAURequest
			{
				sa_data = app.ReadJson("sa_data_pe.json"),
				engine_version = app.Temp.engineVersion,
				patch_version = app.Temp.patchVersion,
				message = text,
				sauth_json = JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(sauth)["sauth_json"].ToString()),
				seed = guid,
				sign = text2
			});
			Function.ClientLog("[Login] Login request: " + text3);
			string text4 = CloudCrypto.EncryptHex(Encoding.UTF8.GetBytes(text3), HostAccount, HostPassword);
			if (string.IsNullOrEmpty(text4))
			{
				Function.ClientError("[PE] 云端加密失败，登录中止");
				return null;
			}
			StringContent stringContent = new StringContent(text4, Encoding.UTF8, "application/json");
			stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			string result = client.PostAsync(app.Temp.PE_url + "/pe-authentication", stringContent).Result.Content.ReadAsStringAsync().Result;
			string text5 = CloudCrypto.DecryptHex(result, HostAccount, HostPassword);
			if (string.IsNullOrEmpty(text5))
			{
				Function.ClientError("[PE] 云端解密失败，登录中止");
				return null;
			}
			return text5;
		}
		return null;
	}

	public static void LoadToken(string Login_otp)
	{
		new LoginotpResposne();
		new AuthenticationResponse();
		string keyStr;
		string keyStr2;
		string text = CoreNative.ParseLoginResponse(CoreNative.HttpEncrypt("/authentication-otp", Login_otp, out keyStr), out keyStr2);
		LastLoginError = "";
		try
		{
			AuthenticationResponse authenticationResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(text);
			if (authenticationResponse == null)
			{
				LastLoginError = "登录响应为空，请稍后重试。";
				Function.ClientError("[Login] 响应解析失败：" + text);
				return;
			}
			if (authenticationResponse.entity == null || string.IsNullOrEmpty(authenticationResponse.entity.entity_id))
			{
				LastLoginError = (string.IsNullOrWhiteSpace(authenticationResponse.message) ? $"登录失败（code {authenticationResponse.code}）" : authenticationResponse.message);
				Function.ClientError("[Login] " + LastLoginError + " | 原始响应：" + text);
				return;
			}
			UID = authenticationResponse.entity.entity_id;
			Function.ClientLog("[Login] Login UID:" + UID);
			LoginDToken = authenticationResponse.entity.token;
			Function.ClientLog("[Login] Login Dtoken:" + LoginDToken);
			try
			{
				LoginSRCToken = MclNetClient.GetDecryptToken(LoginDToken);
				Function.ClientLog("[Login] Login token:" + LoginSRCToken);
			}
			catch (Exception ex)
			{
				Function.ClientError("[Login] Compute SRCToken:" + ex.ToString());
				LoginSRCToken = LoginDToken;
			}
			try
			{
				sd.i("RELEASE", uint.Parse(UID));
			}
			catch (Exception ex2)
			{
				Function.ClientError("[Libary] setuid error:" + ex2.ToString());
			}
			try
			{
				string value = x19post("https://x19apigatewayobt.nie.netease.com", "/user-official-account-info/check", "{\"entity_id\":\"" + UID + "\"}");
				PEUID = JsonConvert.DeserializeObject<PEUid>(value)?.entity?.entity_id;
			}
			catch (Exception ex3)
			{
				Function.ClientError("[Login] PEUID 获取失败:" + ex3.Message);
			}
		}
		catch (Exception ex4)
		{
			LastLoginError = "登录过程异常：" + ex4.Message;
			Console.WriteLine("出错了？请切换账号后重试！");
			Console.WriteLine("错误信息：" + ex4.ToString());
			Function.ClientError("[Login] error:" + ex4.ToString());
			Console.WriteLine("登录响应：" + Regex.Unescape(text));
			Close();
			return;
		}
		Console.WriteLine("登录成功！");
		GetName();
		Console.WriteLine("UID:" + UID);
		Function.delete = true;
		app.Temp.Version = Function.GetVersion();
	}

	public static void SetToken(string Token)
	{
		CoreNative.ParseLoginResponse(CoreNative.HttpEncrypt("/authentication-otp", Token, out var _), out var _);
	}

	public static void UpdateLogin()
	{
		Function.ClientLog("[AuthUpdate] Create Update");
		Thread = new Thread((ThreadStart)delegate
		{
			try
			{
				while (true)
				{
					Thread.Sleep(600000);
					string text = UpdateAuthotp();
					AuthenticationResponse authenticationResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(text);
					UID = authenticationResponse.entity.entity_id;
					LoginDToken = authenticationResponse.entity.token;
					try
					{
						LoginSRCToken = MclNetClient.GetDecryptToken(LoginDToken);
						Function.ClientLog("[Login] Login token:" + LoginSRCToken);
					}
					catch (Exception ex)
					{
						Function.ClientError("[Login] Compute SRCToken:" + ex.ToString());
						LoginSRCToken = LoginDToken;
					}
					Function.ClientLog("[AuthUpdate] Response:" + text);
					Function.ClientLog("[AuthUpdate] SRCToken:" + LoginSRCToken);
					Function.UpdateToken();
				}
			}
			catch (Exception ex2)
			{
				Function.ClientError("[AuthUpdate] " + ex2.Message);
			}
		});
		Thread.Start();
	}

	public static void Close()
	{
		Function.ClientLog("[AuthUpdate] Delete");
		Thread = null;
		Delete();
	}

	public static string UpdateAuthotp()
	{
		try
		{
			if (g79)
			{
				Function.ClientLog("[Authentication] Update");
				_ = string.Empty;
				string authenticationUpdate = GetAuthenticationUpdate(UID);
				string value = CoreNative.ComputeDynamicToken("/authentication/update", authenticationUpdate);
				HttpContent httpContent = new StringContent(x19Crypt.HttpEncrypt_g79v12(Encoding.ASCII.GetBytes(authenticationUpdate)).ToHex());
				httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
				httpContent.Headers.Add("user-token", value);
				httpContent.Headers.Add("user-id", UID);
				string text = x19Crypt.HttpDecrypt_g79v12_(client.PostAsync(app.Temp.PE_url + "/authentication/update", httpContent).Result.Content.ReadAsStringAsync().Result.HexToBytes());
				Function.ClientLog("[Login] response:" + text);
				SetToken(text);
				return text;
			}
			Function.ClientLog("[Authentication] Update");
			string keyStr = string.Empty;
			string authenticationUpdate2 = GetAuthenticationUpdate(UID);
			string value2 = CoreNative.ComputeDynamicToken("/authentication/update", authenticationUpdate2);
			HttpContent httpContent2 = new ByteArrayContent(CoreNative.HttpEncrypt("/authentication/update", authenticationUpdate2, out keyStr));
			httpContent2.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent2.Headers.Add("user-token", value2);
			httpContent2.Headers.Add("user-id", UID);
			string text2 = CoreNative.ParseLoginResponse(client.PostAsync(app.Temp.url + "/authentication/update", httpContent2).Result.Content.ReadAsByteArrayAsync().Result, out keyStr);
			Function.ClientLog("[Login] response:" + text2);
			return text2;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return null;
		}
	}

	public static string UpdateAuthotp(string url, string token)
	{
		try
		{
			Function.ClientLog("[Authentication] Update");
			string keyStr = string.Empty;
			string authenticationUpdate = GetAuthenticationUpdate(UID);
			string value = x19Crypt.ComputeDynamicToken("/authentication/update", authenticationUpdate, token);
			HttpContent httpContent = new ByteArrayContent(CoreNative.HttpEncrypt("/authentication/update", authenticationUpdate, out keyStr));
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			string text = CoreNative.HttpDecrypt(client.PostAsync(url + "/authentication/update", httpContent).Result.Content.ReadAsByteArrayAsync().Result, out keyStr);
			Function.ClientLog("[Login] response:" + text);
			return text;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return null;
		}
	}

	public static string GetAuthenticationUpdate(string uid)
	{
		return JsonConvert.SerializeObject(new AuthenticationEntity
		{
			entity_id = uid
		});
	}

	public static void Delete()
	{
		try
		{
			string text = JsonConvert.SerializeObject(new AuthenticationDelete
			{
				user_id = UID
			});
			string value = CoreNative.ComputeDynamicToken("/authentication/delete", text);
			HttpContent httpContent = new StringContent(text);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			string result = client.PostAsync(app.Temp.url + "/authentication/delete", httpContent).Result.Content.ReadAsStringAsync().Result;
			Function.ClientLog("[Authentication] Delete:" + result);
			Function.delete = false;
		}
		catch (Exception ex)
		{
			Function.ClientError("[Authentication] Delete error: " + ex.Message);
			Function.delete = false;
		}
	}

	public static void GetName()
	{
		string value = CoreNative.ComputeDynamicToken("/user-detail", "");
		HttpContent httpContent = new StringContent("");
		httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		httpContent.Headers.Add("user-token", value);
		httpContent.Headers.Add("user-id", UID);
		app.player_info = Regex.Unescape(client.PostAsync("https://x19apigatewayobt.nie.netease.com/user-detail", httpContent).Result.Content.ReadAsStringAsync().Result);
	}

	public static void SetName(string Name)
	{
		postAPIPC("{\"name\":\"" + Name + "\",\"entity_id\":\"" + UID + "\"}", "/nickname-setting");
	}

	public static void SearchPE(string room_info)
	{
		ItemSearch(room_info);
		LobbySearch(room_info);
		RoomWithName(room_info);
	}

	public static void Search(string room_info)
	{
		postAPIPC("{\"room_name\":\"" + room_info + "\",\"res_id\":\"\",\"version\":\"" + app.Temp.Version + "\",\"offset\":0,\"length\":50}", "/online-lobby-room/query/search-by-name");
		postAPIPC("{\"item_type\":2,\"keyword\":\"" + room_info + "\",\"master_type_id\":\"0\",\"network_tag\":false,\"year\":0,\"sort_type\":1,\"order\":0,\"price_type\":0,\"is_has\":false,\"offset\":0,\"length\":24}", "/item/query/search-by-keyword");
	}

	private static void ItemSearch(string room_info)
	{
		postAPI("{\"length\":10,\"keyword\":\"" + room_info + "\",\"offset\":0,\"sort_type\":1,\"asc_flag\":1}", "/pe-item/query/search-lobby-res-by-keyword");
	}

	private static void LobbySearch(string room_info)
	{
		postAPImclobtPE("{\"keyword\":\"" + room_info + "\",\"length\":10,\"offset\":0,\"version\":\"" + app.Temp.Version + "\"}", "/online-lobby-room/query/search-by-name-v2");
	}

	private static void RoomWithName(string room_info)
	{
		postAPIMcltransfer("{\"name\":\"" + room_info + "\",\"uid\":" + UID + "}", "/room-with-name");
	}

	public static void RoomListPE(string res_id)
	{
		try
		{
			ListEntityPE listEntityPE = JsonConvert.DeserializeObject<ListEntityPE>(postAPI("{\"res_id\":\"" + res_id + "\",\"version\":\"" + app.Temp.Version + "\",\"with_friend\":true,\"offset\":0,\"length\":10}", "/online-lobby-room/query/list-room-by-res-id"));
			for (int i = 0; i < listEntityPE.entities.Length; i++)
			{
				Function.OutRoomListPE(listEntityPE.entities[i]);
			}
		}
		catch (Exception innerException)
		{
			throw new FormatException("无法获取列表", innerException);
		}
	}

	public static void RoomList(string res_id)
	{
		try
		{
			string value = x19post("https://x19apigatewayobt.nie.netease.com", "/online-lobby-room/query/list-room-by-res-id", "{\"res_id\":\"" + res_id + "\",\"version\":\"" + app.Temp.Version + "\",\"with_friend\":true,\"offset\":0,\"length\":10}");
			ListEntity listEntity = new ListEntity();
			listEntity = JsonConvert.DeserializeObject<ListEntity>(value);
			for (int i = 0; i < listEntity.entities.Length; i++)
			{
				Function.OutRoomList(listEntity.entities[i]);
			}
		}
		catch (Exception innerException)
		{
			throw new FormatException("无法获取列表", innerException);
		}
	}

	public static void joined(string room_id)
	{
		if (JsonConvert.DeserializeObject<JObject>(postAPI("{\"room_id\":\"" + room_id + "\",\"password\":\"\",\"check_visibilily\":true}", "/online-lobby-room-enter"))["code"].ToString() == "0")
		{
			room_id_public = room_id;
		}
	}

	public static void left(string roomid)
	{
		postAPIPC("{\"room_id\":\"" + roomid + "\"}", "/online-lobby-room-enter/leave-room");
	}

	public static void GetIP()
	{
		postAPI("", "/online-lobby-game-enter");
	}

	public static void PurchaseItem(string itemid)
	{
		postAPImclobtPE("{\"item_id\": \"" + itemid + "\", \"expertcomment_info\": {\"expertcomment_id\": \"0\", \"video_url\": \"0\", \"expert_id\": \"0\"}, \"buy_path\": \"\\u9996\\u9875-\\u8054\\u673a\\u5927\\u5385-\\u8be6\\u60c5\\u9875:" + itemid + "\", \"coupon_ids\": [], \"cdk_code\": \"\"}", "/pe-purchase-item/");
	}

	public static void PurchaseItemPC(string itemid)
	{
		postAPIPC("{\"entity_id\":0,\"item_id\":\"" + itemid + "\",\"item_level\":0,\"user_id\":\"" + UID + "\",\"purchase_time\":0,\"last_play_time\":0,\"total_play_time\":0,\"receiver_id\":\"\",\"buy_path\":\"PC_H5_COMPONENT_DETAIL\",\"coupon_ids\":null,\"diamond\":0,\"activity_name\":\"\",\"batch_count\":1}", "/user-item-purchase");
	}

	public static void aicommand(string rid, string command)
	{
		postAPI("{\"type\": \"game_ai_command_prompt\",\"data\": {\"room\": \"" + rid + "\",\"game_type\": \"联机大厅\",\"content\": \"" + command + "\",\"send_error\": \"\",\"send_success\": true,\"type\": \"打字\"}}", "/salog");
		postAPI("{\"type\": \"game_ai_command_common_send\", \"data\": {\"content\": \"" + command + "\", \"game_type\": \"联机大厅\", \"room\": \"" + rid + "\", \"correct\": false}}", "/salog");
	}

	public static void NameUpdate(string name)
	{
		postAPIPC("{\"name\":\"" + name + "\"}", "/pc-nickname-setting/update");
		GetName();
		Function.GetName(app.player_info);
	}

	public static void SearchItem(string itemid)
	{
		postAPIPC("{\"item_id_list\":[\"" + itemid + "\"],\"channel_id\":11}", "/item-channel/query/search-item-channel-list-by-id");
		postAPIPC("{\"item_ids\":[\"" + itemid + "\"],\"entity_ids\":[]}", "/item/query/search-mcgame-item-list");
	}

	public static void GetGame()
	{
		postAPIPC("{\"os\":\"10.0\",\"version\":100000000,\"entity_id\":null}", "/cpp-game-client-info");
	}

	public static string postAPI(string requests, string url, bool output = true)
	{
		try
		{
			string value = CoreNative.ComputeDynamicToken(url, requests);
			HttpContent httpContent = new StringContent(requests);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			Console.WriteLine("https://g79apigatewayobt.minecraft.cn" + url);
			string text = Regex.Unescape(client.PostAsync("https://g79apigatewayobt.minecraft.cn" + url, httpContent).Result.Content.ReadAsStringAsync().Result);
			if (output)
			{
				Console.WriteLine(text);
			}
			return text;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return null;
		}
	}

	public static string postAPIPC(string requests, string url, bool displaylog = true)
	{
		try
		{
			Function.ClientLog("[ApiGateway] Url:" + url);
			Function.ClientLog("[ApiGateway] Request:" + requests);
			string value = CoreNative.ComputeDynamicToken(url, requests);
			HttpContent httpContent = new StringContent(requests);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			string result = client.PostAsync("https://x19apigatewayobt.nie.netease.com" + url, httpContent).Result.Content.ReadAsStringAsync().Result;
			Function.ClientLog("[ApiGateway] Response:" + result);
			if (displaylog)
			{
				Console.WriteLine(Regex.Unescape(result));
			}
			return result;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return null;
		}
	}

	public static string postAPIPC_NoCommon(string requests, string url, string token, string uid, bool DisplayLog = true)
	{
		try
		{
			string value = x19Crypt.ComputeDynamicToken(url, requests, token);
			HttpContent httpContent = new StringContent(requests);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", uid);
			string text = Regex.Unescape(client.PostAsync("https://x19apigatewayobt.nie.netease.com" + url, httpContent).Result.Content.ReadAsStringAsync().Result);
			if (DisplayLog)
			{
				Console.WriteLine(text);
			}
			return text;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
			return null;
		}
	}

	public static string postAPIHost(string requests, string url, string host)
	{
		try
		{
			string value = CoreNative.ComputeDynamicToken(url, requests);
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

	public static string postAPIHostByte(byte[] requests, string url, string host)
	{
		try
		{
			string value = CoreNative.ComputeDynamicToken(url, Encoding.ASCII.GetString(requests));
			HttpContent httpContent = new ByteArrayContent(requests);
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

	public static string postAPIHost(string requests, string url, string host, string uid, string token)
	{
		try
		{
			string value = x19Crypt.ComputeDynamicToken(url, requests, token);
			HttpContent httpContent = new StringContent(requests);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", uid);
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

	public static string x19post(string host, string url, string data)
	{
		try
		{
			Function.ClientLog("[ApiGateway] Url:" + url);
			Function.ClientLog("[ApiGateway] Request:" + data);
			string result = new HttpClient
			{
				DefaultRequestHeaders = 
				{
					{ "user-id", UID },
					{
						"user-token",
						CoreNative.ComputeDynamicToken(url, data)
					}
				}
			}.PostAsync(content: new StringContent(data, Encoding.UTF8, "application/json"), requestUri: host + url).Result.Content.ReadAsStringAsync().Result;
			Function.ClientLog("[ApiGateway] Response:" + result);
			return result;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return null;
		}
	}

	public static string x19post(string host, string url, string data, byte[] MD5token, string uid)
	{
		try
		{
			Function.ClientLog("[ApiGateway] Url:" + url);
			Function.ClientLog("[ApiGateway] Request:" + data);
			string result = new HttpClient
			{
				DefaultRequestHeaders = 
				{
					{ "user-id", uid },
					{
						"user-token",
						x19Crypt.ComputeDynamicToken(url, data, MD5token)
					}
				}
			}.PostAsync(content: new StringContent(data, Encoding.UTF8, "application/json"), requestUri: host + url).Result.Content.ReadAsStringAsync().Result;
			Function.ClientLog("[ApiGateway] Response:" + result);
			return result;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return null;
		}
	}

	public static void postAPImclobtPE(string requests, string url)
	{
		try
		{
			string value = CoreNative.ComputeDynamicToken(url, requests);
			HttpContent httpContent = new StringContent(requests);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			Console.WriteLine(Regex.Unescape(client.PostAsync("https://g79mclobt.minecraft.cn" + url, httpContent).Result.Content.ReadAsStringAsync().Result));
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}
	}

	public static void postAPIMcltransfer(string requests, string url)
	{
		try
		{
			HttpContent httpContent = new StringContent(requests);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", CoreNative.ComputeDynamicToken(url, requests));
			httpContent.Headers.Add("user-id", UID);
			Console.WriteLine(Regex.Unescape(client.PostAsync("https://g79mcltransfer.minecraft.cn" + url, httpContent).Result.Content.ReadAsStringAsync().Result));
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}
	}

	public static string EncyptPOST(string url, string requests, string host)
	{
		try
		{
			Function.ClientLog("[CommonEncrypt] Url:" + url);
			Function.ClientLog("[CommonEncrypt] Request:" + requests);
			string keyStr = string.Empty;
			string value = CoreNative.ComputeDynamicToken(url, requests);
			HttpContent httpContent = new ByteArrayContent(CoreNative.HttpEncrypt(url, requests, out keyStr));
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			string text = CoreNative.HttpDecrypt(client.PostAsync(host + url, httpContent).Result.Content.ReadAsByteArrayAsync().Result, out keyStr);
			Console.WriteLine(Regex.Unescape(text));
			Function.ClientLog("[CommonEncrypt] Response:" + text);
			return text;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return null;
		}
	}

	public static string EncyptPOST_g79v12(string url, string requests, string host)
	{
		try
		{
			Function.ClientLog("[CommonEncrypt] Url:" + url);
			Function.ClientLog("[CommonEncrypt] Request:" + requests);
			string value = CoreNative.ComputeDynamicToken(url, requests);
			HttpContent httpContent = new StringContent(x19Crypt.HttpEncrypt_g79v12(Encoding.UTF8.GetBytes(requests)).ToHex());
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			string text = x19Crypt.HttpDecrypt_g79v12_(client.PostAsync(host + url, httpContent).Result.Content.ReadAsStringAsync().Result.HexToBytes());
			Console.WriteLine(text);
			Function.ClientLog("[CommonEncrypt] Response:" + text);
			return text;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return null;
		}
	}

	public static string EncyptPOST_g79v3(string url, string requests, string host)
	{
		try
		{
			Function.ClientLog("[CommonEncrypt] Url:" + url);
			Function.ClientLog("[CommonEncrypt] Request:" + requests);
			string value = CoreNative.ComputeDynamicToken(url, requests);
			HttpContent httpContent = new StringContent(x19Crypt.HttpEncrypt_g79v3(Encoding.UTF8.GetBytes(requests)).ToHex());
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			string text = x19Crypt.ParseLoginResponse_g79v3(client.PostAsync(host + url, httpContent).Result.Content.ReadAsStringAsync().Result.HexToBytes());
			Console.WriteLine(Regex.Unescape(text));
			Function.ClientLog("[CommonEncrypt] Response:" + text);
			return text;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return null;
		}
	}

	public static string getAPIPC(string url, bool output = true)
	{
		try
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("https://x19apigatewayobt.nie.netease.com" + url);
			string value = CoreNative.ComputeDynamicToken(url, "");
			httpWebRequest.Headers["user-id"] = UID;
			httpWebRequest.Headers["user-token"] = value;
			string str = new StreamReader(((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream(), Encoding.UTF8).ReadToEnd();
			if (output)
			{
				Console.WriteLine(Regex.Unescape(str));
			}
			response_data = str;
			istrue = false;
			return response_data;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return null;
		}
	}

	public static void KickAsync(string uid)
	{
		new Thread((ThreadStart)delegate
		{
			postAPIPC("{\"room_id\":\"" + room_id_public + "\",\"user_id\":" + uid + "}", "/online-lobby-member-kick");
		}).Start();
	}

	public static void Result()
	{
		HttpListenerContext context = httpobj.GetContext();
		HttpListenerRequest request = context.Request;
		HttpListenerResponse response = context.Response;
		string text = Guid.NewGuid().ToString();
		Console.ForegroundColor = ConsoleColor.White;
		Console.WriteLine("接到新的请求:" + text + ",时间：" + DateTime.Now.ToString() + " Url:" + request.RawUrl);
		context.Response.ContentType = "text/plain;charset=UTF-8";
		context.Response.AddHeader("Content-type", "text/plain");
		context.Response.ContentEncoding = Encoding.UTF8;
		string text2 = "";
		text2 = ((!(request.HttpMethod == "POST") || request.InputStream == null) ? "1" : HandleRequest(request, response, EncryptionAuthentication));
		byte[] bytes = Encoding.UTF8.GetBytes(text2);
		try
		{
			using Stream stream = response.OutputStream;
			stream.Write(bytes, 0, bytes.Length);
		}
		catch (Exception ex)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("网络蹦了：" + ex.ToString());
		}
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine("请求处理完成：" + text + ",时间：" + DateTime.Now.ToString() + "\r\n");
		Console.ForegroundColor = ConsoleColor.Gray;
	}

	public static void Result_PCCheck()
	{
		HttpListenerContext context = httpobj.GetContext();
		HttpListenerRequest request = context.Request;
		HttpListenerResponse response = context.Response;
		string text = Guid.NewGuid().ToString();
		Console.ForegroundColor = ConsoleColor.White;
		Console.WriteLine("接到新的请求:" + text + ",时间：" + DateTime.Now.ToString() + " Url:" + request.RawUrl);
		context.Response.ContentType = "text/plain;charset=UTF-8";
		context.Response.AddHeader("Content-type", "text/plain");
		context.Response.ContentEncoding = Encoding.UTF8;
		string text2 = "";
		text2 = ((!(request.HttpMethod == "POST") || request.InputStream == null) ? "1" : HandleRequestPCCheck(request, response));
		byte[] bytes = Encoding.UTF8.GetBytes(text2);
		try
		{
			using Stream stream = response.OutputStream;
			stream.Write(bytes, 0, bytes.Length);
		}
		catch (Exception ex)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("网络蹦了：" + ex.ToString());
		}
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine("请求处理完成：" + text + ",时间：" + DateTime.Now.ToString() + "\r\n");
		Console.ForegroundColor = ConsoleColor.Gray;
	}

	public static void Result_v2()
	{
		HttpListenerContext context = httpobj.GetContext();
		HttpListenerRequest request = context.Request;
		HttpListenerResponse response = context.Response;
		string text = Guid.NewGuid().ToString();
		Console.ForegroundColor = ConsoleColor.White;
		Console.WriteLine("接到新的请求:" + text + ",时间：" + DateTime.Now.ToString() + " Url:" + request.RawUrl);
		context.Response.ContentType = "text/plain;charset=UTF-8";
		context.Response.AddHeader("Content-type", "text/plain");
		context.Response.ContentEncoding = Encoding.UTF8;
		string s = "";
		if (request.HttpMethod == "POST" && request.InputStream != null)
		{
			if (request.RawUrl == "/api/phoenix/login")
			{
				s = HandleRequest_v2(request, response, EncryptionAuthentication);
			}
			if (request.RawUrl == "/api/phoenix/transfer_check_num")
			{
				s = HandleRequest_checkNum(request, response, EncryptionAuthentication);
			}
		}
		else
		{
			s = "1";
		}
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		try
		{
			using Stream stream = response.OutputStream;
			stream.Write(bytes, 0, bytes.Length);
		}
		catch (Exception ex)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("网络蹦了：" + ex.ToString());
		}
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine("请求处理完成：" + text + ",时间：" + DateTime.Now.ToString() + "\r\n");
		Console.ForegroundColor = ConsoleColor.Gray;
	}

	public static void Result_v3()
	{
		HttpListenerContext context = httpobj.GetContext();
		HttpListenerRequest request = context.Request;
		HttpListenerResponse response = context.Response;
		string text = Guid.NewGuid().ToString();
		Console.ForegroundColor = ConsoleColor.White;
		Console.WriteLine("接到新的请求:" + text + ",时间：" + DateTime.Now.ToString() + " Url:" + request.RawUrl);
		context.Response.ContentType = "text/plain;charset=UTF-8";
		context.Response.AddHeader("Content-type", "text/plain");
		context.Response.ContentEncoding = Encoding.UTF8;
		string s = "";
		if (request.HttpMethod == "POST" && request.InputStream != null)
		{
			if (request.RawUrl == "/authentication")
			{
				s = HandleRequest_v3(request, response, EncryptionAuthentication);
			}
		}
		else
		{
			s = "1";
		}
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		try
		{
			using Stream stream = response.OutputStream;
			stream.Write(bytes, 0, bytes.Length);
		}
		catch (Exception ex)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("网络蹦了：" + ex.ToString());
		}
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine("请求处理完成：" + text + ",时间：" + DateTime.Now.ToString() + "\r\n");
		Console.ForegroundColor = ConsoleColor.Gray;
	}

	private static string HandleRequest(HttpListenerRequest request, HttpListenerResponse response, bool Encrypt)
	{
		string text = null;
		string text2 = null;
		string array2;
		try
		{
			List<byte> list = new List<byte>();
			byte[] array = new byte[2048];
			int num = 0;
			int num2 = 0;
			do
			{
				num = request.InputStream.Read(array, 0, array.Length);
				num2 += num;
				list.AddRange(array);
			}
			while (num != 0);
			text = Encoding.UTF8.GetString(list.ToArray(), 0, num2);
			if (!LinkConnection.Is_Connected)
			{
				app.linkConnection = new LinkConnection();
			}
			string empty = string.Empty;
			empty = ((!Encrypt) ? text : x19Crypt.HttpDecrypt_g79v12(text.HexToBytes()));
			Console.WriteLine(empty);
			authenticationg79 authenticationg = new authenticationg79();
			JObject jObject = JsonConvert.DeserializeObject<JObject>(empty);
			authenticationg.bit = "64";
			authenticationg.clientKey = jObject["clientKey"].ToString();
			authenticationg.displayName = app.displayName;
			authenticationg.engineVersion = app.Temp.engineVersion;
			authenticationg.netease_sid = server_id;
			authenticationg.os_name = "android";
			authenticationg.patchVersion = app.Temp.patchVersion;
			authenticationg.uid = long.Parse(UID);
			string text3 = JsonConvert.SerializeObject(authenticationg);
			string empty2 = string.Empty;
			empty2 = ((!Encrypt) ? "/authentication" : "/authentication-v2");
			CoreNative.ComputeDynamicToken(empty2, text3);
			text2 = ((!Encrypt) ? HttpPost(empty2, text3, "https://g79authobt.minecraft.cn", out array2) : HttpPost_v2(empty2, text3, "https://g79authobt.minecraft.cn", out array2));
			Console.WriteLine(text2);
		}
		catch (Exception ex)
		{
			response.StatusDescription = "403";
			response.StatusCode = 403;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("在接收数据时发生错误:" + ex.ToString());
			return "在接收数据时发生错误:" + ex.ToString();
		}
		response.StatusDescription = "200";
		response.StatusCode = 200;
		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine("接收数据完成:" + text.Trim() + ",时间：" + DateTime.Now.ToString());
		return array2;
	}

	private static string HandleRequestPCCheck(HttpListenerRequest request, HttpListenerResponse response)
	{
		string text = null;
		string array2;
		try
		{
			List<byte> list = new List<byte>();
			byte[] array = new byte[2048];
			int num = 0;
			int num2 = 0;
			do
			{
				num = request.InputStream.Read(array, 0, array.Length);
				num2 += num;
				list.AddRange(array);
			}
			while (num != 0);
			text = Encoding.UTF8.GetString(list.ToArray(), 0, num2);
			string empty = string.Empty;
			empty = x19Crypt.HttpDecrypt_g79v12(text.HexToBytes());
			Console.WriteLine(empty);
			authenticationx19 authenticationx = new authenticationx19();
			JObject jObject = JsonConvert.DeserializeObject<JObject>(empty);
			authenticationx.bit = "32";
			authenticationx.clientKey = jObject["clientKey"].ToString();
			authenticationx.displayName = app.displayName;
			authenticationx.engineVersion = app.Temp.engineVersion;
			authenticationx.netease_sid = server_id;
			authenticationx.platform = "pc";
			authenticationx.os_name = "windows";
			authenticationx.pcCheck = "0";
			authenticationx.patchVersion = "";
			authenticationx.uid = long.Parse(UID);
			string text2 = JsonConvert.SerializeObject(authenticationx);
			_ = string.Empty;
			CoreNative.ComputeDynamicToken("/authentication-v2", text2);
			Console.WriteLine(HttpPost_v2("/authentication-v2", text2, "https://g79authobt.minecraft.cn", out array2));
		}
		catch (Exception ex)
		{
			response.StatusDescription = "403";
			response.StatusCode = 403;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("在接收数据时发生错误:" + ex.ToString());
			return "在接收数据时发生错误:" + ex.ToString();
		}
		response.StatusDescription = "200";
		response.StatusCode = 200;
		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine("接收数据完成:" + text.Trim() + ",时间：" + DateTime.Now.ToString());
		return array2;
	}

	private static string HandleRequest_v2(HttpListenerRequest request, HttpListenerResponse response, bool Encrypt)
	{
		string text = null;
		string text2 = null;
		try
		{
			List<byte> list = new List<byte>();
			byte[] array = new byte[2048];
			int num = 0;
			int num2 = 0;
			do
			{
				num = request.InputStream.Read(array, 0, array.Length);
				num2 += num;
				list.AddRange(array);
			}
			while (num != 0);
			text = Encoding.UTF8.GetString(list.ToArray(), 0, num2);
			Console.WriteLine(text);
			string clientKey = JsonConvert.DeserializeObject<JObject>(text)["client_public_key"].ToString();
			string text3 = JsonConvert.SerializeObject(new authenticationx19
			{
				bit = "32",
				clientKey = clientKey,
				displayName = app.displayName,
				engineVersion = app.Temp.engineVersion,
				netease_sid = server_id,
				os_name = "windows",
				patchVersion = "",
				pcCheck = "0",
				platform = "pc",
				uid = uint.Parse(UID)
			});
			_ = string.Empty;
			CoreNative.ComputeDynamicToken("/authentication-v2", text3);
			text2 = HttpPost_v2("/authentication-v2", text3, "https://g79authobt.minecraft.cn", out var _);
			text2 = text2.Substring(0, text2.IndexOf("\"]}") + 3);
			text2 = JsonConvert.SerializeObject(new fbEntity
			{
				success = true,
				server_msg = "感谢您使用TLSP验证服务！",
				token = "",
				respond_to = "",
				growth_level = 14,
				ip_address = server_ip,
				chainInfo = text2
			});
			Console.WriteLine(text2);
		}
		catch (Exception ex)
		{
			response.StatusDescription = "403";
			response.StatusCode = 403;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("在接收数据时发生错误:" + ex.ToString());
			return "在接收数据时发生错误:" + ex.ToString();
		}
		response.StatusDescription = "200";
		response.StatusCode = 200;
		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine("接收数据完成:" + text.Trim() + ",时间：" + DateTime.Now.ToString());
		return text2;
	}

	private static string HandleRequest_checkNum(HttpListenerRequest request, HttpListenerResponse response, bool Encrypt)
	{
		string text = null;
		string text2 = null;
		try
		{
			List<byte> list = new List<byte>();
			byte[] array = new byte[2048];
			int num = 0;
			int num2 = 0;
			do
			{
				num = request.InputStream.Read(array, 0, array.Length);
				num2 += num;
				list.AddRange(array);
			}
			while (num != 0);
			text = Encoding.UTF8.GetString(list.ToArray(), 0, num2);
			text2 = x19post("http://127.0.0.1:24793", "/check-num", text);
			Console.WriteLine(text2);
		}
		catch (Exception ex)
		{
			response.StatusDescription = "403";
			response.StatusCode = 403;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("在接收数据时发生错误:" + ex.ToString());
			return "在接收数据时发生错误:" + ex.ToString();
		}
		response.StatusDescription = "200";
		response.StatusCode = 200;
		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine("接收数据完成:" + text.Trim() + ",时间：" + DateTime.Now.ToString());
		return text2;
	}

	private static string HandleRequest_v3(HttpListenerRequest request, HttpListenerResponse response, bool Encrypt)
	{
		string text = null;
		string text2 = null;
		try
		{
			List<byte> list = new List<byte>();
			byte[] array = new byte[2048];
			int num = 0;
			int num2 = 0;
			do
			{
				num = request.InputStream.Read(array, 0, array.Length);
				num2 += num;
				list.AddRange(array);
			}
			while (num != 0);
			text = Encoding.UTF8.GetString(list.ToArray(), 0, num2);
			if (!LinkConnection.Is_Connected)
			{
				app.linkConnection = new LinkConnection();
			}
			Console.WriteLine(text);
			string clientKey = JsonConvert.DeserializeObject<JObject>(text)["identityPublicKey"].ToString();
			string param = JsonConvert.SerializeObject(new authenticationg79
			{
				bit = "64",
				clientKey = clientKey,
				displayName = app.displayName,
				engineVersion = app.Temp.engineVersion,
				netease_sid = server_id,
				os_name = "android",
				patchVersion = app.Temp.patchVersion,
				uid = uint.Parse(UID)
			});
			_ = string.Empty;
			text2 = HttpPost_v2("/authentication-v2", param, "https://g79authobt.minecraft.cn", out var _);
			text2 = text2.Substring(0, text2.IndexOf("\"]}") + 3);
			Console.WriteLine(text2);
		}
		catch (Exception ex)
		{
			response.StatusDescription = "403";
			response.StatusCode = 403;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("在接收数据时发生错误:" + ex.ToString());
			return "在接收数据时发生错误:" + ex.ToString();
		}
		response.StatusDescription = "200";
		response.StatusCode = 200;
		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine("接收数据完成:" + text.Trim() + ",时间：" + DateTime.Now.ToString());
		return text2;
	}

	public static string HttpPost(string url, string param, string host, out string array)
	{
		HttpClient httpClient = new HttpClient();
		HttpContent httpContent = new StringContent(param);
		httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		httpContent.Headers.Add("user-token", Encoding.ASCII.GetBytes(CoreNative.ComputeDynamicToken(url, param)).ToHex(toUpper: true));
		httpContent.Headers.Add("user-id", UID);
		httpClient.DefaultRequestHeaders.Add("User-Agent", "libhttpclient/1.0.0.0");
		httpClient.DefaultRequestHeaders.ExpectContinue = false;
		return array = httpClient.PostAsync(host + url, httpContent).Result.Content.ReadAsStringAsync().Result;
	}

	public static string HttpPost_(string url, string param, string host)
	{
		HttpClient httpClient = new HttpClient();
		HttpContent httpContent = new StringContent(param);
		httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		httpContent.Headers.Add("user-token", Encoding.ASCII.GetBytes(CoreNative.ComputeDynamicToken(url, param)).ToHex(toUpper: true));
		httpContent.Headers.Add("user-id", UID);
		httpClient.DefaultRequestHeaders.Add("accept-encoding", "gzip");
		httpClient.DefaultRequestHeaders.ExpectContinue = false;
		return httpClient.PostAsync(host + url, httpContent).Result.Content.ReadAsStringAsync().Result;
	}

	public static string HttpPost_v2(string url, string param, string host, out string array)
	{
		HttpClient httpClient = new HttpClient();
		HttpContent httpContent = new StringContent(x19Crypt.HttpEncrypt_g79v12(Encoding.UTF8.GetBytes(param)).ToHex());
		httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		httpContent.Headers.Add("user-token", Encoding.ASCII.GetBytes(CoreNative.ComputeDynamicToken(url, param)).ToHex(toUpper: true));
		httpContent.Headers.Add("user-id", UID);
		httpClient.DefaultRequestHeaders.Add("User-Agent", "libhttpclient/1.0.0.0");
		httpClient.DefaultRequestHeaders.ExpectContinue = false;
		return x19Crypt.HttpDecrypt_g79v12((array = httpClient.PostAsync(host + url, httpContent).Result.Content.ReadAsStringAsync().Result).HexToBytes());
	}

	public static string HttpPost_v2_(string url, string param, string host)
	{
		HttpClient httpClient = new HttpClient();
		HttpContent httpContent = new StringContent(x19Crypt.HttpEncrypt_g79v12(Encoding.UTF8.GetBytes(param)).ToHex());
		httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		httpContent.Headers.Add("user-token", Encoding.ASCII.GetBytes(CoreNative.ComputeDynamicToken(url, param)).ToHex(toUpper: true));
		httpContent.Headers.Add("user-id", UID);
		httpClient.DefaultRequestHeaders.Add("accept-encoding", "gzip");
		httpClient.DefaultRequestHeaders.ExpectContinue = false;
		return x19Crypt.HttpDecrypt_g79v12(httpClient.PostAsync(host + url, httpContent).Result.Content.ReadAsStringAsync().Result.HexToBytes());
	}

	public static void Authserver(int port)
	{
		try
		{
			httpobj = new HttpListener();
			string text = "http://127.0.0.1:" + port + "/";
			httpobj.Prefixes.Add(text);
			httpobj.Start();
			new Thread((ThreadStart)delegate
			{
				while (true)
				{
					Result();
				}
			}).Start();
			AuthServerUrl = text;
			Console.WriteLine("服务端初始化完毕，正在等待客户端请求,时间：" + DateTime.Now.ToString() + "\r\n");
			IsAuthServer = true;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}
	}

	public static void Authserver_PCCheck(int port)
	{
		try
		{
			httpobj = new HttpListener();
			string text = "http://127.0.0.1:" + port + "/";
			httpobj.Prefixes.Add(text);
			httpobj.Start();
			new Thread((ThreadStart)delegate
			{
				while (true)
				{
					Result_PCCheck();
				}
			}).Start();
			AuthServerUrl = text;
			Console.WriteLine("服务端初始化完毕，正在等待客户端请求,时间：" + DateTime.Now.ToString() + "\r\n");
			IsAuthServer = true;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}
	}
}

