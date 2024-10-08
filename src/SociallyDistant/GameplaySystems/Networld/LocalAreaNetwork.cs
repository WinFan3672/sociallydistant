﻿using SociallyDistant.Core.OS.Devices;
using SociallyDistant.Core.OS.Network;

namespace SociallyDistant.GameplaySystems.Networld
{
	public class LocalAreaNetwork
	{
		private readonly Edge<LocalAreaNetwork, InternetServiceProvider> connection = new Edge<LocalAreaNetwork, InternetServiceProvider>();
		private readonly NetworkSimulationController                     simulation;
		private readonly LocalAreaNode                                   node;

		public uint PublicAddress => node.NetworkInterface.NetworkAddress;
		
		public Guid Token => connection.Token;

		public InternetServiceProvider? InternetServiceProvider => connection.Side2;

		public LocalAreaNetwork(NetworkSimulationController simulation, LocalAreaNode node)
		{
			this.simulation = simulation;
			this.node = node;
			this.connection.Side1 = this;
		}
		
		public void ConnectToInternet(InternetServiceProvider isp, uint publicAddress)
		{
			DisconnectFromInternet();

			isp.ConnectLan(this.connection, publicAddress);
		}

		public void DisconnectFromInternet()
		{
			if (connection.Side2 == null)
				return;

			connection.Side2.DisconnectLan(this);
		}

		public NetworkConnection CreateDevice(IComputer computer)
		{
			return this.node.SetUpNewDevice(computer);
		}

		public void DeleteDevice(NetworkConnection connection)
		{
			this.node.DeleteDevice(connection);
		}

		public ForwardingTableEntry GetForwardingRule(INetworkConnection connection, ushort insidePort, ushort outsidePort)
		{
			return this.node.GetForwardingRule(connection, insidePort, outsidePort);
		}

		public bool ContainsDevice(INetworkConnection connection)
		{
			return node.ContainsDevice(connection);
		}
	}
}