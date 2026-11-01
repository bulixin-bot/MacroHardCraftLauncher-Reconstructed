using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netease4399Cli;

public static class Netease4399Login
{
	private static class Logger
	{
		public static bool Verbose { get; set; } = string.Equals(Environment.GetEnvironmentVariable("NETEASE4399_VERBOSE"), "1", StringComparison.Ordinal);

		public static void Debug(string msg)
		{
			if (Verbose)
			{
				Console.Error.WriteLine("[dbg] " + msg);
			}
		}

		public static void Info(string msg)
		{
			Console.Error.WriteLine("[info] " + msg);
		}

		public static void Warn(string msg)
		{
			Console.Error.WriteLine("[warn] " + msg);
		}

		public static void Error(string msg)
		{
			Console.Error.WriteLine("[err] " + msg);
		}

		public static void Error(Exception ex, string msg)
		{
			Console.Error.WriteLine("[err] " + msg + " :: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private class OAuthParams
	{
		public string ClientId = "";

		public string State = "";

		public string RedirectUri = "";

		public string D = "";

		public string BizId = "";

		public string Ref = "";

		public string OauthUrl = "";
	}

	private class OAuthCallbackData
	{
		public string? Uid;

		public string? State;
	}

	private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/129.0.0.0 Safari/537.36";

	private const string RedirectUri = "https://m.4399api.com/openapi/oauth-callback.html?gamekey=44770&game_key=115716";

	private const string SdkVersion = "3.18.0.682";

	public static void SetVerbose(bool verbose)
	{
		Logger.Verbose = verbose;
	}

	public static async Task<Login4399Result> LoginAsync(string username, string password, string? captchaId, string? captcha)
	{
		string submitCaptcha = captcha ?? string.Empty;
		string submitCaptchaId = captchaId ?? string.Empty;
		try
		{
			OAuthParams oAuthParams = await ParamsAsync();
			string loginUrl = "https://ptlogin.4399.com/oauth2/loginAndAuthorize.do?channel=&sdk=op&sdk_version=3.18.0.682";
			Dictionary<string, string> form = BuildLoginForm(username, password, submitCaptcha, submitCaptchaId, oAuthParams);
			using HttpClient client = CreateHttpClient();
			HttpResponseMessage obj = await client.GetAsync(oAuthParams.OauthUrl);
			obj.EnsureSuccessStatusCode();
			List<string> list = new List<string>();
			if (obj.Headers.TryGetValues("Set-Cookie", out IEnumerable<string> values))
			{
				foreach (string item in values)
				{
					string text = item.Split(';')[0].Trim();
					if (!string.IsNullOrEmpty(text))
					{
						list.Add(text);
					}
				}
			}
			string cookieHeader = string.Join("; ", list);
			string responseBody = null;
			HttpResponseMessage httpResponse = null;
			for (int attempt = 0; attempt < 20; attempt++)
			{
				HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, loginUrl)
				{
					Content = new FormUrlEncodedContent(form)
				};
				if (!string.IsNullOrEmpty(cookieHeader))
				{
					httpRequestMessage.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
				}
				httpResponse = await client.SendAsync(httpRequestMessage);
				responseBody = await httpResponse.Content.ReadAsStringAsync();
				if (httpResponse.StatusCode != HttpStatusCode.Accepted)
				{
					break;
				}
				Logger.Warn($"[4399] 触发 4399 服务器问题，第 {attempt + 1}/20 次重试");
				if (attempt < 19)
				{
					await Task.Delay(1000);
				}
			}
			if (httpResponse != null && httpResponse.StatusCode == HttpStatusCode.Accepted)
			{
				Logger.Error("[4399] 已重试 20 次仍触发服务器问题，停止重试");
			}
			if (string.IsNullOrEmpty(responseBody))
			{
				string text2 = httpResponse?.Headers.Location?.ToString();
				if (string.IsNullOrEmpty(text2))
				{
					return Login4399Result.Fail("登录成功但无重定向地址");
				}
				OAuthCallbackData oAuthCallbackData = await GetOAuthCallbackAsync(text2);
				if (oAuthCallbackData == null)
				{
					return Login4399Result.Fail("OAuth 回调解析失败");
				}
				string sauthJson = BuildSauthJson(oAuthCallbackData.Uid, oAuthCallbackData.State);
				using HttpClient sauthClient = CreateHttpClient();
				StringContent content = new StringContent(sauthJson, Encoding.UTF8, "application/json");
				HttpResponseMessage sauthResponse = await sauthClient.PostAsync("https://mgbsdk.matrix.netease.com/x19/sdk/uni_sauth", content);
				string text3 = await sauthResponse.Content.ReadAsStringAsync();
				Logger.Info($"[4399] uni_sauth 状态码: {sauthResponse.StatusCode}");
				Logger.Debug("[4399] uni_sauth 响应: " + text3);
				JObject jObject = null;
				try
				{
					jObject = JObject.Parse(text3);
				}
				catch
				{
				}
				if (jObject != null && (int?)jObject["code"] == 403)
				{
					string text4 = ((string?)jObject["msg"]) ?? "";
					string text5 = Regex.Replace(text4, "<ntsdk[^>]*>.*?</ntsdk>", "", RegexOptions.Singleline).Trim();
					if (string.IsNullOrEmpty(text5))
					{
						text5 = text4;
					}
					int valueOrDefault = ((int?)jObject["subcode"]).GetValueOrDefault();
					Logger.Warn($"[4399] uni_sauth 业务 403: subcode={valueOrDefault} msg={text5}");
					if (TryParseSmsChallenge(text5, out string content2, out string[] numbers))
					{
						return Login4399Result.NeedSmsVerification(content2, numbers, text5);
					}
					return Login4399Result.Fail("4399 安全验证：" + text5);
				}
				if (!sauthResponse.IsSuccessStatusCode)
				{
					return Login4399Result.Fail($"uni_sauth 请求失败 ({sauthResponse.StatusCode}): {text3}");
				}
				return Login4399Result.Ok(WrapSauthJson(sauthJson));
			}
			if (responseBody.Contains("验证码错误"))
			{
				return Login4399Result.NeedCaptcha("验证码错误，请重新输入");
			}
			string text6 = Regex.Match(responseBody, "id=\"login_err_msg\"\\s*>\\s*([^<]+)\\s*<").Groups[1].Value.Trim();
			return Login4399Result.Fail((!string.IsNullOrEmpty(text6)) ? text6 : "未知登录错误");
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "[4399] 登录异常");
			return Login4399Result.Fail(ex.Message);
		}
	}

