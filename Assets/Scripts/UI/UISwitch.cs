using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Toggle))]
public class UISwitch : MonoBehaviour
{
    [SerializeField]
    private RectTransform handle;

    [SerializeField]
    private Vector2 handleOffPosition = new Vector2(-40f, 0f);

    [SerializeField]
    private Vector2 handleOnPosition = new Vector2(40f, 0f);

    [SerializeField, Min(0f)]
    private float moveDuration = 0.15f;

    [Header("Forward state to haptics (optional)")]
    [SerializeField]
    private HapticModeSwitcher hapticModeSwitcher;

    private Toggle toggle;
    private Coroutine moveRoutine;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.transition = Selectable.Transition.None;

        handle.anchoredPosition = toggle.isOn ? handleOnPosition : handleOffPosition;

        if (hapticModeSwitcher != null)
        {
            hapticModeSwitcher.SetBasic(toggle.isOn);
        }

        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool isOn)
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }
        moveRoutine = StartCoroutine(MoveHandle(isOn));

        if (hapticModeSwitcher != null)
        {
            hapticModeSwitcher.SetBasic(isOn);
        }
    }

    private IEnumerator MoveHandle(bool isOn)
    {
        Vector2 startPos = handle.anchoredPosition;
        Vector2 targetPos = isOn ? handleOnPosition : handleOffPosition;

        if (moveDuration <= 0f)
        {
            handle.anchoredPosition = targetPos;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            float t = elapsed / moveDuration;
            handle.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        handle.anchoredPosition = targetPos;
        moveRoutine = null;
    }
}
