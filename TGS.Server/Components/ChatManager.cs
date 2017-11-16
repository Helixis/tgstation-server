﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TGS.Server.ChatCommands;
using TGS.Server.ChatProviders;
using TGS.Interface;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	sealed class ChatManager : IChatManager
	{
		/// <summary>
		/// Used for indicating unintialized encrypted data
		/// </summary>
		const string UninitializedString = "NEEDS INITIALIZING";
		
		// Topic command return parameters
		const string TCPHelpText = "help_text";
		const string TCPAdminOnly = "admin_only";
		const string TCPRequiredParameters = "required_parameters";

		/// <summary>
		/// List of <see cref="IChatProvider"/>s for the <see cref="Instance"/>
		/// </summary>
		IList<IChatProvider> ChatProviders;

		IReadOnlyList<Command> serverChatCommands;

		/// <inheritdoc />
		public void LoadServerChatCommands(string json)
		{
			if (String.IsNullOrWhiteSpace(json))
				return;
			List<Command> tmp = new List<Command>();
			try
			{
				foreach (var I in JsonConvert.DeserializeObject<IDictionary<string, IDictionary<string, object>>>(json))
					tmp.Add(new ServerChatCommand(I.Key, (string)I.Value[CCPHelpText], ((int)I.Value[CCPAdminOnly]) == 1, (int)I.Value[CCPRequiredParameters]));
				serverChatCommands = tmp;
			}
			catch { }
		}

		/// <summary>
		/// Set up the <see cref="ChatProviders"/> for the <see cref="Instance"/>
		/// </summary>
		public void InitChat()
		{
			var infos = InitProviderInfos();
			ChatProviders = new List<IChatProvider>(infos.Count);
			foreach (var info in infos)
			{
				IChatProvider chatProvider;
				try
				{
					switch (info.Provider)
					{
						case ChatProvider.Discord:
							chatProvider = new DiscordChatProvider(info);
							break;
						case ChatProvider.IRC:
							chatProvider = new IRCChatProvider(info);
							break;
						default:
							WriteError(String.Format("Invalid chat provider: {0}", info.Provider), EventID.InvalidChatProvider);
							continue;
					}
				}
				catch (Exception e)
				{
					WriteError(String.Format("Failed to start chat provider {0}! Error: {1}", info.Provider, e.ToString()), EventID.ChatProviderStartFail);
					continue;
				}
				chatProvider.OnChatMessage += ChatProvider_OnChatMessage;
				var res = chatProvider.Connect();
				if (res != null)
					WriteWarning(String.Format("Unable to connect to chat! Provider {0}, Error: {1}", chatProvider.GetType().ToString(), res), EventID.ChatConnectFail);
				ChatProviders.Add(chatProvider);
			}
		}

		/// <summary>
		/// Implementation of <see cref="OnChatMessage"/> that recieves messages from all channels of all connected <see cref="ChatProviders"/>
		/// </summary>
		/// <param name="ChatProvider">The <see cref="IChatProvider"/> that heard the <paramref name="message"/></param>
		/// <param name="speaker">The user who wrote the <paramref name="message"/></param>
		/// <param name="channel">The channel the <paramref name="message"/> is from</param>
		/// <param name="message">The recieved message</param>
		/// <param name="isAdmin"><see langword="true"/> if <paramref name="speaker"/> is considered a chat admin, <see langword="false"/> otherwise</param>
		/// <param name="isAdminChannel"><see langword="true"/> if <paramref name="channel"/> is an admin channel, <see langword="false"/> otherwise</param>
		private void ChatProvider_OnChatMessage(IChatProvider ChatProvider, string speaker, string channel, string message, bool isAdmin, bool isAdminChannel)
		{
			var splits = message.Trim().Split(' ');

			if (splits.Length == 1 && splits[0] == "")
			{
				ChatProvider.SendMessageDirect("Hi!", channel);
				return;
			}

			var asList = new List<string>(splits);

			Command.OutputProcVar.Value = (m) => ChatProvider.SendMessageDirect(m, channel);
			ChatCommand.CommandInfo.Value = new CommandInfo()
			{
				IsAdmin = isAdmin,
				IsAdminChannel = isAdminChannel,
				Speaker = speaker,
				Server = this,
			};
			WriteInfo(String.Format("Chat Command from {0} ({2}): {1}", speaker, String.Join(" ", asList), channel), EventID.ChatCommand);
			if (ServerChatCommands == null)
				LoadServerChatCommands();
			new RootChatCommand(ServerChatCommands).DoRun(asList);
		}

		/// <summary>
		/// Properly shuts down all <see cref="ChatProviders"/>
		/// </summary>
		void DisposeChat()
		{
			var infosList = new List<IList<string>>();

			foreach (var ChatProvider in ChatProviders)
			{
				infosList.Add(ChatProvider.ProviderInfo().DataFields);
				ChatProvider.Dispose();
			}
			ChatProviders = null;

			var rawdata = JsonConvert.SerializeObject(infosList);

			Config.ChatProviderData = Interface.Helpers.EncryptData(rawdata, out string entrp);
			Config.ChatProviderEntropy = entrp;
		}

		/// <inheritdoc />
		public IList<ChatSetupInfo> ProviderInfos()
		{
			var infosList = new List<ChatSetupInfo>();
			foreach (var chatProvider in ChatProviders)
				infosList.Add(chatProvider.ProviderInfo());
			return infosList;
		}

		/// <summary>
		/// Returns a list of <see cref="ChatSetupInfo"/>s loaded from the config or the defaults if none are set
		/// </summary>
		/// <returns>A list of <see cref="ChatSetupInfo"/>s loaded from the config or the defaults if none are set</returns>
		IList<ChatSetupInfo> InitProviderInfos()
		{
			lock (ChatLock)
			{
				var rawdata = Config.ChatProviderData;
				if (rawdata == UninitializedString)
					return new List<ChatSetupInfo>() { new IRCSetupInfo() { Nickname = Config.Name }, new DiscordSetupInfo() };

				string plaintext;
				try
				{
					plaintext = Interface.Helpers.DecryptData(rawdata, Config.ChatProviderEntropy);
					
					var lists = JsonConvert.DeserializeObject<List<List<string>>>(plaintext);
					var output = new List<ChatSetupInfo>(lists.Count);
					var foundirc = 0;
					var founddiscord = 0;
					foreach (var l in lists)
					{
						var info = new ChatSetupInfo(l);
						if (info.Provider == ChatProvider.Discord)
							++founddiscord;
						else if (info.Provider == ChatProvider.IRC)
							++foundirc;
						output.Add(info);
					}

					if (foundirc != 1 || founddiscord != 1)
						throw new Exception();
					
					return output;
				}
				catch
				{
					Config.ChatProviderData = UninitializedString;
				}
			}
			//if we get here we want to retry
			return InitProviderInfos();
		}

		/// <inheritdoc />
		public string SetProviderInfo(ChatSetupInfo info)
		{
			info.AdminList.RemoveAll(x => String.IsNullOrWhiteSpace(x));
			info.AdminChannels.RemoveAll(x => String.IsNullOrWhiteSpace(x));
			info.GameChannels.RemoveAll(x => String.IsNullOrWhiteSpace(x));
			info.DevChannels.RemoveAll(x => String.IsNullOrWhiteSpace(x));
			info.WatchdogChannels.RemoveAll(x => String.IsNullOrWhiteSpace(x));
			try
			{
				lock (ChatLock)
				{
					foreach (var ChatProvider in ChatProviders)
						if (info.Provider == ChatProvider.ProviderInfo().Provider)
							return ChatProvider.SetProviderInfo(info);
					return "Error: Invalid provider: " + info.Provider.ToString();
				}
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}

		/// <inheritdoc />
		public bool Connected(ChatProvider providerType)
		{
			foreach (var I in ChatProviders)
				if (I.ProviderInfo().Provider == providerType)
					return I.Connected();
			return false;
		}

		/// <summary>
		/// Reconnect servers that are enabled and disconnected. Checked every time DreamDaemon reboots
		/// </summary>
		void ChatConnectivityCheck()
		{
			foreach (ChatProvider I in Enum.GetValues(typeof(ChatProvider)))
				if(!Connected(I))
					Reconnect(I);
		}

		/// <inheritdoc />
		public string Reconnect(ChatProvider providerType)
		{
			foreach (var I in ChatProviders)
				if (I.ProviderInfo().Provider == providerType)
					return I.Reconnect();
			return "Could not find specified provider!";
		}

		/// <summary>
		/// Broadcast a message to appropriate channels based on the message type
		/// </summary>
		/// <param name="msg">The message to send</param>
		/// <param name="mt">The message type</param>
		public Task SendMessage(string msg, MessageType mt)
		{
			return Task.Factory.StartNew(() =>
			{
				lock (ChatLock)
				{
					var tasks = new Dictionary<ChatProvider, Task>();
					foreach (var ChatProvider in ChatProviders)
						tasks.Add(ChatProvider.ProviderInfo().Provider, ChatProvider.SendMessage(msg, mt));
					foreach (var T in tasks)
						try
						{
							T.Value.Wait();
						}
						catch (Exception e)
						{
							WriteWarning(String.Format("Chat broadcast failed (Provider: {3}) (Flags: {0}) (Message: {1}): {2}", mt, msg, e.ToString(), T.Key), EventID.ChatBroadcastFail);
						}
				}
			});
		}
	}
}
