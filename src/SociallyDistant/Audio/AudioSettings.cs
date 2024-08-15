using SociallyDistant.Core.Audio;
using SociallyDistant.Core.Core.Config;
using SociallyDistant.Core.Modules;

namespace SociallyDistant.Audio;

[SettingsCategory("audio", "Audio", CommonSettingsCategorySections.Hardware)]
public class AudioSettings : SettingsCategory
{
    public string SoundSchemeId
    {
        get => GetValue(nameof(SoundSchemeId), "default");
        set => SetValue(nameof(SoundSchemeId), value);
    }
    
    public float MusicVolume
    {
        get => GetValue(nameof(MusicVolume), 0.75f);
        set => SetValue(nameof(MusicVolume), value);
    }
    
    public float SfxVolume
    {
        get => GetValue(nameof(SfxVolume), 0.75f);
        set => SetValue(nameof(SfxVolume), value);
    }
    
    
    
    public AudioSettings(ISettingsManager settingsManager) : base(settingsManager)
    {
    }

    public override void BuildSettingsUi(ISettingsUiBuilder uiBuilder)
    {
        var soundSchemes = Application.Instance.Context.ContentManager.GetContentOfType<SoundScheme>().ToArray();
        var schemeIds = soundSchemes.Select(x => x.Id).ToArray();
        var schemeNames = soundSchemes.Select(x => x.UsesLicensedAssets
            ? $"{x.Name}*"
            : x.Name).ToArray();
        
        uiBuilder.AddSection("Volume", out int volumeSection);

        uiBuilder.WithSlider("Music",         null, MusicVolume, 0, 1, x => MusicVolume = x, volumeSection);
        uiBuilder.WithSlider("Sound effects", null, SfxVolume,   0, 1, x => SfxVolume = x,   volumeSection);

        if (soundSchemes.Length > 1)
        {
            var currentSchemeIndex = Array.IndexOf(schemeIds, SoundSchemeId);
            uiBuilder.AddSection("Personalization", out int personalization);
            uiBuilder.WithStringDropdown("Sound theme", "Choose what sounds are used by the game. Sound themes marked with an asterisk use licensed sounds and may not be redistributed.", currentSchemeIndex, schemeNames, x => SoundSchemeId = schemeIds[x], personalization);
        }
        
    }
}