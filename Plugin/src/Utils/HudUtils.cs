using System.Collections;
using UnityEngine;

namespace LobbyControl.Utils;

internal static class HudUtils
{
    private static readonly WaitUntil WaitForAnimation = new (() => HUDManager.Instance.tipsPanelAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1);

    internal static IEnumerator ShowMessageAfterDelay(string title, string text, float delay = 0f,
        bool isWarning = false)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        yield return WaitForAnimation;
        HUDManager.Instance.DisplayTip(title, text, isWarning);
    }

    internal static IEnumerator ShowTipAfterDelay(string title, string text, float delay, string saveKey)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        yield return WaitForAnimation;
        HUDManager.Instance.DisplayTip(title, text, false, true, saveKey);
    }
}
