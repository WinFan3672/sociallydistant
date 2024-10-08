﻿#nullable enable
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Serilog;
using SociallyDistant.Core.Core;
using SociallyDistant.Core.Core.Scripting;
using SociallyDistant.Core.Core.WorldData.Data;
using SociallyDistant.Core.Modules;
using SociallyDistant.GameplaySystems.NonPlayerComputers;
using SociallyDistant.Player;

namespace SociallyDistant.GameplaySystems.Networld
{
	public class NetworkEventListener : GameComponent, INetworkSimulation
	{
		private readonly SociallyDistantGame                           game;
		private readonly Dictionary<ObjectId, InternetServiceProvider> isps               = new Dictionary<ObjectId, InternetServiceProvider>();
		private readonly Dictionary<ObjectId, LocalAreaNetwork>        lans               = new Dictionary<ObjectId, LocalAreaNetwork>();
		private readonly Dictionary<ObjectId, ForwardingTableEntry>    portForwardEntries = new Dictionary<ObjectId, ForwardingTableEntry>();
		private readonly NonPlayerComputerEventListener                npcComputers;
		private readonly NetworkUpdateHook                             updateNetworkHook = new();

		private IWorldManager World => game.WorldManager;
		private NetworkSimulationController Simulation => game.Simulation;

		internal NetworkEventListener(SociallyDistantGame game) : base(game)
		{
			this.game = game;
			this.npcComputers = new NonPlayerComputerEventListener(game);
		}

		public override void Initialize()
		{
			game.ScriptSystem.RegisterHookListener(CommonScriptHooks.AfterWorldStateUpdate, updateNetworkHook);
			
			npcComputers.Initialize();
			InstallEvents();
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			npcComputers.Update(gameTime);
		}

		protected override void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			game.ScriptSystem.UnregisterHookListener(CommonScriptHooks.AfterWorldStateUpdate, updateNetworkHook);
            
			npcComputers.Dispose();
			UninstallEvents();
		}

		private void InstallEvents()
		{
			World.Callbacks.AddCreateCallback<WorldInternetServiceProviderData>(OnCreateIsp);
			World.Callbacks.AddDeleteCallback<WorldInternetServiceProviderData>(OnDeleteIsp);
			World.Callbacks.AddModifyCallback<WorldInternetServiceProviderData>(OnModifyIsp);
			
			World.Callbacks.AddCreateCallback<WorldLocalNetworkData>(OnCreateLAN);
			World.Callbacks.AddDeleteCallback<WorldLocalNetworkData>(OnDeleteLan);
			World.Callbacks.AddModifyCallback<WorldLocalNetworkData>(OnModifyLan);
			
			World.Callbacks.AddCreateCallback<WorldNetworkConnection>(OnCreateConnection);
			World.Callbacks.AddDeleteCallback<WorldNetworkConnection>(OnDeleteConnection);
			World.Callbacks.AddModifyCallback<WorldNetworkConnection>(OnModifyConnection);
			
			World.Callbacks.AddModifyCallback<WorldPlayerData>(OnPlayerDataModified);

			World.Callbacks.AddCreateCallback<WorldPortForwardingRule>(OnCreatePortForwardingRule);
			World.Callbacks.AddModifyCallback<WorldPortForwardingRule>(OnModifyPortForwardingRule);
			World.Callbacks.AddDeleteCallback<WorldPortForwardingRule>(OnDeletePortForwardingRule);
		}

		
		private void OnPlayerDataModified(WorldPlayerData subjectprevious, WorldPlayerData subjectnew)
		{
			if (isps.TryGetValue(subjectnew.PlayerInternetProvider, out InternetServiceProvider isp))
			{
				game.Player.ConnectToInternet(isp, subjectnew.PublicNetworkAddress);
			}
			else
			{
				game.Player.DisconnectFromInternet();
			}
		}

