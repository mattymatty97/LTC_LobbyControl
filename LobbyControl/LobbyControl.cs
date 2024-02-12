using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalAPI.TerminalCommands.Models;

namespace LobbyControl
{
    [BepInPlugin(GUID, NAME, VERSION)]
    internal class LobbyControl : BaseUnityPlugin
    {
        public const string GUID = "com.github.mattymatty.LobbyControl";
        public const string NAME = "LobbyControl";
        public const string VERSION = "2.0.0";
        
        internal static ManualLogSource Log;

        public static bool CanModifyLobby = true;

        public static bool CanSave = true;
        
        private TerminalModRegistry _commands;

        private void Awake()
        {
            Log = Logger;

            Harmony harmony = new Harmony(GUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            _commands = TerminalRegistry.RegisterFrom(new LobbyCommands());
        }
    }
}