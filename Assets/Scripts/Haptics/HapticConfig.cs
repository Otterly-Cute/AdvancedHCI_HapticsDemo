using UnityEngine;

namespace HapticsDemo
{
    [System.Serializable]
    public class HapticConfig
    {
        public AudioClip sfx;

        [Range(0, 1)]
        public float sfxVolume = 1f;

        [Range(0, 1)]
        public float basicAmplitude = 1f;

        public Oculus.Haptics.HapticClip advancedClip;

        [Range(0, 1)]
        public float advancedAmplitude = 1f;
    }
}