		private void UninstallEvents()
		{
			World.Callbacks.RemoveCreateCallback<WorldInternetServiceProviderData>(OnCreateIsp);
			World.Callbacks.RemoveDeleteCallback<WorldInternetServiceProviderData>(OnDeleteIsp);
			World.Callbacks.RemoveModifyCallback<WorldInternetServiceProviderData>(OnModifyIsp);
			
			World.Callbacks.RemoveCreateCallback<WorldLocalNetworkData>(OnCreateLAN);
			World.Callbacks.RemoveDeleteCallback<WorldLocalNetworkData>(OnDeleteLan);
			World.Callbacks.RemoveModifyCallback<WorldLocalNetworkData>(OnModifyLan);
			
			World.Callbacks.RemoveCreateCallback<WorldNetworkConnection>(OnCreateConnection);
			World.Callbacks.RemoveDeleteCallback<WorldNetworkConnection>(OnDeleteConnection);
			World.Callbacks.RemoveModifyCallback<WorldNetworkConnection>(OnModifyConnection);
			
			World.Callbacks.RemoveModifyCallback<WorldPlayerData>(OnPlayerDataModified);
			
			World.Callbacks.RemoveCreateCallback<WorldPortForwardingRule>(OnCreatePortForwardingRule);
			World.Callbacks.RemoveModifyCallback<WorldPortForwardingRule>(OnModifyPortForwardingRule);
			World.Callbacks.RemoveDeleteCallback<WorldPortForwardingRule>(OnDeletePortForwardingRule);
		}

		private void OnDeletePortForwardingRule(WorldPortForwardingRule subject)
		{
			if (!portForwardEntries.TryGetValue(subject.InstanceId, out ForwardingTableEntry entry))
				return;

			entry.Delete();
			portForwardEntries.Remove(subject.InstanceId);
		}

		private void OnModifyPortForwardingRule(WorldPortForwardingRule subjectprevious, WorldPortForwardingRule subjectnew)
		{
			// get the LAN
			if (!lans.TryGetValue(subjectnew.LanId, out LocalAreaNetwork lan))
				return;
			
			// and the computer
			if (!npcComputers.TryGetComputer(subjectnew.ComputerId, out NonPlayerComputer computer))
				return;
			
			// Computer must have a network
			if (computer.Network == null)
				return;
			
			// Computer must be connected to this LAN
			if (!lan.ContainsDevice(computer.Network))
				return;

			if (!portForwardEntries.TryGetValue(subjectnew.InstanceId, out ForwardingTableEntry entry))
				return;

			entry.Delete();
			portForwardEntries.Remove(subjectnew.InstanceId);

			OnCreatePortForwardingRule(subjectnew);
		}

		private void OnCreatePortForwardingRule(WorldPortForwardingRule subject)
		{
			// get the LAN
			if (!lans.TryGetValue(subject.LanId, out LocalAreaNetwork lan))
				return;
			
			// and the computer
			if (!npcComputers.TryGetComputer(subject.ComputerId, out NonPlayerComputer computer))
				return;
			
			// Computer must have a network
			if (computer.Network == null)
				return;
			
			// Computer must be connected to this LAN
			if (!lan.ContainsDevice(computer.Network))
				return;
			
			if (!portForwardEntries.TryGetValue(subject.InstanceId, out ForwardingTableEntry entry))
			{
				entry = lan.GetForwardingRule(computer.Network, subject.LocalPort, subject.GlobalPort);
				this.portForwardEntries.Add(subject.InstanceId, entry);
			}
		}

		
		private void OnModifyConnection(WorldNetworkConnection subjectprevious, WorldNetworkConnection subjectnew)
		{
			// If nothing's changed then ignore the Modify event. Otherwise we'll needlessly wreak havoc in the simulation.
			if (subjectprevious.ComputerId == subjectnew.ComputerId && subjectprevious.LanId == subjectnew.LanId)
			{
				Log.Warning("Something caused a WorldNetworkConnection ModifyEvent but didn't actually change anything. Can we not? Please? Y'all are lucky Ritchie programs defensively.");
				return;
			}

			HandleConnect(subjectnew);
		}

