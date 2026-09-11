using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;

//Not SingletonPersistant: this sits on a CHILD GameObject, where DontDestroyOnLoad is a no-op and only logs a
//warning. It survives scene loads through its persisting root (GameManager.prefab). Moving it out from under that root
//would silently break that - it would need to become SingletonPersistant again, on a root object.
public class SoundManager : Singleton<SoundManager>
{
	[SerializeField]
	private SfxDatabase database;
	[SerializeField]
	private UISoundConfig uiSounds;
	[SerializeField]
	private int poolSize = 16;
	[SerializeField]
	private float musicFadeDuration = 1f;

	private readonly List<AudioSource> pool = new();
	private readonly Dictionary<SfxId, SfxData> lookup = new();

	private AudioSource musicSource;
	private SfxId currentMusicId = SfxId.None;
	private float currentMusicClipVolume = 1f;
	private float musicFade = 1f;
	private Tween musicTween;

	public static Action<SoundChannel> onChannelVolumeChanged;
	public static Action onMasterVolumeChanged;

	public UISoundConfig UISounds => uiSounds;
	public SfxId CurrentMusicId => currentMusicId;

	public float MasterVolume => GameDatas.current.app.masterVolume;

	public override void Awake ()
	{
		base.Awake();
		BuildLookup();
		CreatePool();
		CreateMusicSource();
	}

	private void Start ()
	{
		ApplyMasterVolume();
		GameDatas.onAfterLoad += ApplyMasterVolume;
	}

	private void OnDestroy ()
	{
		GameDatas.onAfterLoad -= ApplyMasterVolume;
		musicTween?.Kill();
	}

	#region Init

	private void BuildLookup ()
	{
		lookup.Clear();

		foreach (var sound in database.Sounds)
		{
			if (Enum.TryParse(sound.Id, out SfxId id))
			{
				if (lookup.ContainsKey(id))
				{
					Debug.LogWarning($"Duplicate SFX Id : {sound.Id}");
					continue;
				}

				lookup.Add(id, sound);
			}
		}
	}

	private void CreatePool ()
	{
		for (int i = 0; i < poolSize; i++)
		{
			GameObject go = new($"AudioSource_{i}");

			go.transform.SetParent(transform);

			AudioSource source = go.AddComponent<AudioSource>();
			source.playOnAwake = false;

			pool.Add(source);
		}
	}

	private void CreateMusicSource ()
	{
		GameObject go = new("MusicSource");

		go.transform.SetParent(transform);

		musicSource = go.AddComponent<AudioSource>();
		musicSource.playOnAwake = false;
		musicSource.loop = true;
		musicSource.spatialBlend = 0f;
	}

	private AudioSource GetFreeSource ()
	{
		foreach (AudioSource source in pool)
		{
			if (!source.isPlaying)
				return source;
		}

		return pool[0];
	}

	#endregion

	#region Volumes

	public float GetChannelVolume ( SoundChannel _channel )
	{
		GameDatas.App app = GameDatas.current.app;

		switch (_channel)
		{
			case SoundChannel.Music: return app.musicVolume;
			case SoundChannel.UI: return app.uiVolume;
			default: return app.sfxVolume;
		}
	}

	public void SetChannelVolume ( SoundChannel _channel, float _volume, bool _save = true )
	{
		GameDatas.App app = GameDatas.current.app;
		_volume = Mathf.Clamp01(_volume);

		switch (_channel)
		{
			case SoundChannel.Music: app.musicVolume = _volume; break;
			case SoundChannel.UI: app.uiVolume = _volume; break;
			default: app.sfxVolume = _volume; break;
		}

		if (_channel == SoundChannel.Music)
			RefreshMusicVolume();

		onChannelVolumeChanged?.Invoke(_channel);

		if (_save)
			SaveVolumes();
	}

	public void SetMasterVolume ( float _volume, bool _save = true )
	{
		GameDatas.current.app.masterVolume = Mathf.Clamp01(_volume);
		ApplyMasterVolume();

		onMasterVolumeChanged?.Invoke();

		if (_save)
			SaveVolumes();
	}

