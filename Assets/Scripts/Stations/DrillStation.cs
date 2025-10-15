using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using HapticsDemo;
using Oculus.Haptics;

public class DrillStation : HapticStationBase
{
    [Header("Child Parts")]
    [SerializeField]
    XRGrabInteractable drill;

    [SerializeField]
    AudioSource motorAudio;

    [Header("Config")]
    [SerializeField]
    HapticConfig drillConfig;

    private HapticClipPlayer drillPlayer;

    private bool isDrilling;
    private float hapticTimer;
    private const float hapticInterval = 0.3f;

    protected override void Awake()
    {
        base.Awake();
        if (drillConfig != null && drillConfig.advancedClip != null)
        {
            drillPlayer = new HapticClipPlayer(drillConfig.advancedClip);
        }
    }

    void OnValidate()
    {
        if (!drill)
            drill = GetComponentInChildren<XRGrabInteractable>(true);
        if (!motorAudio)
            motorAudio = GetComponentInChildren<AudioSource>(true);
    }

    void OnEnable()
    {
        if (!drill)
            return;
        drill.activated.AddListener(OnDrillActivated);
        drill.deactivated.AddListener(OnDrillDeactivated);
        drill.selectExited.AddListener(OnUngrab);
    }

    void OnDisable()
    {
        if (!drill)
            return;
        drill.activated.RemoveListener(OnDrillActivated);
        drill.deactivated.RemoveListener(OnDrillDeactivated);
        drill.selectExited.RemoveListener(OnUngrab);
    }

    void OnDrillActivated(ActivateEventArgs args)
    {
        var hand = HandFrom(args.interactorObject, HandTarget.Right);
        PlayOneShot(motorAudio, drillConfig);

        isDrilling = true;
        hapticTimer = 0f;

        if (UseAdvanced)
        {
            if (drillPlayer != null)
            {
                drillPlayer.amplitude = drillConfig.advancedAmplitude;
                drillPlayer.Play(HapticPlayer.ToMeta(hand));
            }
        }
        else
        {
            PlayBasic(drillConfig, hand);
        }
    }

    void OnDrillDeactivated(DeactivateEventArgs args)
    {
        var hand = HandFrom(args.interactorObject, HandTarget.Right);

        if (motorAudio && motorAudio.isPlaying)
        {
            motorAudio.Stop();
        }

        isDrilling = false;

        if (UseAdvanced)
        {
            if (drillPlayer != null)
                drillPlayer.Stop();
        }
        else
        {
            if (hapticPlayer != null)
            {
                if (hand == HandTarget.Left || hand == HandTarget.Both)
                    hapticPlayer.leftBasic?.SendHapticImpulse(0f, 0f);
                if (hand == HandTarget.Right || hand == HandTarget.Both)
                    hapticPlayer.rightBasic?.SendHapticImpulse(0f, 0f);
            }
        }
    }

    void OnUngrab(SelectExitEventArgs args)
    {
        if (motorAudio && motorAudio.isPlaying)
        {
            motorAudio.Stop();
        }

        isDrilling = false;

        if (hapticPlayer != null)
        {
            hapticPlayer.leftBasic?.SendHapticImpulse(0f, 0f);
            hapticPlayer.rightBasic?.SendHapticImpulse(0f, 0f);
        }

        if (drillPlayer != null)
        {
            drillPlayer.Stop();
        }
    }

    void Update()
    {
        if (!isDrilling || UseAdvanced || !motorAudio)
            return;

        if (!motorAudio.isPlaying)
        {
            isDrilling = false;
            return;
        }

        hapticTimer -= Time.deltaTime;
        if (hapticTimer <= 0f)
        {
            hapticTimer = hapticInterval;
            PlayBasic(drillConfig, HandTarget.Right);
        }
    }
}
