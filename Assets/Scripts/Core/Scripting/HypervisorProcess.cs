﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Architecture;
using Core.Systems;
using OS.Devices;

namespace Core.Scripting
{
	public sealed class HypervisorProcess : ISystemProcess
	{
		private readonly int id;
		private readonly IEnvironmentVariableProvider environment = new SimpleEnvironmentVariableProvider();
		private readonly UniqueIntGenerator idGenerator;
		private readonly ISystemProcess? parent;
		private readonly List<ISystemProcess> children = new List<ISystemProcess>();
        private readonly IUser user;
		private bool alive = true;

		/// <inheritdoc />
		public int Id => id;

		/// <inheritdoc />
		public string Name { get; set; } = "hypervisor-script";

		/// <inheritdoc />
		public IUser User => user;

		/// <inheritdoc />
		public ISystemProcess? Parent => parent;

		/// <inheritdoc />
		public IEnumerable<ISystemProcess> Children => children;

		/// <inheritdoc />
		public string WorkingDirectory { get; set; } = "/";

		/// <inheritdoc />
		public bool IsAlive => alive;

		/// <inheritdoc />
		public IEnvironmentVariableProvider Environment => environment;

		/// <inheritdoc />
		public event Action<ISystemProcess>? Killed;

		private HypervisorProcess(UniqueIntGenerator idGenerator, IUser user, ISystemProcess? parent)
		{
			this.user = user;
			this.idGenerator = idGenerator;
			this.parent = parent;
			this.id = idGenerator.GetNextValue();
		}
		
		public HypervisorProcess() : this(new UniqueIntGenerator(), new HypervisorUser(), null)
		{
			
		}
		
		/// <inheritdoc />
		public ISystemProcess Fork()
		{
			return ForkAsUser(user);
		}

		/// <inheritdoc />
		public ISystemProcess ForkAsUser(IUser user)
		{
			var proc = new HypervisorProcess(idGenerator, user, this);

			this.children.Add(proc);

			proc.Killed += HandleKilled;

			return proc;
			
			void HandleKilled(ISystemProcess killed)
			{
				killed.Killed -= HandleKilled;
				children.Remove(killed);
			}
		}

		/// <inheritdoc />
		public void Kill()
		{
			if (!alive)
				return;

			while (children.Count > 0)
				children[0].Kill();

			idGenerator.DeclareUnused(id);
			alive = false;
			Killed?.Invoke(this);
		}
	}
}