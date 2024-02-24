LobbyControl
============
[![GitHub Release](https://img.shields.io/github/v/release/mattymatty97/LTC_LobbyControl?display_name=release&logo=github&logoColor=white)](https://github.com/mattymatty97/LTC_LobbyControl/releases/latest)
[![GitHub Pre-Release](https://img.shields.io/github/v/release/mattymatty97/LTC_LobbyControl?include_prereleases&display_name=release&logo=github&logoColor=white&label=preview)](https://github.com/mattymatty97/LTC_LobbyControl/releases)  
[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/mattymatty/LobbyControl?style=flat&logo=thunderstore&logoColor=white&label=thunderstore)](https://thunderstore.io/c/lethal-company/p/mattymatty/LobbyControl/)


A collection of Patches for Lethal Company with Quality of life changes for the Host.

**This mod is 100% Vanilla Compatible** and does not change any of the vanilla gameplay.

Patches:
--------
- ### Steam Lobby ( all controls from terminal ):
  - **re-open** the lobby everytime the Ship is **in Orbit**
  - **change the visibility** of the lobby  
  (public, Invite-Only, Friends-Only)
  - **rename** the lobby
- ### Vanilla Saving ( all controls from terminal ):
  - **toggle autosave** for the current SaveFile  
  ( no need to quit before Orbit to reset a run )
  - **force save** the lobby  
  ( optionally with a alternative filename backup )
  - **hotload** of the lobby  
  ( reload a previous/different savefile without having to quit the lobby )
- ### ItemLimit
  - **removed** limit on amount of items that can be saved  
  ( vanilla = 25 )
  - **removed** limit to amount of scrap that can be synchronized  
  ( vanilla = 250 )
- ### Storage Cabinet
  - **fix items inside of Storage Cabinet falling to the ground on load**
  - **fix items on top of Storage Cabinet falling to the ground on load**
- ### ItemClippingFix
  - **fix items clipping into the ground while dropped**
- ### RadarFixes
  - **fix orphaned radar icons from deleted scrap**  
  ( scarp sold will appear on the radar in all the maps )
  - **fix items from a newly created lobby being visible on the radar**
- ### InvisibleManFix ( Experimental )
  - **fix for late joining player being invisible if the previous owner of the body disconnected while dead**


Terminal Command:
-----------------

#### Syntax: lobby [command] (option)  
[option]  means required  
(option)  means optional

#### Sub-Commnads:
- status        : prints the current lobby status
- open          : open the lobby
- close         : close the lobby
- private       : set lobby to Invite Only
- friend        : set lobby to Friends Only
- public        : set lobby to Public
- rename \[name] : change the name of the lobby
- autosave      : toggle the autosave state
- save (name)   : forcefully save the lobby
- load (name)   : re-load the lobby from SaveFile

Differences to [ShipLobby](https://thunderstore.io/c/lethal-company/p/tinyhoot/ShipLobby/)
------------------------
This mod started as a Fork of ShipLobby
and instead of always reopening the lobby it allows the host to decide if and when with terminal commands

Differences to [ItemClippingFix](https://thunderstore.io/c/lethal-company/p/ViViKo/ItemClippingFix/)
------------------------
This mod uses the same values from ItemClippingFix
expanding on it by fixing also the rotation of Objects in a newly hosted game

Differences to [MoreItems](https://thunderstore.io/c/lethal-company/p/Drakorle/MoreItems/)
------------------------
MoreItems simply sets the max amount of items to the arbitrary value of 999.  
This mod instead removes the limit entirely, additionally it also allows you to sync the scrap value of all those items

Differences to [CupboardFix](https://thunderstore.io/c/lethal-company/p/Rocksnotch/CupboardFix/)
------------------------
CupboardFix removes the gravity from all item types that are above the ground and never resets it,  
this causes a lot of items to spawn floating both from the DropShip and inside the Factory.  
This mod instead only affects the items specifically inside the Closet and above it,  
additionally forces the parent of the objects inside back to the closet itself allowing you to move them together with the closet,  
as would happen if you had deposited the items manually inside 

Installation
------------

- Install [BepInEx](https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/)
- Unzip this mod into your `Lethal Company/BepInEx/plugins` folder

Or use the mod manager to handle the installing for you.
