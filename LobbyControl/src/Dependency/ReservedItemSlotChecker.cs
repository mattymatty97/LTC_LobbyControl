using GameNetcodeStuff;
using System.Runtime.CompilerServices;
using ReservedItemSlotCore.Data;

namespace LobbyControl.Dependency
{
    public static class ReservedItemSlotChecker
    {
        public static bool Enabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("FlipMods.ReservedItemSlotCore"); } }
        
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool CheckIfReservedItemSlot(PlayerControllerB playerController, int itemSlotIndex)
        {
            return ReservedPlayerData.allPlayerData.TryGetValue(playerController, out var playerData) && playerData.IsReservedItemSlot(itemSlotIndex);
        }        
        
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool HasEmptySlotForReservedItem(PlayerControllerB playerController, GrabbableObject grabbableObject)
        {
            return ReservedPlayerData.allPlayerData.TryGetValue(playerController, out var playerData) && 
                   playerData.GetFirstEmptySlotForReservedItem(grabbableObject.itemProperties.itemName) != null;
        }
    }
}