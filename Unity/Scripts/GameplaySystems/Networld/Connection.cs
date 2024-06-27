﻿using OS.Network;

namespace GameplaySystems.Networld
{
	public class Connection : IConnection
	{
		private Listener.ConnectionHandle handle;

		public bool Connected => handle.IsValid;

		public ServerInfo ServerInfo => handle.ServerInfo;
		
		internal Connection(Listener.ConnectionHandle handle)
		{
			this.handle = handle;
		}

		public bool Receive(out byte[] data)
		{
			return handle.TryDequeueReceivedData(out data);
		}

		public void Send(byte[] data)
		{
			handle.Send(data);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			handle.Close();
		}
	}
}