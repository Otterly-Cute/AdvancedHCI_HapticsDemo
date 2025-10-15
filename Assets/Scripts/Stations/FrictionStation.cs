using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using HapticsDemo;
using Oculus.Haptics;

public class FrictionStation : HapticStationBase
{
    [Header("Child Parts")]
    [SerializeField]
    private XRGrabInteractable stone;

    [SerializeField]
    private AudioSource bedA;

    [SerializeField]
    private AudioSource bedB;

    [SerializeField]
    private AudioSource grainSrc;

    [Header("Config")]
    [SerializeField]
    private HapticConfig scrapeConfig;

    [Header("Speed Tuning")]
    [Tooltip("Below this, no scrape.")]
    [SerializeField]
    private float minSpeed = 0.03f;

    [Tooltip("Maps to full intensity.")]
    [SerializeField]
    private float maxSpeed = 0.8f;

    [Tooltip("Seconds of exponential smoothing for speed.")]
    [SerializeField]
    private float speedSmoothing = 0.10f;

    [Header("Grain (Distance-Based)")]
    [Tooltip("Grains per meter at minimum intensity.")]
    [SerializeField]
    private float grainPerMeterAtMin = 80f;

    [Tooltip("Grains per meter at maximum intensity.")]
    [SerializeField]
    private float grainPerMeterAtMax = 260f;

    [Tooltip("Random grain length in milliseconds (min,max).")]
    [SerializeField]
    private Vector2 grainLenMs = new Vector2(35f, 90f);

    [Tooltip("Random pitch range for grains.")]
    [SerializeField]
    private Vector2 grainPitch = new Vector2(0.92f, 1.08f);

    [Tooltip("Minimum grain volume before intensity scaling.")]
    [SerializeField]
    private float grainVolMin = 0.25f;

    [Header("Bed (Continuous)")]
    [Tooltip("Crossfade time between bed clips (ms).")]
    [SerializeField]
    private float bedXfadeMs = 18f;

    [Tooltip("Randomize bed start offset within this window (s).")]
    [SerializeField]
    private float bedRandOffset = 0.20f;

    [Tooltip("Base bed volume at t=0, scaled up to sfxVolume at t=1.")]
    [SerializeField]
    private float bedMinVolume = 0.08f;

    [Header("Hit Filtering")]
    [SerializeField]
    private LayerMask surfaceMask;

    [Tooltip("Ignore trigger-only colliders (hands, sensors) when detecting contact.")]
    [SerializeField]
    private bool ignoreTriggerColliders = true;

    [Tooltip("Logs when contacts enter/exit and which layers they are.")]
    [SerializeField]
    private bool debugContacts = false;

    private bool isHeld;
    private bool inContact;
    private readonly HashSet<Collider> _contacts = new HashSet<Collider>();
    private HandTarget hand = HandTarget.Right;

    private Transform stoneTf;
    private Vector3 lastPos;
    private float smoothedSpeed;
    private float _lastIntensity;

    private float distAcc;
    private float nextGrainDist;

    private HapticClipPlayer scrapePlayer;
    private float bedHapticTimer;

    private UnityAction<SelectEnterEventArgs> _onEntered;
    private UnityAction<SelectExitEventArgs> _onExited;

    private Coroutine bedCo;

    protected override void Awake()
    {
        base.Awake();

        if (scrapeConfig != null && scrapeConfig.advancedClip != null)
            scrapePlayer = new HapticClipPlayer(scrapeConfig.advancedClip);
    }

