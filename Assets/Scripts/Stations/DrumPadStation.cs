using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using HapticsDemo;
using Oculus.Haptics;

public class DrumPadStation : HapticStationBase
{
    [Serializable]
    public struct Pad
    {
        public XRSimpleInteractable interactable;
        public AudioSource audio;
        public HapticConfig config;
    }

    [Header("Child Parts")]
    [SerializeField]
    private AudioSource stationAudio;

    [SerializeField]
    private Pad[] pads;

    private HapticClipPlayer[] padPlayers;

    [Header("Hit Filtering (Minimal)")]
    [Tooltip("Ignore very slow micromovements below this speed (m/s) when speed is available.")]
    [SerializeField]
    private float minActivationSpeed = 0.05f;

    [Tooltip("Cooldown after a FAST hit (sec).")]
    [SerializeField]
    private float minCooldown = 0.08f;

    [Tooltip("Cooldown after a SLOW hit (sec).")]
    [SerializeField]
    private float maxCooldown = 0.20f;

    [Tooltip("Speed considered 'slow' (m/s). Below this uses maxCooldown.")]
    [SerializeField]
    private float slowSpeed = 0.20f;

    [Tooltip("Speed considered 'fast' (m/s). At/above this uses minCooldown.")]
    [SerializeField]
    private float fastSpeed = 2.0f;

    private float[] lastHitTimes;

    protected override void Awake()
    {
        Debug.Log("[DrumPadStation] Awake() called");
        base.Awake();

        if (pads != null)
        {
            Debug.Log($"[DrumPadStation] Initializing padPlayers for {pads.Length} pads");
            padPlayers = new HapticClipPlayer[pads.Length];
            lastHitTimes = new float[pads.Length];

            for (int i = 0; i < pads.Length; i++)
            {
                lastHitTimes[i] = -999f;

                if (pads[i].config != null && pads[i].config.advancedClip != null)
                {
                    padPlayers[i] = new HapticClipPlayer(pads[i].config.advancedClip);
                    Debug.Log($"[DrumPadStation] Pad {i}: HapticClipPlayer created.");
                }
                else
                {
                    Debug.LogWarning($"[DrumPadStation] Pad {i}: missing config or advancedClip");
                }
            }
        }
        else
        {
            Debug.LogWarning("[DrumPadStation] No pads defined in Awake()");
        }
    }

    void OnValidate()
    {
        Debug.Log("[DrumPadStation] OnValidate() called");
        if (pads == null || pads.Length == 0)
        {
            var found = GetComponentsInChildren<XRSimpleInteractable>(true);
            pads = new Pad[found.Length];
            for (int i = 0; i < found.Length; i++)
            {
                pads[i].interactable = found[i];
                Debug.Log($"[DrumPadStation] Auto-assigned interactable: {found[i].name}");
            }
        }

        if (!stationAudio)
        {
            stationAudio = GetComponentInChildren<AudioSource>(true);
            Debug.Log($"[DrumPadStation] Auto-assigned stationAudio: {stationAudio?.name}");
        }

        slowSpeed = Mathf.Max(0.001f, slowSpeed);
        fastSpeed = Mathf.Max(slowSpeed + 0.001f, fastSpeed);
        minCooldown = Mathf.Max(0f, minCooldown);
        maxCooldown = Mathf.Max(minCooldown, maxCooldown);
        minActivationSpeed = Mathf.Max(0f, minActivationSpeed);
    }

    void OnEnable()
    {
        Debug.Log("[DrumPadStation] OnEnable() called");

        if (pads == null || pads.Length == 0)
        {
            Debug.LogWarning("[DrumPadStation] Pads array empty on enable!");
            return;
        }

        foreach (var p in pads)
        {
            if (p.interactable)
            {
                Debug.Log($"[DrumPadStation] Adding listeners to pad: {p.interactable.name}");
                p.interactable.hoverEntered.AddListener(OnPadTriggered);
                p.interactable.selectEntered.AddListener(OnPadTriggered);
            }
            else
            {
                Debug.LogWarning("[DrumPadStation] Found null interactable in pads array");
            }
        }
    }