	public void SaveVolumes ()
	{
		if (ApplicationManager.Instance != null)
			ApplicationManager.Instance.SaveApplication();
	}

	private void ApplyMasterVolume ()
	{
		AudioListener.volume = Mathf.Clamp01(GameDatas.current.app.masterVolume);
	}

	#endregion

	#region Sfx

	public void Play ( SfxId _id )
	{
		PlayInternal(_id, null);
	}

	public void Play ( SfxId _id, SoundChannel _channel )
	{
		PlayInternal(_id, _channel);
	}

	public void PlayUI ( SfxId _id )
	{
		PlayInternal(_id, SoundChannel.UI);
	}

	public static void PlayUISafe ( SfxId _id )
	{
		if (_id == SfxId.None || Instance == null)
			return;

		Instance.PlayUI(_id);
	}

	public static void PlayUISafe ( SfxId _override, Func<UISoundConfig, SfxId> _default, bool _useDefault = true )
	{
		if (_override != SfxId.None)
		{
			PlayUISafe(_override);
			return;
		}

		if (!_useDefault || Instance == null || Instance.uiSounds == null)
			return;

		PlayUISafe(_default(Instance.uiSounds));
	}

	private void PlayInternal ( SfxId _id, SoundChannel? _channelOverride )
	{
		if (_id == SfxId.None)
			return;

		if (!lookup.TryGetValue(_id, out SfxData sound))
		{
			Debug.LogWarning($"Missing SFX : {_id}");
			return;
		}

		if (sound.Clip == null)
			return;

		SoundChannel channel = _channelOverride ?? sound.Channel;

		if (channel == SoundChannel.Music)
		{
			PlayMusic(_id);
			return;
		}

		AudioSource source = GetFreeSource();

		source.pitch = sound.Pitch;
		source.PlayOneShot(sound.Clip, sound.Volume * GetChannelVolume(channel));
	}

	#endregion

	#region Music

	public void PlayMusic ( SfxId _id, bool _fade = true )
	{
		if (_id == SfxId.None)
		{
			StopMusic(_fade);
			return;
		}

		if (_id == currentMusicId && musicSource.isPlaying)
			return;

		if (!lookup.TryGetValue(_id, out SfxData sound) || sound.Clip == null)
		{
			Debug.LogWarning($"Missing music : {_id}");
			return;
		}

		currentMusicId = _id;
		musicTween?.Kill();

		float duration = _fade ? musicFadeDuration : 0f;

		if (duration <= 0f)
		{
			StartMusicClip(sound, 1f);
			return;
		}

		if (!musicSource.isPlaying)
		{
			StartMusicClip(sound, 0f);
			musicTween = FadeMusic(1f, duration);
			return;
		}

		musicTween = FadeMusic(0f, duration * .5f).OnComplete(() =>
		{
			StartMusicClip(sound, 0f);
			musicTween = FadeMusic(1f, duration * .5f);
		});
	}

	public void StopMusic ( bool _fade = true )
	{
		currentMusicId = SfxId.None;
		musicTween?.Kill();

		if (!_fade || musicFadeDuration <= 0f || !musicSource.isPlaying)
		{
			ClearMusicClip();
			return;
		}

		musicTween = FadeMusic(0f, musicFadeDuration).OnComplete(ClearMusicClip);
	}

	private void StartMusicClip ( SfxData _sound, float _fade )
	{
		currentMusicClipVolume = _sound.Volume;
		musicFade = _fade;

		musicSource.clip = _sound.Clip;
		musicSource.pitch = _sound.Pitch;
		RefreshMusicVolume();
		musicSource.Play();
	}

	private void ClearMusicClip ()
	{
		musicSource.Stop();
		musicSource.clip = null;
	}

	private Tween FadeMusic ( float _target, float _duration )
	{
		return DOTween.To(() => musicFade, _value =>
		{
			musicFade = _value;
			RefreshMusicVolume();
		}, _target, _duration).SetUpdate(true);
	}

	private void RefreshMusicVolume ()
	{
		musicSource.volume = currentMusicClipVolume * GetChannelVolume(SoundChannel.Music) * musicFade;
	}

	#endregion
}
