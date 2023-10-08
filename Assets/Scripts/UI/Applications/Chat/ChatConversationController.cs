﻿#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityExtensions;

namespace UI.Applications.Chat
{
	public class ChatConversationController : MonoBehaviour
	{
		[SerializeField]
		private ChatMessageListView listView = null!;

		private void Awake()
		{
			this.AssertAllFieldsAreSerialized(typeof(ChatConversationController));
		}

		public void SetMessageList(IList<ChatMessageModel> messageList)
		{
			this.listView.SetItems(messageList);
			if (messageList.Count > 0)
				this.listView.ScrollTo(messageList.Count - 1, 0);
		}
	}
}