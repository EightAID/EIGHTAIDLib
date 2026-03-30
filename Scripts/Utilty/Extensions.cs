using System.Collections.Generic;
using UnityEngine;

namespace EightAID.EIGHTAIDLib.Utility
{
    public static class Extensions
    {
        public static Color WithAlpha(this Color color, float alpha)
            => new Color(color.r, color.g, color.b, alpha);

        public static float Remap(this float value, float inMin, float inMax, float outMin, float outMax)
            => Mathf.Lerp(outMin, outMax, Mathf.InverseLerp(inMin, inMax, value));

        public static void DestroyChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        public static T GetRandom<T>(this IList<T> list)
            => list.Count == 0 ? default : list[Random.Range(0, list.Count)];

        public static void SetVisible(this CanvasGroup group, bool visible)
        {
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
