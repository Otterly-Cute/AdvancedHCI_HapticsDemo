using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Oculus.Haptics;

namespace HapticsDemo
{
    public abstract class HapticStationBase : MonoBehaviour
    {
        [Header("Shared")]
        [SerializeField]
        protected HapticPlayer hapticPlayer;

        [SerializeField]
        protected HapticModeSwitcher modeSwitcher;

        protected virtual void Awake()
        {
            if (!modeSwitcher)
                modeSwitcher = GetComponentInParent<HapticModeSwitcher>();
            if (!hapticPlayer)
                hapticPlayer = FindFirstObjectByType<HapticPlayer>();
        }

        protected bool UseAdvanced => modeSwitcher && modeSwitcher.Mode == HapticMode.Advanced;

        protected static HandTarget HandFrom(
            IXRInteractor interactor,
            HandTarget fallback = HandTarget.Right
        ) => HapticHandUtil.FromInteractor(interactor, fallback);

        protected void PlayBasic(
            HapticConfig cfg,
            HandTarget hand,
            float amplitudeMul = 1f,
            float? durOverride = null,
            float? freqOverride = null
        )
        {
            if (hapticPlayer == null)
                return;
            hapticPlayer.PulseBasic(cfg, hand, amplitudeMul, durOverride, freqOverride);
        }

        protected static void PlayOneShot(AudioSource asrc, HapticConfig cfg)
        {
            if (!cfg.sfx)
            {
                Debug.LogError(
                    $"[{nameof(HapticStationBase)}] Missing AudioClip on HapticConfig. Assign an AudioClip in the inspector."
                );
                return;
            }
            if (asrc)
                asrc.PlayOneShot(cfg.sfx, cfg.sfxVolume);
        }
    }
}