    private void OnValidate()
    {
        if (!stone)
            stone = GetComponentInChildren<XRGrabInteractable>(true);

        if (stone)
        {
            var cols = stone.GetComponents<Collider>();
            bool hasSolid = cols != null && cols.Any(c => c.enabled && !c.isTrigger);
            if (!hasSolid)
            {
                var added = stone.gameObject.AddComponent<BoxCollider>();
                added.isTrigger = false;
                Debug.Log(
                    $"[{nameof(FrictionStation)}] Added non-trigger BoxCollider for physics on {stone.name}."
                );
            }

            EnsureRelayOnStone();
        }

        EnsureAudioSource(ref bedA, "BedA");
        EnsureAudioSource(ref bedB, "BedB");
        EnsureAudioSource(ref grainSrc, "Grain");

        Configure3DAudio(bedA);
        Configure3DAudio(bedB);
        Configure3DAudio(grainSrc);

        if (maxSpeed <= minSpeed + 0.0001f)
            maxSpeed = minSpeed + 0.0001f;

        grainLenMs.x = Mathf.Max(5f, Mathf.Min(grainLenMs.x, grainLenMs.y));
        grainLenMs.y = Mathf.Max(grainLenMs.x, grainLenMs.y);
        grainPitch.x = Mathf.Max(0.5f, Mathf.Min(grainPitch.x, grainPitch.y));
        grainPitch.y = Mathf.Max(grainPitch.x, grainPitch.y);
    }

    private void EnsureRelayOnStone()
    {
        var relay = stone.GetComponent<FrictionCollisionRelay>();
        if (!relay)
            relay = stone.GetComponentInChildren<FrictionCollisionRelay>(true);

        if (!relay)
        {
            Debug.LogWarning(
                $"[{nameof(FrictionStation)}] No FrictionCollisionRelay found on {stone.name}. Scrape will never trigger until you add one."
            );
            return;
        }

        var rc = relay.GetComponent<Collider>();
        if (!rc)
        {
            rc = relay.gameObject.AddComponent<BoxCollider>();
            rc.isTrigger = true;
            Debug.Log(
                $"[{nameof(FrictionStation)}] Added trigger BoxCollider to existing relay on {relay.gameObject.name}."
            );
        }
        else if (!rc.isTrigger)
        {
            rc.isTrigger = true;
            Debug.Log(
                $"[{nameof(FrictionStation)}] Set relay collider on {relay.gameObject.name} to isTrigger=true."
            );
        }

        if (relay.station != this)
            relay.station = this;
    }

    private void EnsureAudioSource(ref AudioSource src, string name)
    {
        if (!src)
        {
            var t = transform.Find(name);
            if (t)
                src = t.GetComponent<AudioSource>();
            if (!src)
            {
                var go = new GameObject(name);
                go.transform.SetParent(transform, false);
                src = go.AddComponent<AudioSource>();
            }
        }
    }

    private void Configure3DAudio(AudioSource a)
    {
        if (!a)
            return;
        a.playOnAwake = false;
        a.loop = false;
        a.spatialBlend = 1f;
        a.rolloffMode = AudioRolloffMode.Logarithmic;
        a.maxDistance = 8f;
        a.dopplerLevel = 0f;
    }

    private void OnEnable()
    {
        if (!stone)
            return;

        stoneTf = stone.transform;

        _onEntered = OnSelectEntered;
        _onExited = OnSelectExited;

        stone.selectEntered.AddListener(_onEntered);
        stone.selectExited.AddListener(_onExited);
    }

    private void OnDisable()
    {
        if (!stone)
            return;

        if (_onEntered != null)
            stone.selectEntered.RemoveListener(_onEntered);
        if (_onExited != null)
            stone.selectExited.RemoveListener(_onExited);
    }

    private void OnSelectEntered(SelectEnterEventArgs a)
    {
        isHeld = true;
        hand = HandFrom(a.interactorObject, HandTarget.Right);
        lastPos = stoneTf.position;
        smoothedSpeed = 0f;
        _lastIntensity = 0f;
        ResetDistanceGrains(0f);
        bedHapticTimer = 0f;
    }

    private void OnSelectExited(SelectExitEventArgs _)
    {
        isHeld = false;
        _contacts.Clear();
        inContact = false;
        StopAll();
    }

