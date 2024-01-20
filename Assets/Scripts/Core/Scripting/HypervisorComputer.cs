﻿#nullable enable
using System;
using OS.Devices;
using OS.FileSystems;
using OS.Network;

namespace Core.Scripting
{
	public sealed class HypervisorComputer : IComputer
	{
		private readonly IUser user;
		private readonly IFileSystem hypervisorFileSystem = new HypervisorFileSystem();
		
		/// <inheritdoc />
		public string Name => "socially-distant";

		/// <inheritdoc />
		public bool FindUserById(int id, out IUser? user)
		{
			user = null;

			if (id == 0)
				user = this.user;

			return user != null;
		}

		/// <inheritdoc />
		public bool FindUserByName(string username, out IUser? user)
		{
			user = null;

			if (username == this.user.UserName)
				user = this.user;
            
			return user != null;
		}

		/// <inheritdoc />
		public IUser SuperUser => user;

		/// <inheritdoc />
		public ISystemProcess? ExecuteProgram(ISystemProcess parentProcess, ITextConsole console, string programName, string[] arguments)
		{
			return null;
		}

		/// <inheritdoc />
		public IVirtualFileSystem GetFileSystem(IUser user)
		{
			return new VirtualFileSystem(hypervisorFileSystem, user);
		}

		/// <inheritdoc />
		public INetworkConnection? Network => null;

		/// <inheritdoc />
		public ISystemProcess? CreateDaemonProcess(string name)
		{
			return null;
		}

		internal HypervisorComputer(IUser user)
		{
			this.user = user;
		}
	}
}