using UnityEngine;

public enum HapticMode
{
    Basic,
    Advanced
}

public class HapticModeSwitcher : MonoBehaviour
{
    [SerializeField]
    HapticMode mode = HapticMode.Basic;
    public HapticMode Mode
    {
        get => mode;
        set
        {
            mode = value;
            BroadcastMessage("OnHapticModeChanged", mode, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void SetBasic(bool isOn) => Mode = isOn ? HapticMode.Advanced : HapticMode.Basic;
}
