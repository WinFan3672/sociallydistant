using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Serilog;
using SociallyDistant.Core.Core.Config;
using SociallyDistant.Core.Core.Events;

namespace SociallyDistant.Audio;

internal class MusicManager : GameComponent
{
    private readonly SociallyDistantGame             sociallyDistant;
    private readonly Dictionary<string, SoundEffect> loadedSoundEffects = new();
    private readonly Stack<SoundEffectInstance>      previousSong       = new();
    private readonly double                          fadeDuration       = 0.4f;
    private          float                           musicVolume;
    private          double                          fadeProgress;
    private          float                           fadeOUtVolume;
    private          float                           fadeInVolume;
    private          STate                           state;
    private          IDisposable?                    settingsObserver;
    private          IDisposable?                    musicEventListener;
    private          SoundEffectInstance?            currentSong;
    private          SoundEffectInstance?            nextSong;
    
    public MusicManager(SociallyDistantGame game) : base(game)
    {
        this.sociallyDistant = game;
    }

    public override void Initialize()
    {
        settingsObserver = sociallyDistant.SettingsManager.ObserveChanges(OnSettingsChanged);
        musicEventListener = EventBus.Listen<PlaySongEvent>(OnMusicRequested);
        
        base.Initialize();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        if (!disposing)
            return;

        musicEventListener?.Dispose();
        settingsObserver?.Dispose();
        settingsObserver = null;
        musicEventListener = null;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        switch (state)
        {
            case STate.Playing:
            {
                if (currentSong == null)
                    break;

                if (currentSong.State != SoundState.Playing)
                {
                    if (previousSong.Count == 0)
                        nextSong = null;
                    else
                        nextSong = previousSong.Pop();

                    state = STate.NewSongRequested;
                }
                break;
            }
            case STate.NewSongRequested:
            {
                nextSong?.Play();
                fadeProgress = 0;
                fadeOUtVolume = currentSong?.Volume ?? 0;
                fadeInVolume = nextSong?.Volume ?? 0;
                state = STate.FadingToNext;
                break;
            }
            case STate.FadingToNext:
            {
                var fadePercentage = (float) Math.Clamp(fadeProgress / fadeDuration, 0, 1);
                var outVolume = MathHelper.Lerp(fadeOUtVolume, 0,            fadePercentage);
                var inVolume = MathHelper.Lerp(0,              fadeInVolume, fadePercentage);

                if (currentSong != null)
                    currentSong.Volume = outVolume;

                if (nextSong != null)
                    nextSong.Volume = inVolume;

                if (fadeProgress >= fadeDuration)
                {
                    currentSong = nextSong;
                    nextSong = null;
                    state = STate.Playing;
                }
                
                fadeProgress += gameTime.ElapsedGameTime.TotalSeconds;
                break;
            }
        }
    }

    private SoundEffectInstance? GetSoundInstance(string path)
    {
        if (!loadedSoundEffects.TryGetValue(path, out SoundEffect? soundEffect))
        {
            try
            {
                loadedSoundEffects.Add(path, sociallyDistant.Content.Load<SoundEffect>(path));
            }
            catch (Exception ex)
            {
                Log.Warning($"Cannot load sound resource into MusicManager: {path} - {ex.Message}");
                return null;
            }
        }

        return loadedSoundEffects[path].CreateInstance();
    }
    
    private void OnMusicRequested(PlaySongEvent songEvent)
    {
        var newInstance = GetSoundInstance(songEvent.ResourcePath);

        if (newInstance == null && !songEvent.IsLooped)
            return;

        if (newInstance != null)
        {
            newInstance.Volume = musicVolume;
            newInstance.IsLooped = songEvent.IsLooped;
        }

        if (songEvent.IsLooped)
        {
            nextSong = newInstance;
            state = STate.NewSongRequested;
        }
        else
        {
            if (currentSong != null)
                previousSong.Push(currentSong);

            nextSong = newInstance;
            state = STate.NewSongRequested;
        }
    }
    
    private void OnSettingsChanged(ISettingsManager settings)
    {
        var audio = new AudioSettings(settings);

        this.musicVolume = audio.MusicVolume;
        
        if (state == STate.Playing)
        {
            if (currentSong != null)
                currentSong.Volume = audio.MusicVolume;
        }
        else if (state == STate.FadingToNext)
        {
            fadeOUtVolume = audio.MusicVolume;
            fadeInVolume = audio.MusicVolume;
            
            var fadePercentage = (float) Math.Clamp(fadeProgress / fadeDuration, 0, 1);
            var outVolume = MathHelper.Lerp(fadeOUtVolume, 0,            fadePercentage);
            var inVolume = MathHelper.Lerp(0,              fadeInVolume, fadePercentage);

            if (currentSong != null)
                currentSong.Volume = outVolume;

            if (nextSong != null)
                nextSong.Volume = inVolume;
        }
    }

    private enum STate
    {
        Playing,
        NewSongRequested,
        FadingToNext
    }
}