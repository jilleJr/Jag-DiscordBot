﻿using Discord;
using Discord.Commands;
using DiscordBot.Utility;
using Nito.AsyncEx.Synchronous;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot {

	public sealed class Program {
		internal static Bot[] bots;
		internal static bool restarting = false;
		internal static bool autorun = false;

		[DllImport("Kernel32")]
		static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);
		delegate bool HandlerRoutine(CtrlTypes ctrlType);
		static HandlerRoutine handler = new HandlerRoutine(ConsoleCtrlCheck);
		enum CtrlTypes {
			CTRL_C_EVENT = 0,
			CTRL_BREAK_EVENT,
			CTRL_CLOSE_EVENT,
			CTRL_LOGOFF_EVENT,
			CTRL_SHUTDOWN_EVENT
		}

		private static bool ConsoleCtrlCheck(CtrlTypes ctrlType) {
			// Put your own handler here

			ShutdownBots();

			return true;
		}

		internal static void ShutdownBots() {
			bool? save = null;
			if (bots != null) {
				for (int i = 0; i < bots.Length; i++) {
					if (bots[i] != null) {
						save = (save.HasValue ? save.Value : true) && bots[i].initialized;
						bots[i].Dispose();
						bots[i] = null;
					}
				}
				bots = null;
			}
			if (save.HasValue && save.Value)
				SaveData.Save();
		}

		internal static void StartupBots() {
			SaveData.Load();

			if (!autorun) {
				SaveData.singleton.Bot_tokens = UserInterface.AskForTokens(SaveData.singleton.Bot_tokens);
				SaveData.singleton.Youtube_Key = UserInterface.AskForSingleKey("Enter a youtube api key:", SaveData.singleton.Youtube_Key);
				SaveData.singleton.Wolfram_Key = UserInterface.AskForSingleKey("Enter a wolfram alpha appid:", SaveData.singleton.Wolfram_Key);
			}
			bots = new Bot[SaveData.singleton.Bot_tokens.Length];

			for (int i=0; i< SaveData.singleton.Bot_tokens.Length; i++) {
				bots[i] = new Bot(SaveData.singleton.Bot_tokens[i]);
			}
		}

		internal static void RestartBots() {
			restarting = true;
			ShutdownBots();
			Thread.Sleep(2500);
			StartupBots();
			restarting = false;
		}

		static void Main(string[] args) {
			autorun = args.Length > 0 && args.Contains("--autorun");

			try {
				ComputerHelper.Init();

				SetConsoleCtrlHandler(handler, true);

				AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
				
				LogHelper.LogCenter("Program start", ConsoleColor.Yellow);

				StartupBots();

				UserInterface.MainLoop();
				ShutdownBots();
				LogHelper.LogCenter("End of program", ConsoleColor.Yellow);
				UserInterface.PauseBeforeExit();
			} finally {
				ComputerHelper.Dispose();
			}
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
			Console.WriteLine(e.ExceptionObject.ToString());
		}
	}
}
