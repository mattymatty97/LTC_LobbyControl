# LobbyControl

A mod for Lethal Company which allows fine control of the steam lobby whenever you're in orbit in between missions.

**Only the host** needs to install this mod. It has no impact when used as a client. Clients who
want to join you do not need to have this mod installed.

#### Lobby Command:
- status        : prints the current lobby status
- open          : open the lobby
- close         : close the lobby
- private       : set lobby to Invite Only
- friend        : set lobby to Friends Only
- public        : set lobby to Public
- rename \[name] : change the name of the lobby
- autosave      : toggle the autosave state
- save (name)   : forcefully save the lobby

### Differences to ShipLobby
This mod is a Fork of [ShipLobby](https://github.com/tinyhoot/ShipLobby) 
but instead of always reopening the lobby it allows the host to decide if and when with terminal commands

In addition this mod implements the changes from [ItemClippingFix](https://thunderstore.io/c/lethal-company/p/ViViKo/ItemClippingFix/)
expanding on it by fixing also the rotation of Objects in a newly hosted game

### Differences to LateCompany

LateCompany allows joining at any point in time, even during ongoing missions. This mod
allows joining or "backfilling" *only between missions* while the ship is in orbit.
Thus, the impact of any newly joining clients is minimal and there is no danger of desynchronisation.

## Installation

- Install [BepInEx](https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/)
- Unzip this mod into your `Lethal Company/BepInEx/plugins` folder

Or use the thunderstore mod manager to handle the installing for you. (Coming Soon)