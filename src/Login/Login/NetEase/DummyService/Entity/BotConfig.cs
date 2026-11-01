using System.Collections.Generic;

namespace Login.NetEase.DummyService.Entity;

internal class BotConfig
{
	public string message_pe;

	public string message_pc;

	public int task_time;

	public int dummy_level { get; set; }

	public List<WhiteList> white_list { get; set; }
}

