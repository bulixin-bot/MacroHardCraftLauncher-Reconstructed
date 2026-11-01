using System;
using System.Collections.Generic;

namespace Netease4399Cli;

public sealed class Login4399Result
{
	public bool Success { get; private init; }

	public bool CaptchaRequired { get; private init; }

	public bool SmsVerificationRequired { get; private init; }

	public string SmsContent { get; private init; } = string.Empty;

	public IReadOnlyList<string> SmsNumbers { get; private init; } = Array.Empty<string>();

	public string? ErrorMessage { get; private init; }

	public string? SauthJson { get; private init; }

	public static Login4399Result Ok(string sauthJson)
	{
		return new Login4399Result
		{
			Success = true,
			SauthJson = sauthJson
		};
	}

	public static Login4399Result NeedCaptcha(string msg)
	{
		return new Login4399Result
		{
			CaptchaRequired = true,
			ErrorMessage = msg
		};
	}

	public static Login4399Result Fail(string error)
	{
		return new Login4399Result
		{
			ErrorMessage = error
		};
	}

	public static Login4399Result NeedSmsVerification(string content, IReadOnlyList<string> numbers, string msg)
	{
		return new Login4399Result
		{
			SmsVerificationRequired = true,
			SmsContent = content,
			SmsNumbers = numbers,
			ErrorMessage = msg
		};
	}
}

