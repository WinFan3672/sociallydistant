﻿#nullable enable
using System;
using Core.Serialization;

namespace OS.Network
{
	public interface IConnection : IDisposable
	{
		bool Connected { get; }
		
		ServerInfo ServerInfo { get; }

		bool Receive(out byte[] data);

		void Send(byte[] data);
	}

	public interface IPacketMessage
	{
		void Write(IDataWriter writer);
		void Read(IDataReader reader);
	}
}