    void OnDisable()
    {
        Debug.Log("[DrumPadStation] OnDisable() called");

        if (pads == null)
            return;

        foreach (var p in pads)
        {
            if (p.interactable)
            {
                p.interactable.hoverEntered.RemoveListener(OnPadTriggered);
                p.interactable.selectEntered.RemoveListener(OnPadTriggered);
            }
        }
    }

    void OnPadTriggered(BaseInteractionEventArgs e)
    {
        Debug.Log($"[DrumPadStation] OnPadTriggered() fired by interactor {e.interactorObject}");

        if (!(e.interactorObject is XRDirectInteractor || e.interactorObject is XRPokeInteractor))
        {
            Debug.Log(
                $"[DrumPadStation] Ignored: interactor type = {e.interactorObject.GetType().Name}"
            );
            return;
        }

        var hitComp = e.interactableObject as Component;
        if (!hitComp)
        {
            Debug.LogWarning("[DrumPadStation] interactableObject not a valid Component");
            return;
        }

        int idx = Array.FindIndex(
            pads,
            p => p.interactable && p.interactable.transform == hitComp.transform
        );
        Debug.Log($"[DrumPadStation] Pad index found: {idx}");

        if (idx < 0)
        {
            Debug.LogWarning("[DrumPadStation] Could not find matching pad for interaction");
            return;
        }

        float speed = 0f;
        bool hasSpeed = false;
        var interactorComp = e.interactorObject as Component;
        if (interactorComp != null)
        {
            var rb = interactorComp.GetComponentInParent<Rigidbody>();
            if (rb != null)
            {
                speed = rb.linearVelocity.magnitude;
                hasSpeed = true;
            }
        }

        if (hasSpeed && speed < minActivationSpeed)
        {
            Debug.Log(
                $"[DrumPadStation] Ignored micromovement (speed={speed:F3} < {minActivationSpeed:F3} m/s)"
            );
            return;
        }

        float cooldown;
        if (hasSpeed)
        {
            float t = Mathf.InverseLerp(slowSpeed, fastSpeed, speed);
            cooldown = Mathf.Lerp(maxCooldown, minCooldown, t);
        }
        else
        {
            cooldown = minCooldown;
        }

        if (lastHitTimes == null || lastHitTimes.Length != pads.Length)
        {
            lastHitTimes = new float[pads.Length];
            for (int i = 0; i < lastHitTimes.Length; i++)
                lastHitTimes[i] = -999f;
        }

        float now = Time.time;
        float elapsed = now - lastHitTimes[idx];
        if (elapsed < cooldown)
        {
            Debug.Log(
                $"[DrumPadStation] Debounced pad {idx}: elapsed={elapsed:F3}s < cooldown={cooldown:F3}s (speed={(hasSpeed ? speed : -1f):F3})"
            );
            return;
        }
        lastHitTimes[idx] = now;

        var pad = pads[idx];
        var hand = HandFrom(e.interactorObject, HandTarget.Right);
        var srcAudio = pad.audio ? pad.audio : stationAudio;

        Debug.Log($"[DrumPadStation] Playing one shot on pad {idx} | Audio: {srcAudio?.name}");

        PlayOneShot(srcAudio, pad.config);

        if (UseAdvanced)
        {
            if (padPlayers != null && idx < padPlayers.Length && padPlayers[idx] != null)
            {
                var player = padPlayers[idx];
                player.amplitude = pad.config.advancedAmplitude;
                player.Play(HapticPlayer.ToMeta(hand));
                Debug.Log($"[DrumPadStation] Played advanced haptic clip for pad {idx}");
            }
            else
            {
                Debug.LogWarning($"[DrumPadStation] Missing HapticClipPlayer for pad {idx}");
            }
        }
        else
        {
            Debug.Log($"[DrumPadStation] Playing basic haptics for pad {idx}");
            PlayBasic(pad.config, hand, 1f, 0.04f);
        }
    }
}
