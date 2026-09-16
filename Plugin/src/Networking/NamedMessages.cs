using System.Collections.Generic;
using LobbyControl.Patches;
using Steamworks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace LobbyControl.Networking;

internal static class NamedMessages
{
    private static readonly string BaseName = typeof(NamedMessages).FullName;
    private static readonly string ReorderRadarClientRpcMessage = $"{BaseName}|ReorderRadarClientRpc";
    private static readonly string ResetPlayerValuesClientRpcMessage = $"{BaseName}|ResetPlayerValuesClientRpc";
    private static readonly string LobbyStatusClientRpcMessage = $"{BaseName}|LobbyStatusClientRpc";

    internal static void RegisterNamedMessages()
    {
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(ReorderRadarClientRpcMessage,
            OnReorderRadarClientRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(ResetPlayerValuesClientRpcMessage,
            OnResetPlayerValuesClientRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(LobbyStatusClientRpcMessage,
            OnLobbyStatusClientRpc);
    }

    internal static void UnregisterNamedMessages()
    {
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(ReorderRadarClientRpcMessage);
        NetworkManager.Singleton.CustomMessagingManager
            .UnregisterNamedMessageHandler(ResetPlayerValuesClientRpcMessage);
        NetworkManager.Singleton.CustomMessagingManager
            .UnregisterNamedMessageHandler(LobbyStatusClientRpcMessage);
    }

    internal static void ReorderRadarClientRpc(IReadOnlyList<ulong> targets = null)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (!PluginConfig.Networking.Enabled.Value ||
            !PluginConfig.Networking.SyncRadarNames.Value)
            return;

        var buffer = new FastBufferWriter(0, Allocator.Temp);

        if (targets == null)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(ReorderRadarClientRpcMessage, buffer);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(ReorderRadarClientRpcMessage, targets,
                buffer);
    }

    private static void OnReorderRadarClientRpc(ulong senderId, FastBufferReader data)
    {
        if (senderId != NetworkManager.ServerClientId)
            return;

        if (!PluginConfig.Networking.Enabled.Value ||
            !PluginConfig.Networking.SyncRadarNames.Value)
            return;

        if (!StartOfRound.Instance || !StartOfRound.Instance.localPlayerController || !StartOfRound.Instance.mapScreen)
        {
            LobbyControl.Log.LogError($"Received {nameof(ReorderRadarClientRpc)} while not connected to a lobby!");
            return;
        }

        StartOfRound.Instance.mapScreen.SyncOrderOfRadarBoostersInList();
    }

    internal static void ResetPlayerValuesClientRpc(int playerIndex, IReadOnlyList<ulong> targets = null)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (!PluginConfig.Networking.Enabled.Value ||
            !PluginConfig.Networking.ResetPlayerValues.Value)
            return;

        var buffer = new FastBufferWriter(sizeof(int), Allocator.Temp);
        buffer.WriteValue(playerIndex);

        if (targets == null)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(ResetPlayerValuesClientRpcMessage, buffer);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(ResetPlayerValuesClientRpcMessage, targets,
                buffer);
    }

