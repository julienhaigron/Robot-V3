using System.Collections.Generic;
using UnityEngine;
using System;

public class SoundManager : MonoBehaviour
{
	public static SoundManager Instance { get; private set; }

	[SerializeField]
	private SfxDatabase database;
	[SerializeField]
	private int poolSize = 16;

	private readonly List<AudioSource> pool = new();
	private readonly Dictionary<SfxId, SfxData> lookup = new();

	private void Awake ()
	{
		if (Instance != null)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;

		BuildLookup();
		CreatePool();
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
		source.PlayOneShot(sound.Clip, sound.Volume);
	}
}