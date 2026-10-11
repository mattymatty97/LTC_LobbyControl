using System;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using LobbyCompatibility.Enums;
using LobbyCompatibility.Features;

namespace LobbyControl.Dependency;

internal static class LobbyCompatibilityProxy
{
    public static bool Enabled
    {
        get { return Chainloader.PluginInfos.ContainsKey("BMX.LobbyCompatibility"); }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void Init(string GUID, Version version, int Level, int strictness)
    {
        PluginHelper.RegisterPlugin(GUID, version, (CompatibilityLevel)Level, (VersionStrictness)strictness);
    }
}
