using UnityEngine;
using HapticsDemo;
using Oculus.Haptics;

public class UIHapticsStation : HapticStationBase
{
    [Header("Audio Output")]
    [SerializeField]
    AudioSource audioSource;

    [Header("Configs")]
    [SerializeField]
    HapticConfig hoverConfig;

    [SerializeField]
    HapticConfig clickConfig;

    private HapticClipPlayer hoverPlayer;
    private HapticClipPlayer clickPlayer;

    protected override void Awake()
    {
        base.Awake();
        if (hoverConfig != null && hoverConfig.advancedClip != null)
        {
            hoverPlayer = new HapticClipPlayer(hoverConfig.advancedClip);
        }
        if (clickConfig != null && clickConfig.advancedClip != null)
        {
            clickPlayer = new HapticClipPlayer(clickConfig.advancedClip);
        }
    }

    public void PlayHover(HandTarget hand) => Play(hoverConfig, hand);

    public void PlayClick(HandTarget hand) => Play(clickConfig, hand);

    void Play(HapticConfig cfg, HandTarget hand)
    {
        if (cfg == null)
            return;

        if (cfg.sfx != null)
        {
            PlayOneShot(audioSource, cfg);
        }

        if (UseAdvanced)
        {
            HapticClipPlayer playerToUse = null;
            if (cfg == hoverConfig)
                playerToUse = hoverPlayer;
            else if (cfg == clickConfig)
                playerToUse = clickPlayer;

            if (playerToUse != null)
            {
                playerToUse.amplitude = cfg.advancedAmplitude;
                playerToUse.Play(HapticPlayer.ToMeta(hand));
            }
        }
        else
        {
            PlayBasic(cfg, hand);
        }
    }
}
