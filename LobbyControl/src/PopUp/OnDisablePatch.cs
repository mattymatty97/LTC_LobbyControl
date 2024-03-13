using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace LobbyControl.PopUp
{
    [HarmonyPatch]
    internal class OnDisablePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Awake))]
        private static void AddPopup(MenuManager __instance)
        {
            if (LobbyControl.FoundIncompatibilities.Count <= 0) 
                return;
            
            var menuContainer = GameObject.Find("/Canvas/MenuContainer/");
            var lanPopup = GameObject.Find("Canvas/MenuContainer/LANWarning/");
            if (lanPopup == null) 
                return;
            
            LobbyControl.Log.LogWarning("Cloning!");
            var newPopup = UnityEngine.Object.Instantiate(lanPopup, menuContainer.transform);
            newPopup.name = "LC_Incompatibility";
            newPopup.SetActive(true);
            LobbyControl.Log.LogWarning("Finding text!");
            var textHolder = GameObject.Find("Canvas/MenuContainer/LC_Incompatibility/Panel/NotificationText");
            LobbyControl.Log.LogWarning("Finding TextMeshPro!");
            var text = textHolder.GetComponent<TextMeshProUGUI>();
            LobbyControl.Log.LogWarning("Changing text!");
            StringBuilder sb = new StringBuilder("LOBBY CONTROL was DISABLED!\nIncompatible:");
            foreach (var plugin in LobbyControl.FoundIncompatibilities)
            {
                sb.Append("\n").Append(plugin.Metadata.Name);
            }
            text.text = sb.ToString();
        }
    }
}