	private static string WrapSauthJson(string sauthJson)
	{
		return new JObject { ["sauth_json"] = sauthJson }.ToString(Formatting.None);
	}

	private static bool TryParseSmsChallenge(string message, out string content, out string[] numbers)
	{
		content = string.Empty;
		numbers = Array.Empty<string>();
		if (string.IsNullOrWhiteSpace(message))
		{
			return false;
		}
		Match match = Regex.Match(message, "发送\\s*(?<content>[A-Za-z0-9\\-_]+)\\s*(?:至|到)\\s*(?<first>\\d{5,})\\s*(?:或|、|/)\\s*(?<second>\\d{5,})", RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			return false;
		}
		content = match.Groups["content"].Value;
		numbers = new string[2]
		{
			match.Groups["first"].Value,
			match.Groups["second"].Value
		};
		return true;
	}

	private static async Task<OAuthParams> ParamsAsync()
	{
		using HttpClient client = CreateHttpClient();
		string text = JObject.Parse(await client.GetStringAsync("https://m.4399api.com/openapi/oauth-callback.html?gamekey=44770&game_key=115716"))["result"]?.ToString();
		if (string.IsNullOrEmpty(text))
		{
			throw new InvalidOperationException("4399 没有返回 OAuth URL（oauth-callback.html 的 result 为空）");
		}
		Dictionary<string, string> dictionary = ParseQueryString(new Uri(text).Query);
		string value;
		string value2;
		string value3;
		string value4;
		string value5;
		string value6;
		return new OAuthParams
		{
			ClientId = (dictionary.TryGetValue("client_id", out value) ? value : ""),
			State = (dictionary.TryGetValue("state", out value2) ? value2 : ""),
			RedirectUri = (dictionary.TryGetValue("redirect_uri", out value3) ? value3 : ""),
			D = (dictionary.TryGetValue("_d", out value4) ? value4 : ""),
			BizId = (dictionary.TryGetValue("bizId", out value5) ? value5 : ""),
			Ref = (dictionary.TryGetValue("ref", out value6) ? value6 : ""),
			OauthUrl = text
		};
	}

