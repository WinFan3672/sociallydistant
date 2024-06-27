﻿#nullable enable
using System;
using Chat;
using Core;
using GamePlatform;
using GameplaySystems.Social;
using UnityEngine;
using UnityExtensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Scripting;
using Core.WorldData;
using GamePlatform.ContentManagement;
using Modules;
using Social;
using UniRx;

namespace GameplaySystems.Chat
{
	public sealed class ConversationManager : MonoBehaviour
	{
		private static readonly Singleton<ConversationManager> singleton = new();
		
		private readonly List<IChatConversation> allConversations = new();
		private readonly Dictionary<string, int> conversationsById = new();
		private readonly Dictionary<string, List<int>> conversationsByMember = new();
		private readonly Dictionary<ChatScriptType, List<int>> conversationsByType = new();
		private readonly Dictionary<string, ActiveConversation> activeConversations = new();
		private readonly Dictionary<string, ConversationScriptController> activeControllers = new();
		private readonly List<string> completedConversations = new();
		private readonly Dictionary<IChatChannel, List<Subject<BranchDefinitionList>>> branchSubjects = new();
		private readonly Subject<ChatBoxRequest> chatBoxRequestSubject = new();
		private readonly Dictionary<ObjectId, List<IBranchDefinition>> initialInteractionsByChannel = new();

		private WorldManager worldManagerHolder = null!;
		private ISocialService socialService = null!;
		private GameManager gameManagerHolder = null!;
		private ChatBoxRequest? pendingChatBoxRequest;
		private Task? saveTask = null!;
		private IHookListener updateDatabaseHook = null!;

		public IEnumerable<string> AllConversationIds => conversationsById.Keys;
		public IEnumerable<string> ActiveConversations => activeConversations.Keys;
		
		private void Awake()
		{
			singleton.SetInstance(this);
			
			gameManagerHolder = GameManager.Instance;
			socialService = gameManagerHolder.SocialService;
			worldManagerHolder = WorldManager.Instance;
			
			this.AssertAllFieldsAreSerialized(typeof(ConversationManager));
			updateDatabaseHook = new UpdateChatDatabaseHook(this);
		}

		private void Start()
		{
			gameManagerHolder.ScriptSystem.RegisterHookListener(CommonScriptHooks.AfterWorldStateUpdate, updateDatabaseHook);
		}

		private void OnDestroy()
		{
			gameManagerHolder.ScriptSystem.UnregisterHookListener(CommonScriptHooks.AfterWorldStateUpdate, updateDatabaseHook);
			singleton.SetInstance(null);
		}

		private void Update()
		{
			if (saveTask != null)
			{
				if (saveTask.IsCompleted)
					saveTask = null;

				return;
			}
			
			var mustSave = false;
			if (completedConversations.Count > 0)
			{
				ProtectedWorldState protectedState = worldManagerHolder.World.ProtectedWorldData.Value;

				var interactionList = new List<string>();
				protectedState.CompletedInteractions ??= new List<string>();
				
				interactionList.AddRange(protectedState.CompletedInteractions);
				
				while (completedConversations.Count > 0)
				{
					string id = completedConversations[0];
					if (!interactionList.Contains(id))
						interactionList.Add(id);
					
					completedConversations.RemoveAt(0);
					activeConversations.Remove(id);
					activeControllers.Remove(id);

					mustSave = activeConversations.Count == 0;
				}

				protectedState.CompletedInteractions = interactionList;
				
				worldManagerHolder.World.ProtectedWorldData.Value = protectedState;
			}

			if (mustSave)
			{
				saveTask = gameManagerHolder.SaveCurrentGame(false);
				return;
			}

			foreach (string key in activeConversations.Keys)
			{
				ActiveConversation conversation = activeConversations[key];
				if (conversation.Task.IsCompleted)
				{
					if (conversation.Task.Exception != null)
						Debug.LogException(conversation.Task.Exception);

					completedConversations.Add(key);
				}
			}
		}

		public IDisposable ObservePendingChatBoxRequests(Action<ChatBoxRequest> callback)
		{
			if (pendingChatBoxRequest != null)
				callback?.Invoke(pendingChatBoxRequest);
			
			return chatBoxRequestSubject.Subscribe(callback);
		}
		
		public Task<ChatBoxController> RequestChatBoxAccess(ObjectId channelId)
		{
			if (this.pendingChatBoxRequest != null)
				throw new InvalidOperationException("Another script is waiting for control of the chatbox!");
			
			var completionSource = new TaskCompletionSource<ChatBoxController>();
			var request = new ChatBoxRequest(channelId, completionSource, () =>
			{
				pendingChatBoxRequest = null;
			});

			this.pendingChatBoxRequest = request;
			this.chatBoxRequestSubject.OnNext(this.pendingChatBoxRequest);
			
			return completionSource.Task;
		}
		
		public void ChooseBranch(string conversation, string branchId)
		{
			if (!activeControllers.TryGetValue(conversation, out ConversationScriptController? controller))
				return;

			controller.ChooseBranch(branchId);
		}
		
		public bool IsConversationActive(string id)
		{
			return activeConversations.ContainsKey(id);
		}
		
		public bool TryStartConversation(string conversationId)
		{
			if (!conversationsById.TryGetValue(conversationId, out int conversationIndex))
				return false;
			
			if (activeConversations.ContainsKey(conversationId))
				return false;

			IChatConversation conversation = allConversations[conversationIndex];

			INarrativeThread thread = socialService.GetNarrativeThread(conversation.Type switch
			{
				ChatScriptType.Dm => NarrativeThread.DirectMessage,
				ChatScriptType.Group => NarrativeThread.DirectMessageGroup,
				ChatScriptType.Guild => NarrativeThread.Channel
			}, conversation.GuildId, conversation.ChannelId, true, conversation.ActorIds.ToArray());
			
			var tokenSource = new CancellationTokenSource();
			var controller = new ConversationScriptController(this, worldManagerHolder, socialService, conversation, thread);
			Task task = conversation.StartConversation(tokenSource.Token, controller);

			this.activeConversations[conversationId] = new ActiveConversation(tokenSource, task);
			this.activeControllers[conversationId] = controller;
			return true;
		}

