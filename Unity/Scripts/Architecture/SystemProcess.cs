﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Core.Systems;
using OS.Devices;
using System.Threading.Tasks;

namespace Architecture
{
	public class SystemProcess : ISystemProcess
	{
		private readonly UniqueIntGenerator pidGenerator;
		private readonly DeviceCoordinator coordinator;
		private bool isAlive = true;
		private int exitCode;

		/// <inheritdoc />
		public int ExitCode => exitCode;
		
		/// <inheritdoc />
		public bool IsAlive => isAlive;
		
		/// <inheritdoc />
		public int Id { get; }

		/// <inheritdoc />"/>
		public string Name { get; set; }
		
		/// <inheritdoc />
		public IUser User { get; private set; }

		/// <inheritdoc />
		public ISystemProcess? Parent { get; }

		/// <inheritdoc />
		public IEnumerable<ISystemProcess> Children => coordinator.GetChildProcesses(this);

		/// <inheritdoc />
		public string WorkingDirectory { get; set; }
		
		/// <inheritdoc />
		public IEnvironmentVariableProvider Environment { get; }
		
		public SystemProcess(UniqueIntGenerator pidGenerator, DeviceCoordinator coordinator, ISystemProcess parent, IUser user)
		{
			this.pidGenerator = pidGenerator;
			this.coordinator = coordinator;
			this.WorkingDirectory = parent.WorkingDirectory;
			this.Environment = parent.Environment.DeepClone();
			this.Id = pidGenerator.GetNextValue();
			this.Name = parent.Name;
			this.User = user;
			this.Parent = parent;

			this.coordinator.DeclareProcess(this);
		}
		
		/// <inheritdoc />
		public Task<ISystemProcess> Fork()
		{
			return Task.FromResult<ISystemProcess>(new SystemProcess(
				pidGenerator,
				coordinator,
				this,
				User
			));
		}
		
		/// <inheritdoc />
		public async Task<ISystemProcess> ForkAsUser(IUser user)
		{
			if (user == User)
				return await Fork();
			
			// Prevent users not from the same computer from
			// executing processes on it.
			if (user.Computer != User.Computer)
				throw new InvalidOperationException("An invalid attempt was made to execute a process on one computer by the user of another computer.");

			IUser previousUser = this.User;
			this.User = user;
			ISystemProcess forked = await Fork();

			this.User = previousUser;

			if (this.User != user)
			{
				this.coordinator.CopyEnvironment(user, forked);
			}
			
			return forked;
		}
		
		/// <inheritdoc />
		public event Action<ISystemProcess>? Killed; 

		/// <inheritdoc />
		public void Kill(int exitCode)
		{
			foreach (ISystemProcess child in Children.ToArray())
				child.Kill();

			this.exitCode = exitCode;
			isAlive = false;
			Killed?.Invoke(this);
		}
	}
}