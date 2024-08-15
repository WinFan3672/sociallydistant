using System.Runtime.CompilerServices;
using SociallyDistant.Core.ContentManagement;
using SociallyDistant.Core.Core.Events;

namespace SociallyDistant.Core.Audio;

public abstract class SoundScheme : IGameContent
{
    private readonly SoundSchemeAttribute attribute;
    private static   SoundScheme?         current;

    protected abstract void OnPlayGuiSound(GuiSoundName soundName);

    public string Id => attribute.Id;
    public string Name => attribute.DisplayName;
    public bool UsesLicensedAssets => attribute.Licensed;
    
    protected SoundScheme()
    {
        this.attribute = GetAttribute();
    }

    private SoundSchemeAttribute GetAttribute()
    {
        var type = this.GetType();
        return type.GetCustomAttributes(false).OfType<SoundSchemeAttribute>().First();
    }

    protected void Play(string resourcePath)
    {
        EventBus.Post(new PlaySoundEvent(resourcePath));
    }
    
    public static void SetSoundScheme(SoundScheme? newScheme)
    {
        current = newScheme;
    }

    public static void PlayGuiSound(GuiSoundName soundName)
    {
        current?.OnPlayGuiSound(soundName);
    }
}