		public void RefreshBranches(ObjectId channelId)
		{
			IChatChannel? lookedUpChannel = branchSubjects.Keys.FirstOrDefault(x => x.Id == channelId);
			if (lookedUpChannel == null)
				return;

			BranchDefinitionList branchList = GatherAvailableBranchesInternal(lookedUpChannel);
			
			foreach (Subject<BranchDefinitionList> subject in branchSubjects[lookedUpChannel])
				subject.OnNext(branchList);
		}
		
		private async Task UpdateConversationDatabase()
		{
			this.conversationsById.Clear();
			this.conversationsByMember.Clear();
			this.conversationsByType.Clear();
			this.allConversations.Clear();
			
			// find all conversations via ContentManager
			var i = 0;
			foreach (IChatConversation conversation in gameManagerHolder.ContentManager.GetContentOfType<IChatConversation>())
			{
				await Task.Yield();
				allConversations.Add(conversation);
				conversationsById[conversation.Id] = i;
				
				if (!conversationsByType.ContainsKey(conversation.Type))
					conversationsByType[conversation.Type] = new List<int>();
				
				conversationsByType[conversation.Type].Add(i);
				
				foreach (string actor in conversation.ActorIds)
				{
					if (!conversationsByMember.ContainsKey(actor))
						conversationsByMember[actor] = new List<int>();

					conversationsByMember[actor].Add(i);
				}
				
				i++;
			}
			
			UpdateInitialInteractions();
		}

		private void UpdateInitialInteractions()
		{
			initialInteractionsByChannel.Clear();
			
			foreach (IChatConversation conversation in allConversations)
			{
				if (conversation.StartType != ChatStartType.Auto)
					continue;

				if (!conversation.CheckConditions(worldManagerHolder, socialService))
					continue;
				
				if (string.IsNullOrWhiteSpace(conversation.StartMessage))
					continue;

				if (!conversation.IsRepeatable && worldManagerHolder.World.IsInteractionCompleted(conversation.Id))
					continue;
				
				INarrativeThread? thread = socialService.GetNarrativeThread(conversation.Type switch
				{
					ChatScriptType.Dm => NarrativeThread.DirectMessage,
					ChatScriptType.Group => NarrativeThread.DirectMessageGroup,
					ChatScriptType.Guild => NarrativeThread.Channel
				}, conversation.GuildId, conversation.ChannelId, false, conversation.ActorIds.ToArray());

				if (thread == null)
					continue;
				
				IProfile target = socialService.GetNarrativeProfile(conversation.ActorIds.First());

				var interaction = new InitialInteraction(this, target, conversation.Id, conversation.StartMessage);

				if (!initialInteractionsByChannel.ContainsKey(thread.ChannelId))
					initialInteractionsByChannel[thread.ChannelId] = new List<IBranchDefinition>();

				initialInteractionsByChannel[thread.ChannelId].Add(interaction);
			}
			
			foreach (ObjectId channelId in this.initialInteractionsByChannel.Keys)
				RefreshBranches(channelId);
		}

		public IDisposable ObserveBranchDefinitions(IChatChannel channel, Action<BranchDefinitionList> callback)
		{
			if (!branchSubjects.TryGetValue(channel, out List<Subject<BranchDefinitionList>>? subjectList))
			{
				subjectList = new List<Subject<BranchDefinitionList>>();
				branchSubjects.Add(channel, subjectList);
			}

			var subject = new Subject<BranchDefinitionList>();

			IDisposable observer = subject.Subscribe(callback);

			subjectList.Add(subject);
			
			subject.OnNext(GatherAvailableBranchesInternal(channel));

			return observer;
		}

		private BranchDefinitionList GatherAvailableBranchesInternal(IChatChannel channel)
		{
			var list = new BranchDefinitionList();
			
			// Find any active conversations in this channel and eat up the branches in them.
			foreach (ConversationScriptController conversation in activeControllers.Values)
			{
				foreach (IBranchDefinition branch in conversation.GetBranches())
				{
					list.Add(branch);
				}
			}

			if (list.Count == 0)
			{
				if (initialInteractionsByChannel.TryGetValue(channel.Id, out List<IBranchDefinition>? interactionList))
				{
					foreach (IBranchDefinition interaction in interactionList)
						list.Add(interaction);
				}
			}
			
			return list;
		}
		
		private sealed class UpdateChatDatabaseHook : IHookListener
		{
			private readonly ConversationManager conversationManager;

			public UpdateChatDatabaseHook(ConversationManager conversationManager)
			{
				this.conversationManager = conversationManager;
			}

			/// <inheritdoc />
			public async Task ReceiveHookAsync(IGameContext game)
			{
				await conversationManager.UpdateConversationDatabase();
			}
		}

		private sealed class ActiveConversation
		{
			private readonly CancellationTokenSource tokenSource;
			private readonly Task task;

			public CancellationTokenSource TokenSource => tokenSource;
			public Task Task => task;
			
			public ActiveConversation(CancellationTokenSource tokenSource, Task task)
			{
				this.tokenSource = tokenSource;
				this.task = task;
			}
		}

		public static ConversationManager? Instance => singleton.Instance;
	}
}