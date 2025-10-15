using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Oculus.Haptics;

namespace HapticsDemo
{
    public class HammerStation : HapticStationBase, IOverridesAmplitude
    {
        [Header("Child Parts")]
        [SerializeField]
        XRGrabInteractable hammer;

        [SerializeField]
        AudioSource impactAudio;

        [Header("Config")]
        [SerializeField]
        HapticConfig impactConfig;

        [Header("Hit Filtering")]
        [SerializeField]
        LayerMask surfaceMask;

        [Header("Velocity Mapping")]
        [SerializeField, Tooltip("Velocity that gives maximum amplitude (m/s).")]
        float maxImpactSpeed = 1.2f;

        [SerializeField, Tooltip("Velocity below which hits are ignored (m/s).")]
        float minImpactSpeed = 0.05f;

        private bool isGrabbed;
        private HandTarget currentHand = HandTarget.Right;
        private HapticClipPlayer impactPlayer;

        protected override void Awake()
        {
            base.Awake();

            if (impactConfig != null && impactConfig.advancedClip != null)
            {
                impactPlayer = new HapticClipPlayer(impactConfig.advancedClip);
                Debug.Log(
                    $"[{nameof(HammerStation)}] Initialized advanced haptic clip '{impactConfig.advancedClip.name}'."
                );
            }
            else
            {
                Debug.LogWarning(
                    $"[{nameof(HammerStation)}] No advanced haptic clip configured for impact."
                );
            }
        }

        void OnEnable()
        {
            if (!hammer)
            {
                Debug.LogError($"[{nameof(HammerStation)}] No hammer reference set.");
                return;
            }

            hammer.selectEntered.AddListener(a =>
            {
                isGrabbed = true;
                currentHand = HandFrom(a.interactorObject, HandTarget.Right);
                Debug.Log($"[{nameof(HammerStation)}] Hammer grabbed with {currentHand} hand.");
            });

            hammer.selectExited.AddListener(_ =>
            {
                isGrabbed = false;
                Debug.Log($"[{nameof(HammerStation)}] Hammer released.");
            });
        }

        void OnDisable()
        {
            if (!hammer)
                return;
            hammer.selectEntered.RemoveAllListeners();
            hammer.selectExited.RemoveAllListeners();
            Debug.Log($"[{nameof(HammerStation)}] Event listeners removed.");
        }

        public void HandleImpact(float velocity, Collider other)
        {
            Debug.Log(
                $"[{nameof(HammerStation)}] HandleImpact() called | Grabbed={isGrabbed} | Velocity={velocity:F3} | Other={other.name}"
            );

            if (!isGrabbed)
            {
                Debug.Log($"[{nameof(HammerStation)}] Ignored impact — hammer not grabbed.");
                return;
            }

            int layer = other.gameObject.layer;
            bool layerMatch = (surfaceMask.value & (1 << layer)) != 0;
            Debug.Log(
                $"[{nameof(HammerStation)}] Layer check: '{LayerMask.LayerToName(layer)}' => {(layerMatch ? "VALID" : "IGNORED")}"
            );

            if (!layerMatch)
                return;

            float amplitude = Mathf.Clamp01(
                (velocity - minImpactSpeed) / (maxImpactSpeed - minImpactSpeed)
            );
            Debug.Log(
                $"[{nameof(HammerStation)}] Computed amplitude={amplitude:F3} from velocity={velocity:F3} (min={minImpactSpeed}, max={maxImpactSpeed})"
            );

            if (amplitude <= 0.01f)
            {
                Debug.Log(
                    $"[{nameof(HammerStation)}] Ignored — amplitude too low ({amplitude:F3})."
                );
                return;
            }

            if (impactAudio && impactConfig.sfx)
            {
                float volume = impactConfig.sfxVolume * amplitude;
                impactAudio.PlayOneShot(impactConfig.sfx, volume);
                Debug.Log(
                    $"[{nameof(HammerStation)}] Played impact sound. Volume={volume:F2} | Clip={impactConfig.sfx.name}"
                );
            }
            else
            {
                Debug.LogWarning(
                    $"[{nameof(HammerStation)}] Missing AudioSource or AudioClip. No sound played."
                );
            }

            if (UseAdvanced)
            {
                if (impactPlayer != null)
                {
                    impactPlayer.amplitude = amplitude;
                    impactPlayer.Play(HapticPlayer.ToMeta(currentHand));
                    Debug.Log(
                        $"[{nameof(HammerStation)}] Played advanced haptics. Hand={currentHand}, Amp={amplitude:F2}"
                    );
                }
                else
                {
                    Debug.LogWarning(
                        $"[{nameof(HammerStation)}] Advanced mode active but impactPlayer is null."
                    );
                }
            }
            else
            {
                PlayBasic(impactConfig, currentHand, amplitudeMul: amplitude);
                Debug.Log(
                    $"[{nameof(HammerStation)}] Played basic haptics. Hand={currentHand}, Amp={amplitude:F2}"
                );
            }
        }
    }
}
