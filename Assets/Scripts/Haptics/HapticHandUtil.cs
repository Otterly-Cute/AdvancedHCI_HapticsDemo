using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace HapticsDemo
{
    public enum HandTarget
    {
        Left,
        Right,
        Both
    }

    public static class HapticHandUtil
    {
        public static HandTarget FromInteractor(
            IXRInteractor interactor,
            HandTarget fallback = HandTarget.Right
        )
        {
            var comp = interactor as Component;
            if (!comp)
                return fallback;

            var hand = TryResolveViaControllerNode(comp);
            if (hand.HasValue)
                return hand.Value;

            var t = comp.transform;
            while (t != null)
            {
                var n = t.name.ToLowerInvariant();
                if (n.Contains("left"))
                    return HandTarget.Left;
                if (n.Contains("right"))
                    return HandTarget.Right;
                t = t.parent;
            }

            return fallback;
        }

        static HandTarget? TryResolveViaControllerNode(Component source)
        {
            var typeABC = Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.ActionBasedController, Unity.XR.Interaction.Toolkit"
            );
            var hand = GetHandFromControllerNodeProperty(source, typeABC);
            if (hand.HasValue)
                return hand;

            var typeXRC = Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.XRController, Unity.XR.Interaction.Toolkit"
            );
            hand = GetHandFromControllerNodeProperty(source, typeXRC);
            if (hand.HasValue)
                return hand;

            return null;
        }

        static HandTarget? GetHandFromControllerNodeProperty(Component source, Type controllerType)
        {
            if (controllerType == null)
                return null;

            var ctrl = source.GetComponentInParent(controllerType) as Component;
            if (!ctrl)
                return null;

            var prop = controllerType.GetProperty("controllerNode");
            if (prop == null)
                return null;

            try
            {
                var value = prop.GetValue(ctrl, null);
                if (value is XRNode node)
                {
                    if (node == XRNode.LeftHand)
                        return HandTarget.Left;
                    if (node == XRNode.RightHand)
                        return HandTarget.Right;
                }
            }
            catch { }

            return null;
        }
    }
}
