using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using Oculus.Haptics;

namespace HapticsDemo
{
    public class HapticPlayer : MonoBehaviour
    {
        [Header("Assign these (usually on XR Origin)")]
        public HapticImpulsePlayer leftBasic;
        public HapticImpulsePlayer rightBasic;

        public void PulseBasic(
            HapticConfig cfg,
            HandTarget hand,
            float amplitudeMultiplier = 1f,
            float? durationOverride = null,
            float? freqOverride = null
        )
        {
            if (cfg == null)
            {
                Debug.LogWarning("[HapticPlayer] PulseBasic called with null HapticConfig.");
                return;
            }

            var amp = Mathf.Clamp01(cfg.basicAmplitude * amplitudeMultiplier);
            var dur = durationOverride ?? (cfg.sfx ? cfg.sfx.length : 0f);

            Debug.Log(
                $"[HapticPlayer] PulseBasic called | Hand: {hand} | "
                    + $"Amplitude: {amp:F2} | Duration: {dur:F2}s"
            );

            if (hand == HandTarget.Left || hand == HandTarget.Both)
            {
                if (leftBasic != null)
                {
                    Debug.Log("[HapticPlayer] Sending LEFT haptic impulse.");
                    leftBasic.SendHapticImpulse(amp, dur);
                }
                else
                {
                    Debug.LogWarning("[HapticPlayer] Left HapticImpulsePlayer not assigned.");
                }
            }

            if (hand == HandTarget.Right || hand == HandTarget.Both)
            {
                if (rightBasic != null)
                {
                    Debug.Log("[HapticPlayer] Sending RIGHT haptic impulse.");
                    rightBasic.SendHapticImpulse(amp, dur);
                }
                else
                {
                    Debug.LogWarning("[HapticPlayer] Right HapticImpulsePlayer not assigned.");
                }
            }
        }

        public HapticSource BuildAdvancedSource(Transform attachTo)
        {
            if (attachTo == null)
            {
                Debug.LogError("[HapticPlayer] BuildAdvancedSource called with null Transform!");
                return null;
            }

            Debug.Log($"[HapticPlayer] Building advanced haptic source on '{attachTo.name}'.");
            var source = attachTo.gameObject.AddComponent<HapticSource>();
            Debug.Log($"[HapticPlayer] HapticSource added successfully to '{attachTo.name}'.");
            return source;
        }

        public static Controller ToMeta(HandTarget hand)
        {
            var metaController =
                hand == HandTarget.Left
                    ? Controller.Left
                    : hand == HandTarget.Right
                        ? Controller.Right
                        : Controller.Both;

            Debug.Log(
                $"[HapticPlayer] Converted HandTarget '{hand}' to Meta Controller '{metaController}'."
            );
            return metaController;
        }
    }
}
