LobbyControl
============

A collection of Patches for Lethal Company with Quality of life changes for the Host.

**This mod is 100% Vanilla Compatible** and does not change any of the vanilla gameplay.

Patches:
--------
- ### Steam Lobby ( all controls from terminal ):
  - **re-open the lobby everytime the Ship is in Orbit**
  - **change the visibility of the lobby (public, Invite-Only, Friends-Only)**
  - **rename the lobby**
- ### Vanilla Saving ( all controls from terminal ):
  - **toggle autosave for the current SaveFile**  
  ( no need to quit before Orbit to reset a run )
  - **force save the lobby**  
  ( optionally with a alternative filename backup )
  - **hotload of the lobby**  
  ( reload a previous/different savefile without having to quit the lobby )
- ### ItemClippingFix
  - **fix items clipping into the ground while dropped**
- ### RadarFixes
  - **fix orphaned radar icons from deleted scrap**  
  ( scarp sold will appear on the radar in all the maps )
  - **fix items from a newly created lobby being visible on the radar**
- ### InvisibleManFix
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

Differences to ShipLobby
------------------------
This mod started as a Fork of [ShipLobby](https://thunderstore.io/c/lethal-company/p/tinyhoot/ShipLobby/) 
and instead of always reopening the lobby it allows the host to decide if and when with terminal commands

Differences to ItemClippingFix
------------------------
This mod uses the same values from [ItemClippingFix](https://thunderstore.io/c/lethal-company/p/ViViKo/ItemClippingFix/)
expanding on it by fixing also the rotation of Objects in a newly hosted game

Installation
------------

- Install [BepInEx](https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/)
- Unzip this mod into your `Lethal Company/BepInEx/plugins` folder

Or use the mod manager to handle the installing for you.