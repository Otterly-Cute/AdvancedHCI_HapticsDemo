using UnityEngine;
using UnityEngine.EventSystems;
using HapticsDemo;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class UIHapticsForwarder : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public UIHapticsStation station;

    public void OnPointerEnter(PointerEventData eventData)
    {
        var hand = ResolveHand(eventData);
        if (hand != HandTarget.Both)
            station?.PlayHover(hand);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        var hand = ResolveHand(eventData);
        if (hand != HandTarget.Both)
            station?.PlayClick(hand);
    }

    private HandTarget ResolveHand(PointerEventData eventData)
    {
        if (eventData is TrackedDeviceEventData trackedData)
        {
            var interactor = trackedData.interactor as IXRInteractor;
            if (interactor != null)
            {
                return HapticHandUtil.FromInteractor(interactor, HandTarget.Right);
            }
        }

        Debug.LogWarning(
            "[UIHapticsForwarder] Could not determine which hand triggered the UI event."
        );
        return HandTarget.Both;
    }
}