    public void HandleTriggerEnter(Collider other)
    {
        if (!isHeld)
            return;
        if (ignoreTriggerColliders && other.isTrigger)
            return;
        if (!IsInMask(other.gameObject.layer))
            return;

        if (_contacts.Add(other))
        {
            inContact = true;
            if (debugContacts)
                Debug.Log(
                    $"[FrictionStation] ENTER {other.name} (layer={LayerMask.LayerToName(other.gameObject.layer)}, trigger={other.isTrigger})"
                );
        }
    }

    public void HandleTriggerExit(Collider other)
    {
        if (ignoreTriggerColliders && other.isTrigger)
            return;
        if (!IsInMask(other.gameObject.layer))
            return;

        if (_contacts.Remove(other) && _contacts.Count == 0)
        {
            inContact = false;
            if (debugContacts)
                Debug.Log(
                    $"[FrictionStation] EXIT  {other.name} (layer={LayerMask.LayerToName(other.gameObject.layer)}, trigger={other.isTrigger})"
                );
            StopAll();
        }
    }

    private bool IsInMask(int layer) => (surfaceMask.value & (1 << layer)) != 0;

    private void Update()
    {
        if (!isHeld || !inContact)
            return;

        float dt = Mathf.Max(0.0001f, Time.deltaTime);
        float rawSpeed = (stoneTf.position - lastPos).magnitude / dt;
        lastPos = stoneTf.position;

        float lambda = 1f - Mathf.Exp(-dt / Mathf.Max(0.0001f, speedSmoothing));
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, rawSpeed, lambda);

        if (smoothedSpeed < minSpeed)
        {
            _lastIntensity = 0f;
            StopBedAudio();
            return;
        }

        float t = Mathf.Clamp01((smoothedSpeed - minSpeed) / (maxSpeed - minSpeed));
        _lastIntensity = t;

        StartBedAudioIfNeeded();
        UpdateBedHaptics(t);

        float stepDist = smoothedSpeed * dt;
        distAcc += stepDist;