	private static async Task<OAuthCallbackData?> GetOAuthCallbackAsync(string locationUrl)
	{
		using HttpClient client = CreateHttpClient();
		string text = await client.GetStringAsync(locationUrl);
		string text2 = ((text.Length > 500) ? (text.Substring(0, 500) + "…") : text);
		Logger.Debug("[4399] OAuth 回调响应: " + text2);
		JToken jToken;
		try
		{
			jToken = JToken.Parse(text);
		}
		catch (JsonException ex)
		{
			Logger.Warn("[4399] OAuth 回调不是合法 JSON: " + ex.Message);
			return null;
		}
		if (jToken["result"] is JObject jObject)
		{
			string text3 = (string?)jObject["uid"];
			string text4 = (string?)jObject["state"];
			if (!string.IsNullOrEmpty(text3))
			{
				return new OAuthCallbackData
				{
					Uid = text3,
					State = (text4 ?? "")
				};
			}
		}
		string text5 = (string?)jToken["uid"];
		if (!string.IsNullOrEmpty(text5))
		{
			return new OAuthCallbackData
			{
				Uid = text5,
				State = (((string?)jToken["state"]) ?? "")
			};
		}
		string text6 = ((string?)jToken["result"]) ?? ((string?)jToken["error"]) ?? ((string?)jToken["error_description"]);
		if (!string.IsNullOrEmpty(text6))
		{
			Logger.Warn("[4399] OAuth 回调未带 uid，疑似被拒：" + text6);
		}
		return null;
	}

	private static Dictionary<string, string> BuildLoginForm(string username, string password, string captcha, string captchaId, OAuthParams p)
	{
		return new Dictionary<string, string>
		{
			["auth_action"] = "ORILOGIN",
			["bizId"] = "2100001792",
			["captcha"] = captcha,
			["captcha_id"] = captchaId,
			["client_id"] = p.ClientId,
			["isInputRealname"] = "false",
			["isVaildRealname"] = "false",
			["password"] = password,
			["redirect_uri"] = "https://m.4399api.com/openapi/oauth-callback.html?gamekey=44770&game_key=115716",
			["ref"] = p.Ref,
			["response_type"] = "TOKEN",
			["scope"] = "basic",
			["sec"] = "0",
			["state"] = p.State,
			["username"] = username
		};
	}

	private static string BuildSauthJson(string? uid, string? state)
	{
		return new JObject
		{
			["aim_info"] = "{\"aim\":\"127.0.0.1\",\"country\":\"CN\",\"tz\":\"+0800\",\"tzid\":\"\"}",
			["realname"] = "{\"realname_type\":2}",
			["app_channel"] = "4399com",
			["platform"] = "ad",
			["client_login_sn"] = "4399_Gen",
			["gameid"] = "x19",
			["login_channel"] = "4399com",
			["sdk_version"] = "3.12.2",
			["sdkuid"] = uid,
			["sessionid"] = state,
			["udid"] = Guid.NewGuid().ToString("N").Substring(0, 16),
			["deviceid"] = "4399_Gen"
		}.ToString(Formatting.None);
	}

	private static HttpClient CreateHttpClient()
	{
		return new HttpClient(new HttpClientHandler
		{
			AllowAutoRedirect = false
		})
		{
			DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/129.0.0.0 Safari/537.36" } }
		};
	}

	private static Dictionary<string, string> ParseQueryString(string query)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		if (string.IsNullOrEmpty(query))
		{
			return dictionary;
		}
		if (query.StartsWith("?"))
		{
			query = query.Substring(1);
		}
		string[] array = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
		foreach (string text in array)
		{
			int num = text.IndexOf('=');
			if (num >= 0)
			{
				string key = WebUtility.UrlDecode(text.Substring(0, num));
				string value = WebUtility.UrlDecode(text.Substring(num + 1));
				dictionary[key] = value;
			}
		}
		return dictionary;
	}
}

