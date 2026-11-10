using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Plays all the sound effects and music in the game.
//
// HOW TO ADD SOUNDS (When we have audio so I don't forget):
//   1. Open the prefab  Assets/Resources/AudioManager
//   2. Drag audio clips into the "Sounds" list. Each entry is one SoundId (Jump, Footstep...).
//      Put more than one clip in an entry and it picks a random one each time.
//   3. Music: drag clips into "Scene Music" (each entry is a scene name, or the start of one, like "Floor4"),
//      and into "Tense Music" / "Chase Music".
//   That's it. Sounds with no clip are just silent, so the game works without any audio.
//
// The AudioManager makes itself when the game starts (from the prefab in Resources) and
// lives across scenes, so it does NOT need to be placed in any scene.
// Tick "Log Sounds Without Clips" to see in the Console which sounds the game tried to play.
public class AudioManager : MonoBehaviour
{
    // One sound effect: a name, some clips to pick from, and how it should play
    [System.Serializable]
    public class Sound
    {
        public SoundId id;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0f, 0.5f)] public float pitchVariation = 0.05f;   // a tiny random pitch change so repeats don't sound identical
    }

    // The calm music for one scene (or a group of scenes that start with the same name, like "Floor4")
    [System.Serializable]
    public class SceneMusic
    {
        public string sceneName;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    public static AudioManager Instance { get; private set; }

    [Header("Volumes")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.6f;

    [Header("Sound effects")]
    [SerializeField] private List<Sound> sounds = new List<Sound>();
    // Sounds played with a position get quieter the further they are from the camera
    [SerializeField] private float hearDistance = 25f;

    [Header("Music")]
    [SerializeField] private List<SceneMusic> sceneMusic = new List<SceneMusic>();
    [SerializeField] private AudioClip tenseMusic;   // plays while an enemy has spotted Nin
    [SerializeField] private AudioClip chaseMusic;   // plays while a Wandering Shadow is attacking
    [Range(0f, 1f)] [SerializeField] private float tenseVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float chaseVolume = 1f;
    [SerializeField] private float fadeSpeed = 1f;   // how quickly music fades in and out (volume per second)

    [Header("Testing")]
    [SerializeField] private bool logSoundsWithoutClips = false;

    private const int EffectSources = 8;          // how many sound effects can play at the same time
    private const float ThreatCheckTime = 0.25f;  // how often we look at the enemies for the music

    private enum Threat { None, Tense, Chase }

    private readonly Dictionary<SoundId, Sound> soundLookup = new Dictionary<SoundId, Sound>();
    private AudioSource[] effectSources;
    private int nextEffectSource;

    private AudioSource calmSource;
    private AudioSource tenseSource;
    private AudioSource chaseSource;
    private float calmVolumeForScene = 1f;
    private Threat threat = Threat.None;
    private float threatTimer;

    // ---------------------------------------------------------------- start up

    // Makes the AudioManager automatically when the game starts
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateWhenGameStarts()
    {
        if (Instance != null) return;

        AudioManager prefab = Resources.Load<AudioManager>("AudioManager");
        if (prefab != null)
        {
            Instantiate(prefab);
        }
        else
        {
            new GameObject("AudioManager").AddComponent<AudioManager>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (Sound sound in sounds) soundLookup[sound.id] = sound;

        effectSources = new AudioSource[EffectSources];
        for (int i = 0; i < EffectSources; i++) effectSources[i] = MakeSource(false);

        calmSource = MakeSource(true);
        tenseSource = MakeSource(true);
        chaseSource = MakeSource(true);

        tenseSource.clip = tenseMusic;
        chaseSource.clip = chaseMusic;
        calmSource.volume = 0f;
        tenseSource.volume = 0f;
        chaseSource.volume = 0f;
        if (tenseMusic != null) tenseSource.Play();
        if (chaseMusic != null) chaseSource.Play();

        SceneManager.sceneLoaded += OnSceneLoaded;
        StartSceneMusic(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private AudioSource MakeSource(bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;   // 2D game, so no 3D sound
        return source;
    }

    // ---------------------------------------------------------------- sound effects

    // Plays a sound at full volume (use for things that happen to Nin or in the menus)
    public static void Play(SoundId id)
    {
        if (Instance != null) Instance.PlayEffect(id, 1f);
    }

    // Plays a sound that happens somewhere in the world. It gets quieter the further it is from the camera.
    public static void Play(SoundId id, Vector2 position)
    {
        if (Instance == null) return;

        float volumeScale = 1f;
        Camera cam = Camera.main;
        if (cam != null)
        {
            float distance = Vector2.Distance(position, cam.transform.position);
            volumeScale = 1f - Mathf.Clamp01(distance / Instance.hearDistance);
        }

        Instance.PlayEffect(id, volumeScale);
    }

    private void PlayEffect(SoundId id, float volumeScale)
    {
        Sound sound;
        bool found = soundLookup.TryGetValue(id, out sound);

        if (!found || sound.clips == null || sound.clips.Length == 0)
        {
            if (logSoundsWithoutClips) Debug.Log("Sound played (no clip yet): " + id);
            return;
        }

        if (volumeScale <= 0f) return;

        AudioClip clip = sound.clips[Random.Range(0, sound.clips.Length)];
        if (clip == null) return;

        AudioSource source = effectSources[nextEffectSource];
        nextEffectSource = (nextEffectSource + 1) % effectSources.Length;

        source.clip = clip;
        source.pitch = 1f + Random.Range(-sound.pitchVariation, sound.pitchVariation);
        source.volume = sound.volume * volumeScale * sfxVolume * masterVolume;
        source.Play();
    }

    // ---------------------------------------------------------------- music

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartSceneMusic(scene.name);
    }

    // Finds this scene's calm music. If it's already playing we leave it alone, so it doesn't restart between rooms.
    // An entry matches when the scene's name STARTS with the entry's name (so "Floor4" covers Floor4_Hall and
    // Floor4_Stairs). If two entries match, the longer, more specific name wins.
    private void StartSceneMusic(string sceneName)
    {
        AudioClip wanted = null;
        calmVolumeForScene = 1f;
        int bestMatchLength = 0;

        foreach (SceneMusic music in sceneMusic)
        {
            if (string.IsNullOrEmpty(music.sceneName)) continue;
            if (!sceneName.StartsWith(music.sceneName)) continue;
            if (music.sceneName.Length < bestMatchLength) continue;

            bestMatchLength = music.sceneName.Length;
            wanted = music.clip;
            calmVolumeForScene = music.volume;
        }

        if (wanted == calmSource.clip) return;

        calmSource.clip = wanted;
        if (wanted != null) calmSource.Play();
        else calmSource.Stop();
    }

    private void Update()
    {
        threatTimer -= Time.deltaTime;
        if (threatTimer <= 0f)
        {
            threatTimer = ThreatCheckTime;
            threat = FindThreat();
        }

        // Calm music plays when nothing is hunting Nin. Tense and chase music fade in over the top.
        FadeTo(calmSource, threat == Threat.None ? calmVolumeForScene : 0f);
        FadeTo(tenseSource, threat == Threat.Tense ? tenseVolume : 0f);
        FadeTo(chaseSource, threat == Threat.Chase ? chaseVolume : 0f);
    }

    private void FadeTo(AudioSource source, float target)
    {
        float goal = target * musicVolume * masterVolume;
        source.volume = Mathf.MoveTowards(source.volume, goal, fadeSpeed * Time.deltaTime);
    }

    // Looks at every enemy to see how dangerous things are right now
    private Threat FindThreat()
    {
        Threat result = Threat.None;

        foreach (WanderingShadow shadow in FindObjectsByType<WanderingShadow>(FindObjectsInactive.Exclude))
        {
            if (shadow.CurrentState == WanderingShadow.State.Attack) return Threat.Chase;
            if (shadow.CurrentState == WanderingShadow.State.Alert) result = Threat.Tense;
        }

        foreach (GlaringEyes eyes in FindObjectsByType<GlaringEyes>(FindObjectsInactive.Exclude))
        {
            if (eyes.CurrentState == GlaringEyes.State.Staring) result = Threat.Tense;
        }

        return result;
    }
}
