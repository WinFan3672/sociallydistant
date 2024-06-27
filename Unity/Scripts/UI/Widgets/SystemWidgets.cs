﻿#nullable enable

using AcidicGui.Widgets;
using UI.Applications.Chat;
using UI.Widgets.Settings;
using UnityEngine;

namespace UI.Widgets
{
	[CreateAssetMenu(menuName = "System Widgets")]
	public class SystemWidgets :
		ScriptableObject,
		IWidgetAssembler
	{
		private WidgetRecycleBin? recycleBin;

		[SerializeField]
		private SectionWidgetController sectionPrefab = null!;

		[SerializeField]
		private ButtonWidgetController buttonPrefab = null!;

		[SerializeField]
		private AvatarWidgetController avatarPrefab = null!;
		
		[SerializeField]
		private DropdownController dropdownPrefab = null!;
		
		[SerializeField]
		private InputFieldWidgetController inputFieldPrefab = null!;
		
		[SerializeField]
		private LabelWidgetController labelPrefab = null!;

		[SerializeField]
		private SwitchWidgetController switchPrefab = null!;
		
		[SerializeField]
		private ImageWidgetController imagePrefab = null!;

		[SerializeField]
		private RawImageWidgetController rawImagePrefab = null!;

		[SerializeField]
		private SliderWidgetController sliderPrefab = null!;

		
		[SerializeField]
		private ListWidgetController listPrefab = null!;

		[SerializeField]
		private ListItemWidgetController listItemPrefab = null!;

		[Header("Settings widgets")]
		[SerializeField]
		private SettingsFieldController settingsField = null!;
		
		[Header("Chat")]
		[SerializeField]
		private ChatBubbleWidgetController chatBubblePrefab = null!;

		[SerializeField]
		private GuildHeaderWidgetController guildHeaderPrefab = null!;

		[Header("Rich Embeds")]
		[SerializeField]
		private RichEmbedWidgetController richEmbedPrefab = null!;
		
		/// <inheritdoc />
		public WidgetRecycleBin RecycleBin
		{
			get
			{
				if (recycleBin == null)
					recycleBin = new WidgetRecycleBin();

				return recycleBin;
			}
		}

		/// <inheritdoc />
		public SectionWidgetController GetSectionWidget(RectTransform destination)
		{
			return RecycleOrInstantiate(this.sectionPrefab, destination);
		}
		
		public AvatarWidgetController GetAvatar(RectTransform destination)
		{
			return RecycleOrInstantiate(this.avatarPrefab, destination);
		}
		
		public InputFieldWidgetController GetInputField(RectTransform destination)
		{
			return RecycleOrInstantiate(this.inputFieldPrefab, destination);
		}
		
		/// <inheritdoc />
		public LabelWidgetController GetLabel(RectTransform destination)
		{
			return RecycleOrInstantiate(this.labelPrefab, destination);
		}
		
		public SwitchWidgetController GetSwitch(RectTransform destination)
		{
			return RecycleOrInstantiate(this.switchPrefab, destination);
		}
		
		public DropdownController GetDropdown(RectTransform destination)
		{
			return RecycleOrInstantiate(this.dropdownPrefab, destination);
		}

		/// <inheritdoc />
		public ButtonWidgetController GetButtonWidget(RectTransform destination)
		{
			return RecycleOrInstantiate(this.buttonPrefab, destination);
		}

		/// <inheritdoc />
		public ImageWidgetController GetImage(RectTransform destination)
		{
			return RecycleOrInstantiate(imagePrefab, destination);
		}

		/// <inheritdoc />
		public RawImageWidgetController GetRawImage(RectTransform destination)
		{
			return RecycleOrInstantiate(rawImagePrefab, destination);
		}

		/// <inheritdoc />
		public ListWidgetController GetList(RectTransform destination)
		{
			return RecycleOrInstantiate(listPrefab, destination);
		}

		/// <inheritdoc />
		public ListItemWidgetController GetListItem(RectTransform destination)
		{
			ListItemWidgetController controller = RecycleOrInstantiate(listItemPrefab, destination);
			
			if (controller.ImageWidget != null)
			{
				RecycleBin.Recycle(controller.ImageWidget);
				controller.ImageWidget = null;
			}
			
			return controller;
		}
		
		public SettingsFieldController GetSettingsField(RectTransform destination)
		{
			SettingsFieldController controller = RecycleOrInstantiate(settingsField, destination);

			if (controller.SlotWidget != null)
			{
				RecycleBin.Recycle(controller.SlotWidget);
				controller.SlotWidget = null;
			}
			
			return controller;
		}

		public SliderWidgetController GetSlider(RectTransform destination)
		{
			return RecycleOrInstantiate(this.sliderPrefab, destination);
		}
		
		public ChatBubbleWidgetController GetChatBubble(RectTransform destination)
		{
			return RecycleOrInstantiate(this.chatBubblePrefab, destination);
		}
		
		public GuildHeaderWidgetController GetGuildHeader(RectTransform destination)
		{
			return RecycleOrInstantiate(this.guildHeaderPrefab, destination);
		}
		
		public RichEmbedWidgetController GetRichEmbed(RectTransform destination)
		{
			return RecycleOrInstantiate(this.richEmbedPrefab, destination);
		}
		
		private T RecycleOrInstantiate<T>(T prefabToInstantiate, RectTransform rectTransform)
			where T : WidgetController
		{
			T? recycled = this.RecycleBin.TryGetRecycled<T>(rectTransform);

			if (recycled != null)
				return recycled;

			return Instantiate(prefabToInstantiate, rectTransform);
		}
	}
}