using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LobbyControl.PopUp;

[HarmonyPatch]
internal class PopUpPatch
{
    internal static readonly List<(string objectName, string text)> PopUps = [];

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Awake))]
    private static void AddPopups(MenuManager __instance)
    {
        foreach (var (objectName, text) in PopUps)
        {
            AppendPopup(objectName, text);
        }
    }


    private static void AppendPopup(string objectName, string text)
    {
        var menuContainer = GameObject.Find("/Canvas/MenuContainer/");
        var lanPopup = GameObject.Find("Canvas/MenuContainer/LANWarning/");
        if (lanPopup == null)
            return;

        var newPopup = Object.Instantiate(lanPopup, menuContainer.transform);
        newPopup.name = objectName;
        newPopup.SetActive(true);
        var textHolder = newPopup.transform.Find("Panel/NotificationText");
        var textMesh = textHolder.GetComponent<TextMeshProUGUI>();
        textMesh.text = text;
    }
}
