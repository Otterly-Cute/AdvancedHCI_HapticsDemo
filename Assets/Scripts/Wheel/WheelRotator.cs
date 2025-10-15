using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;

public class WheelRotator : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionProperty rotateLeft;
    public InputActionProperty rotateRight;

    [Header("Rotation Settings")]
    [SerializeField]
    private float rotationDuration = 0.6f;

    [SerializeField]
    private Ease rotationEase = Ease.InOutSine;

    [Header("Additional Objects To Rotate In Sync")]
    [Tooltip(
        "Extra GameObjects that should rotate along with the wheel (e.g. ground, decorations)."
    )]
    public GameObject[] additionalObjects;

    private int currentIndex = 0;
    private int totalChildren;
    private float rotationStep;
    private bool isRotating = false;

    private void Start()
    {
        totalChildren = transform.childCount;
        if (totalChildren == 0)
        {
            Debug.LogWarning("No child podiums found on WheelRotator.");
            return;
        }

        rotationStep = 360f / totalChildren;

        rotateLeft.action.Enable();
        rotateRight.action.Enable();
    }

    private void Update()
    {
        if (isRotating)
            return;

        if (rotateLeft.action.WasPressedThisFrame())
        {
            currentIndex--;
            RotateToIndex();
        }
        else if (rotateRight.action.WasPressedThisFrame())
        {
            currentIndex++;
            RotateToIndex();
        }
    }

    public void RotateLeft()
    {
        if (isRotating)
            return;

        currentIndex--;
        RotateToIndex();
    }

    public void RotateRight()
    {
        if (isRotating)
            return;

        currentIndex++;
        RotateToIndex();
    }

    private void RotateToIndex()
    {
        isRotating = true;
        float targetY = currentIndex * rotationStep;
        Quaternion targetRotation = Quaternion.Euler(0, targetY, 0);

        transform
            .DORotateQuaternion(targetRotation, rotationDuration)
            .SetEase(rotationEase)
            .OnComplete(() => isRotating = false);

        foreach (GameObject obj in additionalObjects)
        {
            if (obj != null)
            {
                obj.transform
                    .DORotateQuaternion(targetRotation, rotationDuration)
                    .SetEase(rotationEase);
            }
        }
    }

    private void OnDisable()
    {
        rotateLeft.action.Disable();
        rotateRight.action.Disable();
    }
}
