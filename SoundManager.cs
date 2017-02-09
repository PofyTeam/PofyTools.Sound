
namespace PofyTools.Sound
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;

    [RequireComponent(typeof(AudioListener))]
    public class SoundManager : MonoBehaviour, IDictionary<string,AudioClip>
    {
        public const string TAG = "<color=red><b><i>SoundManager: </i></b></color>";

        public static SoundManager Sounds;

        [Header("Sounds")]
        public AudioClip[] clips;

        public int voices = 1;
        private int _head = 0;
        public Range volumeVariationRange = new Range(0.9f, 1), pitchVariationRange = new Range(0.95f, 1.05f);

        public AudioListener audioListener{ get; private set; }

        private List<AudioSource> _sources;

        [Header("Music")]
        public AudioClip music;

        public bool crossMixMusic;
        public float crossMixDuration = 0.2f;

        public bool duckMusicOnSound;
        public float duckOnSoundTransitionDuration = 0.1f, duckOnSoundVolume = 0.2f;

        private AudioSource _musicSource
        {
            get
            {
                return this._musicSources[this._musicHead];
            }
        }

        private AudioSource[] _musicSources;
        private int _musicHead = -1;

        [Range(0, 1)]public float musicVolume = 1;

        [Header("Master")]
        [Range(0, 1)]public float masterVolume = 1;

        [Header("Resources")]
        public string resourcePath = "Sound";
        public bool loadFromResources = true;
        private Dictionary<string,AudioClip> _dictionary;

        void Awake()
        {
            if (Sounds == null)
            {
                Sounds = this;
                Initialize();
                DontDestroyOnLoad(this.gameObject);
            }
            else if (Sounds != this)
            {
                Destroy(this.gameObject);
            }
        }

        void OnDestroy()
        {
            StopAllCoroutines();
        }

        void Initialize()
        {
            this.audioListener = GetComponent<AudioListener>();

            this._musicSources = new AudioSource[2];
            this._musicSources[0] = this.gameObject.AddComponent<AudioSource>();
            this._musicSources[1] = this.gameObject.AddComponent<AudioSource>();
            this._musicHead = 0;

            if (this.loadFromResources)
                LoadResourceSounds();
            LoadPrefabSounds();
        }

        void LoadResourceSounds()
        {
            AudioClip[] resourceClips = Resources.LoadAll<AudioClip>(this.resourcePath);

            this._dictionary = new Dictionary<string, AudioClip>(resourceClips.Length + this.clips.Length);

            foreach (var clip in resourceClips)
            {
                this[clip.name] = clip;
            }
        }

        void LoadPrefabSounds()
        {
            
            if (this.music != null)
            {
                this._musicSource.clip = this.music;

                this._musicSource.loop = true;
                this._musicSource.volume = this.musicVolume * this.masterVolume;
            }

            if (this._dictionary == null)
                this._dictionary = new Dictionary<string, AudioClip>(this.clips.Length);
			
            this._sources = new List<AudioSource>(voices);
            for (int i = 0; i < this.voices; ++i)
            {
                this._sources.Add(this.gameObject.AddComponent<AudioSource>());
            }

            for (int i = this.clips.Length - 1; i >= 0; --i)
            {
                this._dictionary[this.clips[i].name] = this.clips[i];
            }
        }

        #region Play

        public static AudioSource Play(string clip, float volume = 1f, float pitch = 1f, bool loop = false, bool lowPriority = false)
        {
            AudioClip audioClip = Sounds[clip];
            return PlayOnAvailableSource(audioClip, volume, pitch, loop, lowPriority);

        }

        public static AudioSource Play(AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false, bool lowPriority = false)
        {
            return PlayOnAvailableSource(clip, volume, pitch, loop, lowPriority);
        }
			
        //plays a clip with pitch/volume variation
        public static AudioSource PlayVariation(string clip, bool loop = false, bool lowPriority = false)
        {
            return Play(clip, Sounds.volumeVariationRange.Random, Sounds.pitchVariationRange.Random, loop, lowPriority);
        }

        //plays a clip with pitch/volume variation
        public static AudioSource PlayVariation(AudioClip clip, bool loop = false, bool lowPriority = false)
        {
            return Play(clip, Sounds.volumeVariationRange.Random, Sounds.pitchVariationRange.Random, loop, lowPriority);
        }

        public static AudioSource PlayRandomFrom(params string[]clips)
        {
            return PlayVariation(clips[Random.Range(0, clips.Length)]);
        }

        public static AudioSource PlayRandomFrom(List<string> list)
        {
            return PlayVariation(list[Random.Range(0, list.Count)]);
        }

        public static AudioSource PlayRandomCustom(params AudioClip[]clips)
        {
            return PlayVariation(clips[Random.Range(0, clips.Length)]);
        }

        public static void PlayMusic()
        {
            Sounds._musicSource.Play();
        }

        public static bool IsMusicPlaying()
        {
            return Sounds._musicSource.isPlaying;
        }

        public static void PlayCustomMusic(AudioClip newMusic)
        {
            //set up the other music source
            var source = Sounds._musicSources[1 - Sounds._musicHead];

            source.clip = newMusic;
            source.loop = true;


            if (Sounds.crossMixMusic)
            {
                source.volume = 0;
                source.Play();
                Sounds.CrossMix(Sounds.crossMixDuration);
            }
            else
            {
                if (Sounds._musicSource.isPlaying)
                    Sounds._musicSource.Stop();

                source.volume = Sounds.masterVolume * Sounds.musicVolume;
                Sounds._musicHead = 1 - Sounds._musicHead;
                Sounds._musicSource.Play();
            }


        }

        //Plays clip that is not in manager's dictionary
        private static AudioSource PlayOnAvailableSource(AudioClip clip, float volume = 1, float pitch = 1, bool loop = false, bool lowPriority = false)
        {
            AudioSource source = Sounds._sources[Sounds._head];
            int startHeadPosition = Sounds._head;

            while (source.isPlaying)
            {
                Sounds._head++;
                if (Sounds._head == Sounds._sources.Count)
                {
                    Sounds._head = 0;
                }
                source = Sounds._sources[Sounds._head];

                if (Sounds._head == startHeadPosition)
                {
                    if (lowPriority)
                    {
                        return null;
                    }

                    while (source.loop)
                    {
                        Sounds._head++;
                        if (Sounds._head == Sounds._sources.Count)
                        {
                            Sounds._head = 0;
                        }
                        source = Sounds._sources[Sounds._head];
                        Debug.Log(Sounds._head);
                        if (Sounds._head == startHeadPosition)
                        {
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

            source.Play();

            if (Sounds.duckMusicOnSound)
                DuckMusicOnSound(clip);
            return source;
        }

        #endregion

        #region Mute

        public static void MuteAll()
        {
            MuteSound(true);
            MuteMusic(true);
        }

        public static void UnMuteAll()
        {
            MuteSound(false);
            MuteMusic(false);
        }

        public static void MuteSound(bool mute = true)
        {
            for (int i = 0, Controller_sourcesCount = Sounds._sources.Count; i < Controller_sourcesCount; i++)
            {
                var source = Sounds._sources[i];
                source.mute = mute;
            }
        }

        public static void MuteMusic(bool mute = true)
        {
            Sounds._musicSource.mute = mute;
        }

        public static void PauseAll()
        {
			
            PauseMusic();
            PauseSound();
        }

        public static void PauseMusic()
        {
            Sounds._musicSource.Pause();
        }

        public static void PauseSound()
        {
            for (int i = 0, Controller_sourcesCount = Sounds._sources.Count; i < Controller_sourcesCount; i++)
            {
                var source = Sounds._sources[i];
                source.Pause();
            }
        }

        public static void ResumeAll()
        {
            ResumeMusic();
            ResumeSound();
        }

        public static void ResumeMusic()
        {
            Sounds._musicSource.UnPause();
        }

        public static void ResumeSound()
        {
            for (int i = 0, Controller_sourcesCount = Sounds._sources.Count; i < Controller_sourcesCount; i++)
            {
                var source = Sounds._sources[i];
                source.UnPause();
            }
        }

        public static void StopAll()
        {
            Sounds._musicSource.Stop();
			
            for (int i = 0, Controller_sourcesCount = Sounds._sources.Count; i < Controller_sourcesCount; i++)
            {
                var source = Sounds._sources[i];
                source.Stop();
                source.loop = false;
            }
        }

        #endregion

        #region Ducking

        //Music Ducking
        private float _musicDuckingVolume;
        private float _musicDuckingTimer;
        private float _musicDuckingDuration;

        //Sound Ducking
        private float _soundDuckingVolume;
        private float _soundDuckingTimer;
        private float _soundDuckingDuration;


        public static bool IsMusicDucked
        {
            get { return !(Sounds._musicSource.volume > Sounds._musicDuckingVolume); }
        }

        public static void DuckAll(float duckToVolume = 1f, float duckingDuration = 0.5f)
        {
            DuckMusic(duckToVolume, duckingDuration);
            DuckSound(duckToVolume, duckingDuration);
        }

        public static void DuckMusic(float duckToVolume = 0f, float duckingDuration = 0.5f, bool onSound = false)
        {
            Sounds.StopCoroutine(Sounds.DuckMusic());

            Sounds._musicDuckingVolume = duckToVolume * Sounds.musicVolume * Sounds.masterVolume;
            Sounds._musicDuckingDuration = duckingDuration;
            Sounds._musicDuckingTimer = duckingDuration;

            if (!onSound)
                Sounds.StartCoroutine(Sounds.DuckMusic());
            else
                Sounds.StartCoroutine(Sounds.DuckMusicOnSound());
        }

        public static void DuckSound(float duckToVolume = 0f, float duckingDuration = 0.5f)
        {
            Sounds.StopCoroutine(Sounds.DuckSound());

            Sounds._soundDuckingVolume = duckToVolume * Sounds.masterVolume;
            Sounds._soundDuckingDuration = duckingDuration;
            Sounds._soundDuckingTimer = duckingDuration;

            Sounds.StartCoroutine(Sounds.DuckSound());
        }

        IEnumerator DuckMusicOnSound()
        {
            yield return DuckMusic();
            yield return new WaitForSeconds(Mathf.Max(this._duckOnSoundDuration - this.duckOnSoundTransitionDuration, 0));
            DuckMusic(1);
        }

        IEnumerator DuckMusic()
        {
            while (this._musicDuckingTimer > 0)
            {
                this._musicDuckingTimer -= Time.unscaledDeltaTime;
                if (this._musicDuckingTimer < 0)
                    this._musicDuckingTimer = 0;

                float normalizedTime = 1 - this._musicDuckingTimer / this._musicDuckingDuration;
                this._musicSource.volume = Mathf.Lerp(this._musicSource.volume, this._musicDuckingVolume, normalizedTime);
                yield return null;
            }
//            SoundManager.IsMusicDucked = this._musicSource.volume
            //Restore on sound end
        }

        private float _duckOnSoundDuration = 0;

        public static void DuckMusicOnSound(AudioClip sound)
        {
            Sounds.StopCoroutine(Sounds.DuckMusic());
            Debug.Log(sound.length);

            Sounds._duckOnSoundDuration = sound.length;

            DuckMusic(Sounds.duckOnSoundVolume, Sounds.duckOnSoundTransitionDuration, true);
        }

        IEnumerator DuckSound()
        {
            while (this._soundDuckingTimer > 0)
            {
                this._soundDuckingTimer -= Time.unscaledDeltaTime;
                if (this._soundDuckingTimer < 0)
                    this._soundDuckingTimer = 0;

                float normalizedTime = 1 - this._soundDuckingTimer / this._soundDuckingDuration;
                foreach (var source in this._sources)
                {
                    source.volume = Mathf.Lerp(source.volume, this._soundDuckingVolume, normalizedTime);
                }
                yield return null;
            }
        }

        #endregion

        #region Cross-Mixing

        private float _crossMixDuration, _crossMixTimer;
        private AudioSource _currentMusicSource, _targetMusicSource;

        private void CrossMix(float duration)
        {
            StopCoroutine(this.CrossMix());

            this._crossMixDuration = duration;
            this._crossMixTimer = duration;

            this._currentMusicSource = this._musicSources[this._musicHead];
            this._targetMusicSource = this._musicSources[1 - this._musicHead];
            this._musicHead = 1 - this._musicHead;

            StartCoroutine(this.CrossMix());
        }

        private IEnumerator CrossMix()
        {
            while (this._crossMixTimer > 0)
            {
                this._crossMixTimer -= Time.unscaledDeltaTime;

                if (this._crossMixTimer < 0)
                    this._crossMixTimer = 0;

                float normalizedTime = 1 - this._crossMixTimer / this._crossMixDuration;

                this._currentMusicSource.volume = (1 - normalizedTime) * this.masterVolume * this.musicVolume;
                this._targetMusicSource.volume = normalizedTime * this.masterVolume * this.musicVolume;

                yield return null;
            }
        }

        #endregion

        #region IDictionary implementation

        public bool ContainsKey(string key)
        {
            return this._dictionary.ContainsKey(key);
        }

        public void Add(string key, AudioClip value)
        {
            this._dictionary.Add(key, value);
        }

        public bool Remove(string key)
        {
            return this._dictionary.Remove(key);
        }

        public bool TryGetValue(string key, out AudioClip value)
        {
            return this._dictionary.TryGetValue(key, out value);
        }

        public AudioClip this [string index]
        {
            get
            {
                return this._dictionary[index];
            }
            set
            {
                this._dictionary[index] = value;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                return this._dictionary.Keys;
            }
        }

        public ICollection<AudioClip> Values
        {
            get
            {
                return this._dictionary.Values;
            }
        }

        #endregion

        #region ICollection implementation

        public void Add(KeyValuePair<string, AudioClip> item)
        {
            throw new System.NotImplementedException();
        }

        public void Clear()
        {
            this._dictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, AudioClip> item)
        {
            throw new System.NotImplementedException();
        }

        public void CopyTo(KeyValuePair<string, AudioClip>[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(KeyValuePair<string, AudioClip> item)
        {
            throw new System.NotImplementedException();
        }

        public int Count
        {
            get
            {
                return this._dictionary.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        #endregion

        #region IEnumerable implementation

        public IEnumerator<KeyValuePair<string, AudioClip>> GetEnumerator()
        {
            return this._dictionary.GetEnumerator();
        }

        #endregion

        #region IEnumerable implementation

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._dictionary.GetEnumerator();
        }

        #endregion
    }
}