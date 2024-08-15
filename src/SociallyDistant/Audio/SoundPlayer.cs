using Microsoft.Build.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Serilog;
using SociallyDistant.Core.Audio;
using SociallyDistant.Core.Core.Config;
using SociallyDistant.Core.Core.Events;

namespace SociallyDistant.Audio;

internal class SoundPlayer : GameComponent
{
    private readonly SociallyDistantGame             sociallyDistant;
    private readonly Dictionary<string, SoundEffect> loadedSounds  = new();
    private readonly List<SoundEffectInstance>       playingSounds = new();
    private          IDisposable?                    settingsObserver;
    private          IDisposable?                    eventListener;
    private          float                           volume;

    public SoundPlayer(SociallyDistantGame game) : base(game)
    {
        this.sociallyDistant = game;
    }

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        settingsObserver = sociallyDistant.SettingsManager.ObserveChanges(OnSettingsChanged);
        this.eventListener = EventBus.Listen<PlaySoundEvent>(OnPlaySoundEvent);
    }

    public override void Update(GameTime gameTime)
    {
        for (var i = 0; i < playingSounds.Count; i++)
        {
            var sound = playingSounds[i];

            if (sound.State == SoundState.Playing)
                continue;

            sound.Dispose();

            playingSounds.RemoveAt(i);
            i--;
        }
    }

    private void OnSettingsChanged(ISettingsManager settings)
    {
        var audio = new AudioSettings(settings);
        this.volume = audio.SfxVolume;

        var schemeId = audio.SoundSchemeId;

        var scheme = sociallyDistant.ContentManager.GetContentOfType<SoundScheme>().FirstOrDefault(x => x.Id == schemeId);

        if (scheme == null)
        {
            Log.Warning($"Applying missing sound scheme {schemeId}. Sounds will not play.");
        }
        else
        {
            Log.Information($"Applying sound scheme {schemeId}.");
        }
        
        SoundScheme.SetSoundScheme(scheme);
    }

    private void OnPlaySoundEvent(PlaySoundEvent soundEvent)
    {
        var soundInstance = GetSoundInstance(soundEvent.ResourcePath);
        if (soundInstance == null)
            return;

        soundInstance.Volume = volume;
        soundInstance.Play();
        this.playingSounds.Add(soundInstance);
    }

    private SoundEffectInstance? GetSoundInstance(string path)
    {
        if (!loadedSounds.TryGetValue(path, out SoundEffect? soundEffect))
        {
            try
            {
                soundEffect = sociallyDistant.Content.Load<SoundEffect>(path);
                loadedSounds.Add(path, soundEffect);
            }
            catch (Exception ex)
            {
                Log.Warning($"Cannot play sound: {path} - {ex.Message}");
                return null;
            }
        }

        return soundEffect.CreateInstance();
    }
}