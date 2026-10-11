using BepInEx;
using BepInEx.Bootstrap;

namespace LobbyControl.Dependency;

internal static class LethalLevelLoaderProxy
{
    private static bool? _enabled;
    private static PluginInfo _pluginInfo;

    public static bool Enabled
    {
        get
        {
            _enabled ??= Chainloader.PluginInfos.TryGetValue("imabatby.lethallevelloader", out _pluginInfo);
            return _enabled.Value;
        }
    }
}
