using SociallyDistant.Core.Audio;

namespace SociallyDistant.Audio.SoundSchemes;

[SoundScheme("default", "Default", Licensed = true)]
[DefaultSoundScheme]
public class DefaultSounds : SoundScheme
{
    protected override void OnPlayGuiSound(GuiSoundName soundName)
    {
    }
}