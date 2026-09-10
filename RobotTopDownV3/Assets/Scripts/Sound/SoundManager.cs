using System.Collections.Generic;
using UnityEngine;
using System;

//Not SingletonPersistant: this sits on a CHILD GameObject, where DontDestroyOnLoad is a no-op and only logs a
//warning. It survives scene loads through its persisting root (GameManager.prefab). Moving it out from under that root
//would silently break that - it would need to become SingletonPersistant again, on a root object.
public class SoundManager : Singleton<SoundManager>
{
	[SerializeField]
	private SfxDatabase database;
	[SerializeField]
	private int poolSize = 16;

	private readonly List<AudioSource> pool = new();
	private readonly Dictionary<SfxId, SfxData> lookup = new();

	public float MasterVolume => GameDatas.current.app.sfxVolume;

	public override void Awake ()
	{
		base.Awake();
		BuildLookup();
		CreatePool();
	}

	public void SetMasterVolume ( float _volume )
	{
		GameDatas.current.app.sfxVolume = Mathf.Clamp01(_volume);
		ApplicationManager.Instance.SaveApplication();
	}

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

			pool.Add(source);
		}
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

	public void Play ( SfxId _id )
	{
		if (!lookup.TryGetValue(_id, out SfxData sound))
		{
			Debug.LogWarning($"Missing SFX : {_id}");
			return;
		}

		if (sound.Clip == null)
			return;

		AudioSource source = GetFreeSource();

		source.pitch = sound.Pitch;
		source.PlayOneShot(sound.Clip, sound.Volume * MasterVolume);
	}
}