		private void OnDeleteConnection(WorldNetworkConnection subject)
		{
			HandleDisconnect(subject);
		}

		private void OnCreateConnection(WorldNetworkConnection subject)
		{
			HandleConnect(subject);
		}

		private void HandleDisconnect(WorldNetworkConnection data)
		{
			if (npcComputers.TryGetComputer(data.ComputerId, out NonPlayerComputer npc))
				npc.DisconnectLan();
		}
		
		private void HandleConnect(WorldNetworkConnection data)
		{
			// Find a computer
			if (!npcComputers.TryGetComputer(data.ComputerId, out NonPlayerComputer computer))
				return;
			
			// Handle LAN changes
			if (!lans.TryGetValue(data.LanId, out LocalAreaNetwork lan))
				computer.DisconnectLan();
			else
				computer.ConnectLan(lan);
		}
		
		private void OnModifyLan(WorldLocalNetworkData subjectprevious, WorldLocalNetworkData subjectnew)
		{
			if (!lans.TryGetValue(subjectnew.InstanceId, out LocalAreaNetwork net))
				return;

			UpdateLAN(net, subjectnew);
		}

		private void OnDeleteLan(WorldLocalNetworkData subject)
		{
			if (!lans.TryGetValue(subject.InstanceId, out LocalAreaNetwork lan))
				return;

			if (lan.InternetServiceProvider != null)
				lan.DisconnectFromInternet();

			lans.Remove(subject.InstanceId);
			//simulation.DeleteLan(lan);
		}

		private void OnCreateLAN(WorldLocalNetworkData subject)
		{
			if (!lans.TryGetValue(subject.InstanceId, out LocalAreaNetwork lan))
			{
				lan = Simulation.CreateLocalAreaNetwork();
				lans.Add(subject.InstanceId, lan);
			}

			UpdateLAN(lan, subject);
		}

		private void OnModifyIsp(WorldInternetServiceProviderData subjectprevious, WorldInternetServiceProviderData subjectnew)
		{
			
		}

		private void OnDeleteIsp(WorldInternetServiceProviderData subject)
		{
			
		}

		private void OnCreateIsp(WorldInternetServiceProviderData subject)
		{
			if (!isps.TryGetValue(subject.InstanceId, out InternetServiceProvider isp))
			{
				isp = Simulation.CreateInternetServiceProvider(subject.CidrNetwork);
				isps.Add(subject.InstanceId, isp);
			}

			UpdateInstance(isp, subject);
		}

		private void UpdateInstance(InternetServiceProvider isp, WorldInternetServiceProviderData data)
		{
			// During game load, this ensures we connect the player to the right ISP when the ISP is loaded.
			WorldPlayerData player = this.World.World.PlayerData.Value;
			if (player.PlayerInternetProvider == data.InstanceId)
				this.OnPlayerDataModified(player, player);
		}

		private void UpdateLAN(LocalAreaNetwork lan, WorldLocalNetworkData data)
		{
			if (!isps.TryGetValue(data.ServiceProviderId, out InternetServiceProvider isp))
			{
				lan.DisconnectFromInternet();
			}
			else
			{
				if (!isp.IsConnectedWithLAN(lan))
				{
					lan.DisconnectFromInternet();
					lan.ConnectToInternet(isp, data.PublicNetworkAddress);
				}
			}
			
		}

		public uint? GetNarrativeAddress(string narrativeId)
		{
			if (narrativeId == "player")
				return this.game.Player.Computer.Network?.PublicAddress;

			if (!World.World.LocalAreaNetworks.ContainsNarrativeId(narrativeId))
				return null;

			var narrativeObject = World.World.LocalAreaNetworks.GetNarrativeObject(narrativeId);
			return lans[narrativeObject.InstanceId].PublicAddress;
		}
	}
}