    private static void OnResetPlayerValuesClientRpc(ulong senderId, FastBufferReader data)
    {
        if (senderId != NetworkManager.ServerClientId)
            return;

        if (!PluginConfig.Networking.Enabled.Value ||
            !PluginConfig.Networking.ResetPlayerValues.Value)
            return;

        if (!GameNetworkManager.Instance || !GameNetworkManager.Instance.localPlayerController)
        {
            LobbyControl.Log.LogError($"Received {nameof(ResetPlayerValuesClientRpc)} while not connected to a lobby!");
            return;
        }

        data.ReadValue(out int playerIndex);

        var startOfRound = StartOfRound.Instance;
        var playerScript = startOfRound.allPlayerScripts[playerIndex];
        var playerObject = startOfRound.allPlayerObjects[playerIndex];

        //do not update our own data
        if (playerScript == GameNetworkManager.Instance.localPlayerController)
            return;

        playerScript.ResetPlayerBloodObjects(playerScript.isPlayerDead);

        playerScript.isClimbingLadder = false;
        playerScript.clampLooking = false;
        playerScript.inVehicleAnimation = false;
        playerScript.disableMoveInput = false;
        playerScript.ResetZAndXRotation();
        playerScript.thisController.enabled = true;
        playerScript.health = 100;
        playerScript.hasBeenCriticallyInjured = false;
        playerScript.disableLookInput = false;
        playerScript.disableInteract = false;
        Debug.Log("Reviving players B");

        playerScript.isPlayerDead = false;

        playerScript.overrideGameOverSpectatePivot = null;
        startOfRound.SetPlayerObjectExtrapolate(enable: false);
        playerScript.setPositionOfDeadPlayer = false;
        playerScript.DisablePlayerModel(playerObject, enable: true, disableLocalArms: true);

        playerScript.helmetLight.enabled = false;

        playerScript.Crouch(crouch: false);

        playerScript.criticallyInjured = false;

        if (playerScript.playerBodyAnimator != null)
        {
            playerScript.playerBodyAnimator.SetBool("Limp", value: false);
        }

        playerScript.bleedingHeavily = false;
        playerScript.activatingItem = false;

        playerScript.twoHanded = false;

        playerScript.inShockingMinigame = false;
        playerScript.inSpecialInteractAnimation = false;
        playerScript.freeRotationInInteractAnimation = false;
        playerScript.disableSyncInAnimation = false;
        playerScript.inAnimationWithEnemy = null;

        playerScript.holdingWalkieTalkie = false;
        playerScript.speakingToWalkieTalkie = false;

        playerScript.isSinking = false;
        playerScript.isUnderwater = false;
        playerScript.sinkingValue = 0f;
        playerScript.statusEffectAudio.Stop();

        playerScript.DisableJetpackControlsLocally();

        playerScript.health = 100;

        playerScript.mapRadarDotAnimator.SetBool("dead", value: false);
        playerScript.externalForceAutoFade = Vector3.zero;

        playerScript.voiceMuffledByEnemy = false;
        SoundManager.Instance.playerVoicePitchTargets[playerIndex] = 1f;
        SoundManager.Instance.SetPlayerPitch(1f, playerIndex);

        if (playerScript.currentVoiceChatIngameSettings == null)
        {
            startOfRound.RefreshPlayerVoicePlaybackObjects();
        }

        if (playerScript.currentVoiceChatIngameSettings != null)
        {
            if (playerScript.currentVoiceChatIngameSettings.voiceAudio == null)
            {
                playerScript.currentVoiceChatIngameSettings.InitializeComponents();
            }

            if (playerScript.currentVoiceChatIngameSettings.voiceAudio == null)
            {
                return;
            }

            playerScript.currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass =
                false;
        }
    }

    internal static void LobbyStatusClientRpc(bool joinability, LobbyType type, IReadOnlyList<ulong> targets = null)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        var buffer = new FastBufferWriter(sizeof(bool) + sizeof(LobbyType), Allocator.Temp);
        buffer.WriteValue(joinability);
        buffer.WriteValue(type);

        if (targets == null)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(LobbyStatusClientRpcMessage, buffer);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(LobbyStatusClientRpcMessage, targets,
                buffer);
    }

    private static void OnLobbyStatusClientRpc(ulong senderId, FastBufferReader data)
    {
        if (senderId != NetworkManager.ServerClientId)
            return;

        if (!GameNetworkManager.Instance || !GameNetworkManager.Instance.localPlayerController || !GameNetworkManager.Instance.currentLobby.HasValue)
        {
            LobbyControl.Log.LogError($"Received {nameof(LobbyStatusClientRpc)} while not connected to a lobby!");
            return;
        }

        var currentLobby = GameNetworkManager.Instance.currentLobby.Value;

        if (currentLobby.IsOwnedBy(SteamClient.SteamId))
            return;

        data.ReadValue(out bool joinability);
        data.ReadValue(out LobbyType type);

        LobbyPatcher.Open[currentLobby] = joinability;
        LobbyPatcher.Visibility[currentLobby] = type;
    }
}
