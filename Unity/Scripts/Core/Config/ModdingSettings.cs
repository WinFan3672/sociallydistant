﻿namespace Core.Config
{
	[SettingsCategory("com.sociallydistant.modding", "Mod settings", CommonSettingsCategorySections.Mods)]
	public class ModdingSettings : SettingsCategory
	{
		[SettingsField("Enable script mods", "Allow execution of script mods. By turning this on, you acknowledge that Socially Distant and its development team is not responsible for the content and behaviour of any installed mods, and that we accept no responsibility for any damage done to the game or your computer.", CommonSettingsSections.Legal)]
		public bool AcceptLegalWaiver
		{
			get => GetValue(nameof(AcceptLegalWaiver), false);
			set => SetValue(nameof(AcceptLegalWaiver), value);
		}

		public bool ModDebugMode
		{
			get => GetValue(nameof(ModDebugMode), false);
			set => SetValue(nameof(ModDebugMode), value);
		}
		
		/// <inheritdoc />
		public ModdingSettings(ISettingsManager settingsManager) : base(settingsManager)
		{ }

		/// <inheritdoc />
		public override void BuildSettingsUi(ISettingsUiBuilder uiBuilder)
		{
			uiBuilder.AddSection(CommonSettingsSections.ScriptMods, out int scriptMods)
				.WithToggle(
					"Enable script mods",
					"Allow loading and execution of script mods.",
					AcceptLegalWaiver,
					x => AcceptLegalWaiver = x,
					scriptMods
				)
				.AddSection(CommonSettingsSections.Legal, out int legal)
				.WithLabel("By enabling script mods, you accept the risk associated with the execution of un-trusted code. You agree that Socially Distant's development team is neither responsible nor liable for damage caused to your device as a result of executing script mods.", legal);

			uiBuilder.AddSection("Development", out int development)
				.WithToggle(
					"Mod debug mode",
					"Enable or disable Mod Debug Mode. Mod Debug mode sets up a safe, temporary environment for testing in-development mods with, and doesn't allow you to play regular save files. Changing this setting requires restarting the game.",
					ModDebugMode,
					x => ModDebugMode = x,
					development
				);
		}
	}
}