        while (distAcc >= nextGrainDist)
        {
            PlayGrain(t);
            distAcc -= nextGrainDist;
            nextGrainDist = MetersToNextGrain(t);
        }
    }

    #region Bed Audio

    private void StartBedAudioIfNeeded()
    {
        if (bedCo != null)
            return;
        if (scrapeConfig == null || scrapeConfig.sfx == null)
            return;

        bedCo = StartCoroutine(BedLoop(scrapeConfig.sfx));
    }

    private void StopBedAudio()
    {
        if (bedCo != null)
        {
            StopCoroutine(bedCo);
            bedCo = null;
        }

        if (bedA)
            bedA.Stop();
        if (bedB)
            bedB.Stop();
    }

    private IEnumerator BedLoop(AudioClip clip)
    {
        var xfade = Mathf.Max(0.005f, bedXfadeMs / 1000f);
        var dur = Mathf.Max(0.05f, clip.length);

        bedA.clip = clip;
        bedB.clip = clip;

        bedA.loop = bedB.loop = false;
        bedA.volume = 0f;
        bedB.volume = 0f;

        bedA.time = Mathf.Clamp(Random.Range(0f, bedRandOffset), 0f, dur - 0.01f);
        bedA.pitch = BedPitch();
        bedA.Play();

        yield return null;

        AudioSource current = bedA;
        AudioSource next = bedB;

        while (inContact && isHeld)
        {
            while (current.isPlaying && inContact && isHeld)
            {
                float timeRemaining = dur - current.time;
                if (timeRemaining <= xfade)
                    break;

                current.volume = BedVolume();
                current.pitch = BedPitch();
                yield return null;
            }

            if (!inContact || !isHeld)
                break;

            next.time = Mathf.Clamp(Random.Range(0f, bedRandOffset), 0f, dur - 0.01f);
            next.pitch = BedPitch();
            next.volume = 0f;
            next.Play();

            float t = 0f;
            while (t < xfade && inContact && isHeld)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / xfade);

                next.volume = BedVolume();
                next.pitch = BedPitch();
                current.volume = BedVolume() * (1f - k);
                current.pitch = BedPitch();

                yield return null;
            }

            current.Stop();

            var tmp = current;
            current = next;
            next = tmp;
        }

        current?.Stop();
        next?.Stop();
        bedCo = null;
    }

    private float BedVolume()
    {
        if (scrapeConfig == null)
            return 0f;
        float target = Mathf.Lerp(bedMinVolume, scrapeConfig.sfxVolume, _lastIntensity);
        return Mathf.Clamp01(target);
    }

    private float BedPitch()
    {
        return Mathf.Lerp(0.90f, 1.15f, _lastIntensity);
    }

    #endregion

    #region Grains (Audio + Haptics)

    private void ResetDistanceGrains(float t)
    {
        distAcc = 0f;
        nextGrainDist = MetersToNextGrain(t);
    }

    private float MetersToNextGrain(float t)
    {
        float grainsPerMeter = Mathf.Lerp(grainPerMeterAtMin, grainPerMeterAtMax, t);
        grainsPerMeter = Mathf.Max(1f, grainsPerMeter);
        float meanDist = 1f / grainsPerMeter;

        float u = Mathf.Clamp01(Random.value);
        return -Mathf.Log(Mathf.Max(1e-4f, u)) * meanDist;
    }

    private void PlayGrain(float intensity)
    {
        if (!inContact)
            return;

        if (grainSrc && scrapeConfig != null && scrapeConfig.sfx != null)
        {
            float len = Random.Range(grainLenMs.x, grainLenMs.y) / 1000f;
            var clip = scrapeConfig.sfx;
            float maxStart = Mathf.Max(0f, clip.length - len - 0.01f);
            float start = Random.Range(0f, maxStart);

            float vol =
                Mathf.Lerp(grainVolMin, scrapeConfig.sfxVolume, intensity)
                * Random.Range(0.95f, 1.05f);
            float pitch = Random.Range(grainPitch.x, grainPitch.y);

            StartCoroutine(PlayGrainCo(start, len, vol, pitch, clip));
        }

        PlayHapticGrain(intensity);
    }

    private IEnumerator PlayGrainCo(float start, float len, float vol, float pitch, AudioClip clip)
    {
        grainSrc.clip = clip;
        grainSrc.volume = Mathf.Clamp01(vol);
        grainSrc.pitch = pitch;
        grainSrc.time = start;
        grainSrc.Play();

        float dur = Mathf.Max(0.008f, len / Mathf.Max(0.01f, pitch));
        yield return new WaitForSeconds(dur);
        grainSrc.Stop();
    }

    #endregion

    #region Haptics

    private void UpdateBedHaptics(float intensity)
    {
        bedHapticTimer -= Time.deltaTime;
        float period = Mathf.Lerp(0.08f, 0.03f, intensity);

        if (bedHapticTimer <= 0f)
        {
            PlayHapticBedPulse(intensity);
            bedHapticTimer = period;
        }
    }

    private void PlayHapticBedPulse(float intensity)
    {
        float amp = Mathf.Lerp(0.22f, 0.60f, intensity);
        float dur = Mathf.Lerp(0.020f, 0.035f, intensity);

        PlayBasic(scrapeConfig, hand, amplitudeMul: amp, durOverride: dur);
    }

    private float _hapticRefractory;

    private void PlayHapticGrain(float intensity)
    {
        if (Time.time < _hapticRefractory)
            return;

        _hapticRefractory = Time.time + 0.010f;
        float amp = Mathf.Lerp(0.25f, 0.8f, intensity) * Random.Range(0.90f, 1.10f);
        float dur = Random.Range(0.012f, 0.028f);

        PlayBasic(scrapeConfig, hand, amplitudeMul: amp, durOverride: dur);
    }

    #endregion

    private void StopAll()
    {
        StopBedAudio();

        if (grainSrc && grainSrc.isPlaying)
            grainSrc.Stop();

        scrapePlayer?.Stop();

        ResetDistanceGrains(0f);
        bedHapticTimer = 0f;
    }
}
