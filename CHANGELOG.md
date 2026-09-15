## v2.6.0
- Add lobby status synchronization between host and clients.
- Leave steam lobby when the host leaves (fixing #30 and closing #31).
- Rework friend invite button to follow the host's lobby status.
- Attempt to fix another "invisible player" bug source.

## v2.5.12
- Add queue handling for Lobby command status.
- Refactor connection queue to improve client management for late joins.
- Enhance client approval handling by introducing queue position tracking.
- Fix a late-join bug that caused null reference exceptions from netowrkObjects that lost their parent.

## v2.5.11
- forcefully close client socket instead of using NGO API to request a disconnection
- increase default connection_timeout_ms to 40s
- change Tip coroutine to wait for HUDManager animator instead of relying on the Vanilla method

## v2.5.10
- limit ConnectionQueue size to try and combat sending players in the "fog" on timeout

## v2.5.9
- minor backend changes:
  - split ConnectionQueue and LataJoin patch into finalizer and postfix

## v2.5.8
- 2nd v70 rebuild

## v2.5.7
- removed audio spatializer fix ( split into its own mod )
- rebuilt for v70

## v2.5.6
- change min timeout from 1s to 10s
- add game tips about late-join
- add opt-in connection notifications

## v2.5.5
- replace boot Popup with ingame Tip after lobby load

## v2.5.4
- add popup when LLL is detected with a low ConnectionTimeout
- increase the internal unity timeout to not disconnect while in queue
- add new hard-incompatibility with VeryLateCompany

## v2.5.2
- use System.Timers.Timer to handle the timeout

## v2.5.1
- correctly use milliseconds instead of Ticks

## v2.5.0
- Rewrite Transpliers to be more readable thanks to Zaggy
- Removed old code that is not needed anymore:
  - removed RadarName ( superseded by Zaggy's TwoRadarMaps )
  - removed ItemSync  ( superseded by AdditionalNetworking )
- replaced TransparentPlayerFix with client-side version
- added ConnectionCheckpoint API
- modified JoinQueue system to use the ConnectionCheckpoint API
- added ConnectionEvents API

## v2.4.9
- fix players being unable to join on rehost

## v2.4.6
- rewrite LateJoin system to prevent joining while loading a moon
- slightly change command handling to ( hopefully ) allow spaces in the `rename` sub-command

## v2.4.5
- make some patches future-proof
- try use MonoMod to prevent Pulling the lever if a player is in queue

## v2.4.4
- add extra checks to prevent joining while ship is landed

## v2.4.3.5
- handle Cruiser save/load

## v2.4.3.4
- use Cecil instead of ReversePatches

## v2.4.3.3
- use MonoMod to prevent round Start
- fix use of KillPlayer for InvisiblePlayerPatch
- get killPlayer and RevivePlayer rpc id at runtime

## v2.4.3.2
- Prevent landing if there are players connecting to the lobby

## v2.4.3
- compatibility with v55

## v2.4.2
- error from previous version

## v2.4.1
- Rewritten Limit Patcher

## v2.4.0
- Rewritten Transparent Player Fix thanks to Zeekers adding a better way to handle it
- Added config toggle for RadarName Patch
- Removed log patch for MoreCompany as now it is fixed
- remove packet size limit for scrap value sync

## !! From this point LobbyControl will only be compatible with v50+

## v2.3.6
- fix spaces in `lobby rename`
- fix wrong radar names on late joining ( hopefully )

## v2.3.5
- soft integration with new AsyncLoggers API

## v2.3.4
- maybe fix disconnected players from remaining in the lobby
- fix minor bug with LobbyCompatibility SoftDependency

## v2.3.3
- fix small error in LimitPatcher

## v2.3.2
- added native compatibility with ReservedSlotCore
- removed `sync_ignore_battery` and `sync_ignore_slot` in favor of new `sync_ignore_name`
- added new LogSpam fix for audio spatializer
- added LobbyCompatibility soft dependency

## v2.3.1
- added config option to whitelist "desync" slots (compatibility with reserved slots)

## v2.3.0
- added LogSpam fixes (CalculatePolygonPath)
- renamed GhostItems to ItemSync
  - added patch to fix shotguns disappearing if client has de-synced inventory
  - added patch to allow clients to pick up items that already belong to them
- fix scrap not having value/wrong state after hotload
- Yeet dependencies :D

### **SPLIT POINT**
- moved non-host related features to [Matty's Fixes]()

## v2.2.6
- fix crash with InvisibleManFix ( rpc handlers are static! )

## v2.2.5
- make the mod actually work (move harmony back to Awake)

## v2.2.4
- Fixes to InvisibleMan to avoid crashes
- Improvements to ItemClippingFix ( now all offsets are dynamically calculated based on the model affecting also modded items)
- Fixes to Late Joining ( prevent a 5th player from connecting while the 4th is still being processed)
- Added OutOfBounds patch to fix items glitching below the ship
- Improved CupBoard fixes to now track each shelf separately and snap items to them
- Added config entry to automatically re-open the lobby once in Orbit
- Actually remove the scrap limits ( previously was setting them to intMax )

## v2.2.3
- Use the correct LethalAPI dependency

## v2.2.2
- Added Config File

## v2.2.1
- Fully working lobby HotLoad
- Added Fixes for CupBoard ( storage closet ) items
- Improvements to invisible Player fix
- Removed item save limit, and scrap sync limits

## v2.2.0
- Added Experimental HotLoad of the lobby
- Added Fixes for various Radar Bugs
- Added Fix for invisible Players

## v2.1.1
- Full support of lobby status
- Addition to ste AutoSaving status and manual save command
- Integration of ItemClippingFix and expansion for host item rotation

## v2.0.0
- Fork Point and first rewrite with terminal commands

## OLD ShipLobby Changelog:

## v1.0.2

- Fixes the ship lever getting stuck if someone joins before the post-mission
  stats screen has finished displaying.
- Fixes the `Invite Friends` button working during a mission.

## v1.0.1

- Fixes an issue where the game would hang after attempting to leave the planet
  if BepInEx's `HideManagerGameObject` was not set to `true`.

## v1.0.0

Initial release.
