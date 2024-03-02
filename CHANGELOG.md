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