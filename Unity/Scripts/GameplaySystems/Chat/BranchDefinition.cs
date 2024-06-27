﻿#nullable enable
using Chat;
using Social;
using UnityEngine;

namespace GameplaySystems.Chat
{
	public sealed class BranchDefinition : IBranchDefinition
	{
		private readonly ConversationScriptController controller;
		
		public IProfile Target { get; }
		public string Identifier { get; }
		public string Message { get; }

		/// <inheritdoc />
		public void Pick()
		{
			controller.ChooseBranch(Identifier);
		}

		public BranchDefinition(ConversationScriptController controller,  IProfile target, string id, string message)
		{
			this.controller = controller;
			this.Target = target;
			this.Identifier = id;
			this.Message = message;
		}
	}
}