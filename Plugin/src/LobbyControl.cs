using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using LobbyControl.Dependency;
using LobbyControl.Patches;
using LobbyControl.PopUp;
using LobbyControl.TerminalCommands;
using MonoMod.RuntimeDetour;
using PluginInfo = BepInEx.PluginInfo;

namespace LobbyControl;

[BepInPlugin(GUID, NAME, VERSION)]
//soft-deps
[BepInDependency("FlipMods.ReservedItemSlotCore", Flags: BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("imabatby.lethallevelloader", Flags: BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("BMX.LobbyCompatibility", Flags: BepInDependency.DependencyFlags.SoftDependency)]
//incompatibilities
[BepInDependency("com.github.tinyhoot.ShipLobby", Flags: BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("twig.latecompany", Flags: BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("McBowie.VeryLateCompany", Flags: BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("com.potatoepet.AdvancedCompany", Flags: BepInDependency.DependencyFlags.SoftDependency)]
internal class LobbyControl : BaseUnityPlugin
{
    public const string GUID = MyPluginInfo.PLUGIN_GUID;
    public const string NAME = MyPluginInfo.PLUGIN_NAME;
    public const string VERSION = MyPluginInfo.PLUGIN_VERSION;

    public static LobbyControl Instance;

    internal static ManualLogSource Log;

    internal static Harmony Harmony  { get; private set; }
    internal static HashSet<Hook> Hooks { get; } = [];

    public static bool CanModifyLobby = true;

    public static bool CanSave = true;
    public static bool AutoSaveEnabled = true;

    private static readonly string[] IncompatibleGUIDs =
    [
        "com.github.tinyhoot.ShipLobby",
        "twig.latecompany",
        "McBowie.VeryLateCompany",
        "com.potatoepet.AdvancedCompany"
    ];

    // ReSharper disable once CollectionNeverQueried.Global
    internal static readonly List<PluginInfo> FoundIncompatibilities = [];

    private void Awake()
    {
        Log = Logger;
        Instance = this;
        try
        {
            Harmony = new Harmony(GUID);

            PluginInfo[] incompatibleMods = Chainloader.PluginInfos.Values
                .Where(p => IncompatibleGUIDs.Contains(p.Metadata.GUID)).ToArray();
            if (incompatibleMods.Length > 0)
            {
                StringBuilder sb = new StringBuilder("LOBBY CONTROL was DISABLED!\nIncompatible:");
                FoundIncompatibilities.AddRange(incompatibleMods);
                foreach (var mod in incompatibleMods)
                {
                    Log.LogWarning($"{mod.Metadata.Name} is incompatible!");
                    sb.Append("\n").Append(mod.Metadata.Name);
                }

                Log.LogError($"{incompatibleMods.Length} incompatible mods found! Disabling!");
                PopUpPatch.PopUps.Add(("LC_Incompatibility", sb.ToString()));
                Harmony.PatchAll(typeof(PopUpPatch));
            }
            else
            {
                if (LobbyCompatibilityProxy.Enabled)
                    LobbyCompatibilityProxy.Init(GUID, Version.Parse(VERSION), 1, 2);
                Log.LogInfo("Initializing Configs");

                PluginConfig.Init(this);

                CommandManager.Initialize();

                LobbyCommand.Init();

                Log.LogInfo("Patching Methods");

                Harmony.PatchAll(typeof(PopUpPatch));

                Harmony.PatchAll(typeof(FacepunchTransportPatches));
                Harmony.PatchAll(typeof(JoinQueuePatches));
                JoinQueuePatches.Init();
                Harmony.PatchAll(typeof(LateJoinPatches));
                Harmony.PatchAll(typeof(LimitPatcher));
                Harmony.PatchAll(typeof(LobbyPatcher));
                Harmony.PatchAll(typeof(LogSpamPatches));
                Harmony.PatchAll(typeof(NetworkManagerPatch));
                Harmony.PatchAll(typeof(SavePatches));
                Harmony.PatchAll(typeof(TerminalPatch));

                Log.LogInfo(NAME + " v" + VERSION + " Loaded!");
            }
        }
        catch (Exception ex)
        {
            Log.LogError("Exception while initializing: \n" + ex);
        }
    }
}
