using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    // ─── Color ────────────────────────────────────────────

    /// <summary>alpha 値だけ変えた Color を返す</summary>
    public static Color WithAlpha(this Color color, float alpha)
        => new Color(color.r, color.g, color.b, alpha);

    // ─── float ────────────────────────────────────────────

    /// <summary>[inMin, inMax] の値を [outMin, outMax] へ線形変換する</summary>
    public static float Remap(this float value, float inMin, float inMax, float outMin, float outMax)
        => Mathf.Lerp(outMin, outMax, Mathf.InverseLerp(inMin, inMax, value));

    // ─── Transform ────────────────────────────────────────

    /// <summary>直接の子を全て Destroy する</summary>
    public static void DestroyChildren(this Transform transform)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Object.Destroy(transform.GetChild(i).gameObject);
    }

    // ─── IList<T> ─────────────────────────────────────────

    /// <summary>リストからランダムに 1 つ取得する。空のとき default を返す</summary>
    public static T GetRandom<T>(this IList<T> list)
        => list.Count == 0 ? default : list[Random.Range(0, list.Count)];

    // ─── CanvasGroup ──────────────────────────────────────

    /// <summary>alpha / interactable / blocksRaycasts をまとめて切り替える</summary>
    public static void SetVisible(this CanvasGroup group, bool visible)
    {
        group.alpha            = visible ? 1f : 0f;
        group.interactable     = visible;
        group.blocksRaycasts   = visible;
    }
}
