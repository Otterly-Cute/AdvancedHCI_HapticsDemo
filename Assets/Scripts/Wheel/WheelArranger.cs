using UnityEngine;

[ExecuteInEditMode]
public class WheelArranger : MonoBehaviour
{
    [Header("Arrangement Settings")]
    [Min(0.1f)]
    public float radius = 3f;

    [Tooltip("If true, podiums will face the center of the circle.")]
    public bool faceCenter = true;

    public void ArrangeInCircle()
    {
        int count = transform.childCount;
        if (count == 0)
        {
            Debug.LogWarning($"[{name}] No child objects found to arrange.");
            return;
        }

        float startAngle = -Mathf.PI / 2f;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + (i * Mathf.PI * 2f / count);
            Vector3 pos = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

            Transform child = transform.GetChild(i);
            child.localPosition = pos;

            if (faceCenter)
                child.LookAt(transform.position);
        }

        Debug.Log(
            $"[{name}] Arranged {count} podiums in a circle (radius: {radius}), starting from north (+Z)."
        );
    }
}
