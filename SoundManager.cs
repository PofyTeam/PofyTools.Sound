
namespace PofyTools
{
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;

	[RequireComponent (typeof(AudioListener))]
	public class SoundManager : MonoBehaviour, IDictionary<string,AudioClip>
	{
		
		public AudioListener audioListener{ get; private set; }

		private List<AudioSource> _sources;
		private AudioSource _musicSource;

		public AudioClip[] clips;
		private Dictionary<string,AudioClip> _dictionary;
		public AudioClip music;
		public static SoundManager Sounds;
		public int voices = 1;
		private int _head = 0;
		public Range volumeVariationRange = new Range (0.9f, 1), pitchVariationRange = new Range (0.95f, 1.05f);
		[Range (0, 1)]public float masterVolume = 1;
		[Range (0, 1)]public float musicVolume = 1;

		[Header ("Resources")]
		public string resourcePath = "Sound";
		public bool loadFromResources = true;

		void Awake ()
		{
			if (Sounds == null) {
				Sounds = this;
				Initialize ();
				DontDestroyOnLoad (this.gameObject);
			} else if (Sounds != this) {
				Destroy (this.gameObject);
			}
		}

		void Initialize ()
		{
			this.audioListener = GetComponent<AudioListener> ();
			if (this.loadFromResources)
				LoadResourceSounds ();
			LoadPrefabSounds ();



		}

		void LoadResourceSounds ()
		{
			AudioClip[] resourceClips = Resources.LoadAll<AudioClip> (this.resourcePath);

			this._dictionary = new Dictionary<string, AudioClip> (resourceClips.Length + this.clips.Length);

			foreach (var clip in resourceClips) {
				this [clip.name] = clip;
			}
		}

		void LoadPrefabSounds ()
		{
			if (this.music != null) {
				this._musicSource = this.gameObject.AddComponent<AudioSource> ();
				this._musicSource.clip = this.music;
				//this._musicSource.playOnAwake = true;
				this._musicSource.loop = true;
				this._musicSource.volume = this.musicVolume;
				this._musicSource.Play ();
			}

			if (this._dictionary == null)
				this._dictionary = new Dictionary<string, AudioClip> (this.clips.Length);
			
			this._sources = new List<AudioSource> (voices);
			for (int i = 0; i < this.voices; ++i) {
				this._sources.Add (this.gameObject.AddComponent<AudioSource> ());
			}

			for (int i = this.clips.Length - 1; i >= 0; --i) {
				this._dictionary [this.clips [i].name] = this.clips [i];
			}
		}

		#region Play

		public static AudioSource Play (string clip, float volume = 1f, float pitch = 1f, bool loop = false, bool lowPriority = false)
		{
			AudioClip audioClip = Sounds [clip];
			return PlayOnAvailableSource (audioClip, volume, pitch, loop, lowPriority);

		}

		public static AudioSource Play (AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false, bool lowPriority = false)
		{
			return PlayOnAvailableSource (clip, volume, pitch, loop, lowPriority);
		}
			
		//plays a clip with pitch/volume variation
		public static AudioSource PlayVariation (string clip, bool loop = false, bool lowPriority = false)
		{
			return Play (clip, Sounds.volumeVariationRange.Random, Sounds.pitchVariationRange.Random, loop, lowPriority);
		}

		//plays a clip with pitch/volume variation
		public static AudioSource PlayVariation (AudioClip clip, bool loop = false, bool lowPriority = false)
		{
			return Play (clip, Sounds.volumeVariationRange.Random, Sounds.pitchVariationRange.Random, loop, lowPriority);
		}

		public static AudioSource PlayRandomFrom (params string[]clips)
		{
			return PlayVariation (clips [Random.Range (0, clips.Length)]);
		}

		public static AudioSource PlayRandomCustom (params AudioClip[]clips)
		{
			return PlayVariation (clips [Random.Range (0, clips.Length)]);
		}

