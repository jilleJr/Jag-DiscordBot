﻿using Discord;
using DiscordBot.Modules;
using DiscordBot.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot.Modules {
	public abstract class Module {
		public Bot bot { get; internal set; }
		public DiscordClient client { get { return bot == null ? null : bot.client; } }
		public virtual string modulePrefix { get { return null; } }
		public virtual string description { get { return null; } }
		public bool shutdown => bot?.valid ?? false;
		public string GetInformation() {
			if (string.IsNullOrWhiteSpace(modulePrefix)) return null;

			Command[] commands = GetCommands();
			string o = string.Empty;
			
			o += "**" + this.GetType().Name + " / " + modulePrefix + "**";
			if (!string.IsNullOrWhiteSpace(description))
				o += "\n**Description**:\n```\n" + description + "```\n";
			if (commands.Length > 0) {
				o += "\n**" + commands.Length + " available " + (commands.Length == 1 ? "command" : "commands") + ":**\n";
				Array.Sort(commands, (a,b) => a.id.CompareTo(b.id));
				o += "```\n" + commands.Sum(c =>
					c.fullUsage + "\n").Trim() + "```";
			}

			return o;
		}

		public abstract void Init();
		public abstract void Unload();

		#region Command methods
		internal void AddCommand<T>(Command<T> cmd) where T : Module {
			if (cmd.requires == CommandPerm.Selfbot && !bot.isSelfbot) return;

			cmd.bot = bot;
			cmd.module = this;
			cmd.id = cmd.name == null ? null : (string.IsNullOrWhiteSpace(modulePrefix) || !cmd.useModulePrefix ? "" : modulePrefix + ".") + cmd.name;
			Array.Sort(cmd.alias);

			Command conflict = null;
			if ((conflict = bot.commands.Find(c=>c.id == cmd.id)) != null)
				LogHelper.LogFailure("Command \"" + cmd.id + "\" (" + cmd.module.GetType().Name + ") is conflicting with command \"" + conflict.id + "\" (" + conflict.module.GetType().Name + ")! Skipping...");
			else {
				bot.commands.Add(cmd);
				LogHelper.LogSuccess("Registered command \"" + cmd.id + "\"");
			}
		}

		internal bool RemoveCommand<T>(Command<T> cmd) where T : Module {
			if (bot.commands.Contains(cmd))
				return bot.commands.Remove(cmd);
			return false;
		}

		internal bool RemoveCommand(string id) {
			return bot.commands.RemoveAll((cmd) => cmd.id == id) > 0;
		}

		public Command[] GetCommands() {
			return bot.commands.Where(cmd => cmd.module == this).ToArray();
		}
		#endregion

		#region Safe Message Wrappers
		public static async Task<Message> DynamicSendMessage(MessageEventArgs e, string text) {
			return await DiscordHelper.DynamicSendMessage(e, text);
		}

		public async Task<Message> DynamicEditMessage(Message message, User target, string text) {
			return await DiscordHelper.DynamicEditMessage(message, target, text);
		}
		#endregion

		// Temporary print function
		internal static void print(object o) {
			LogHelper.LogRawText(o);
		}
	}

}
