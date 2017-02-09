# PofyTools.Sound
Sound Manager for Unity. Requires PofyTools.Core component.

## Version 0.0.2 Changes
- Added Music Ducking
- Added Fade Utility methods
- Added Music Cross-Mixing
- Added Automatic Music Ducking on Sound Playback

#How To Use

##Setup
1. Add `PofyTools.Sound` and `PofyTools.Core` files to your Unity project ([You can find Core here.(https://github.com/PofyTeam/PofyTools.Core "PofyTools.Core")).
2. Attach `SoundManager` script to a new Empty `GameObject` (or existing persisting `GameObject`).
3. `SoundManager` has `[RequireComponent(typeof(AudioListener))]` and will add `AudioListener` component to it's `GameObject` if not present.
Make sure to remove all other `AudioListener` components from other objects in the scene (by default object with `Camera` component has one). 
4. There are three ways `AuidoClip` references can be organized:
* Adding all `AudioClip` references to `public AudioClip[] clips` via Inspector. At runtime `SoundManager` will create `Dictionary<string, AudioClip>` using `AudioClip.name` as a key.
* Providing sound resource directory name to `public string resourcePath` and checking the `public bool loadFromResources` checkbox. All sounds found on this address will be added to manager's runtime Dictionary.
* or you can use it without mapping resource, by using method overloads that take `AudioClip` as parameter.

###More Options
- Specify how many sounds can play simultaneously by setting `public int voices` in Inspector.
- `public Range volumeVariationRange` and `public pitchVariationRange` settings are used for creating random variations in sound playback when using `PlayVariation` method.
- `public AudioClip music` is used to specify default music. To start playing music call `PlayMusic` method.
- To Cross-Mix music on track change, tick `public bool crossMixMusic` in Inspector. The duration of Cross-Mixing is set via `public float crossMixDuration`.
- If you want to use music ducking automaticly on every sound, tick `public bool duckMusicOnSound` checkbox.
Music will duck to `public float duckOnSoundVolume`. Use `public float duckOnSoundTransitionDuration` to specify how fast will music duck.
- `[Range(0, 1)]public float musicVolume` and `[Range(0, 1)]public float masterVolume` are used to control global sound and music volume.


##Use

```c#

AudioClip myClip;

//Play myClip
SoundManager.Play()


```