		//Plays clip that is not in manager's dictionary
		private static AudioSource PlayOnAvailableSource (AudioClip clip, float volume = 1, float pitch = 1, bool loop = false, bool lowPriority = false)
		{
			AudioSource source = Sounds._sources [Sounds._head];
			int startHeadPosition = Sounds._head;

			while (source.isPlaying) {
				Sounds._head++;
				if (Sounds._head == Sounds._sources.Count) {
					Sounds._head = 0;
				}
				source = Sounds._sources [Sounds._head];

				if (Sounds._head == startHeadPosition) {
					if (lowPriority) {
						return null;
					}

					while (source.loop) {
						Sounds._head++;
						if (Sounds._head == Sounds._sources.Count) {
							Sounds._head = 0;
						}
						source = Sounds._sources [Sounds._head];
						Debug.Log (Sounds._head);
						if (Sounds._head == startHeadPosition) {
							break;
						}
					}
					break;
				}
			}

			source.clip = clip;
			source.volume = volume * Sounds.masterVolume;
			source.pitch = pitch;
			source.loop = loop;

			source.Play ();

			return source;
		}

		#endregion

		#region Mute

		public static void MuteAll ()
		{
			MuteSound ();
			MuteMusic ();
		}

		public static void UnMuteAll ()
		{
			MuteSound (false);
			MuteMusic (false);
		}

		public static void MuteSound (bool mute = true)
		{
			for (int i = 0, Controller_sourcesCount = Sounds._sources.Count; i < Controller_sourcesCount; i++) {
				var source = Sounds._sources [i];
				source.mute = mute;
			}
		}

		public static void MuteMusic (bool mute = true)
		{
			Sounds._musicSource.mute = mute;
		}

		public static void PauseAll ()
		{
			if (Sounds.music != null)
				Sounds._musicSource.Pause ();

			for (int i = 0, Controller_sourcesCount = Sounds._sources.Count; i < Controller_sourcesCount; i++) {
				var source = Sounds._sources [i];
				source.Pause ();
			}
		}

		public static void ResumeAll ()
		{
			if (Sounds.music != null)
				Sounds._musicSource.UnPause ();
			
			for (int i = 0, Controller_sourcesCount = Sounds._sources.Count; i < Controller_sourcesCount; i++) {
				var source = Sounds._sources [i];
				source.UnPause ();
			}
		}

		public static void StopAll ()
		{
			if (Sounds.music != null)
				Sounds._musicSource.Stop ();
			
			for (int i = 0, Controller_sourcesCount = Sounds._sources.Count; i < Controller_sourcesCount; i++) {
				var source = Sounds._sources [i];
				source.Stop ();
				source.loop = false;
			}
		}

		#endregion


		#region IDictionary implementation

		public bool ContainsKey (string key)
		{
			return this._dictionary.ContainsKey (key);
		}

		public void Add (string key, AudioClip value)
		{
			this._dictionary.Add (key, value);
		}

		public bool Remove (string key)
		{
			return this._dictionary.Remove (key);
		}

		public bool TryGetValue (string key, out AudioClip value)
		{
			return this._dictionary.TryGetValue (key, out value);
		}

		public AudioClip this [string index] {
			get {
				return this._dictionary [index];
			}
			set {
				this._dictionary [index] = value;
			}
		}

		public ICollection<string> Keys {
			get {
				return this._dictionary.Keys;
			}
		}

		public ICollection<AudioClip> Values {
			get {
				return this._dictionary.Values;
			}
		}

		#endregion

		#region ICollection implementation

		public void Add (KeyValuePair<string, AudioClip> item)
		{
			throw new System.NotImplementedException ();
		}

		public void Clear ()
		{
			this._dictionary.Clear ();
		}

		public bool Contains (KeyValuePair<string, AudioClip> item)
		{
			throw new System.NotImplementedException ();
		}

		public void CopyTo (KeyValuePair<string, AudioClip>[] array, int arrayIndex)
		{
			throw new System.NotImplementedException ();
		}

		public bool Remove (KeyValuePair<string, AudioClip> item)
		{
			throw new System.NotImplementedException ();
		}

		public int Count {
			get {
				return this._dictionary.Count;
			}
		}

		public bool IsReadOnly {
			get {
				return true;
			}
		}

		#endregion

		#region IEnumerable implementation

		public IEnumerator<KeyValuePair<string, AudioClip>> GetEnumerator ()
		{
			return this._dictionary.GetEnumerator ();
		}

		#endregion

		#region IEnumerable implementation

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return this._dictionary.GetEnumerator ();
		}

		#endregion
	}
}