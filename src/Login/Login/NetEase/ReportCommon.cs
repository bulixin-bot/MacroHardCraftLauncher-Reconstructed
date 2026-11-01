using ConsoleAppLogin.NetEase;
using text;

namespace Login.NetEase;

internal class ReportCommon
{
	public static void Report(string UID, string message)
	{
		string text = Http.postAPIPC("{\"user_id\":\"" + UID + "\"}", "/user/query/search-by-uid");
		Http.postAPI("{\"msg\": {\"re_nickName\": \"" + app.displayName + "\", \"type\": \"聊天内容\", \"re_uid\": " + UID + ", \"chat\": {\"user_info\": " + text + ", \"refer_message_id\": \"\", \"words\": {\"quick\": false, \"type\": \"text\", \"data\": \"" + message + "\", \"message_visibility\": 0}, \"msg_type\": \"text\", \"uid\": " + UID + ", \"refer_text\": \"\", \"is_friend\": false, \"message_id\": \"7.60.95.158:8081-1737604474021263019-chat-37609-979\"}, \"uid\": " + UID + "}, \"type\": \"言行不雅\"}", "/user-moment-report");
	}
}

