﻿#nullable enable

namespace SociallyDistant.Core.OS.Network
{
	public interface IPacketQueue : 
		IDisposable
	{
		event Action<Packet>? Received; 
		
		bool TryDequeue(out Packet packet);
		void Enqueue(Packet packet);
		Task<Packet> Dequeue(CancellationToken cancellationToken);
	}
	
	public interface INetworkConnection : INetworkInterfaceEnumerator
	{
		public uint PublicAddress { get; }
		public uint LocalAddress { get; }
		public uint LoopbackAddress { get; }
		
		Guid Identifier { get; }
		
		bool Connected { get; }
		
        // restitch-needed: This is a Unity coroutine and can't be called by mods. Switch to a Task<PingResult> instead.
		Task<PingResult> Ping(uint address, float timeout, bool acceptVoidPackets);

		// restitch-needed: This is a Unity coroutine and can't be called by mods. Switch to a Task<PingResult> instead.
		Task<ConnectionResult> Connect(uint remoteAddress, ushort remotePort);
		
		IListener Listen(ushort port, ServerType serverType = ServerType.Netcat, SecurityLevel secLevel = SecurityLevel.Open);

		/// <summary>
		///		Try to resolve a given hostname or IP address string to a numeric IP address.
		/// </summary>
		/// <param name="host">The host to resolve</param>
		/// <param name="address">The result of the address or hostname resolution. Will be 0 if resolution failed.</param>
		/// <returns>True if the host was successfully resolved. False if DNS resolution failed or the IP string failed to parse.</returns>
		bool Resolve(string host, out uint address);

		Task<PortScanResult> ScanPort(uint address, ushort port);

		bool IsListening(ushort port);

		string GetHostName(uint address);
	}
}