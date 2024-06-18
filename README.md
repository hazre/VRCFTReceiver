# VRCFTReceiver

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod, that let's you use [VRCFaceTracking](https://github.com/benaclejames/VRCFaceTracking) Program for Eye and Face Tracking inside [Resonite](https://resonite.com/).

> [!WARNING]
> This is not a Plug and Play solution, it requires setup in-game.

## Installation

1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
2. Place [VRCFTReceiver.dll](https://github.com/hazre/VRCFTReceiver/releases/latest/download/VRCFTReceiver.dll) into your `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create the folder for you.
3. ~~Copy `vrc_parameters.json` template from the static folder (or from Releases) into `C:\Users\{USER}\AppData\LocalLow\VRChat\VRChat\OSC\{USER_UUID}\Avatars`.~~ (Not required as of v1.0.3)
4. Launch VRCFaceTracking
5. Start the game. If you want to verify that the mod is working you can check your Resonite logs.
6. Use the dynamic variables to drive your avatar's blendshapes

> [!NOTE]
> As of v1.0.3, `vrc_parameters.json` template with all the parameters now gets created at `C:\Users\{USER}\AppData\LocalLow\VRChat\VRChat\OSC\vrcft\Avatars` on initial install, so you don't need to copy it over manually anymore. You can edit this file if you wish to change the parameters.

## Requirements

- [VRCFaceTracking](https://github.com/benaclejames/VRCFaceTracking) 5.1.1 (Version 5.2.3 is broken currently, please use 5.1.1)
  - **How to run/install v5.1.1 even though the certificate has expired:** Download the [MSIX file](https://github.com/benaclejames/VRCFaceTracking/releases/download/5.1.1.0/VRCFaceTracking_5.1.1.0_x64.msix), rename the extension to .zip and export it in a folder. Then you should be able to run VRCFaceTracking.exe in that folder to launch VRCFT v5.1.1

## Resonite Prefabs

A Simple Template Prefab that I use to drive my avatar's face tracking blendshapes. Make to sure to assign the proper fields in the `DV` slot for disabling the face tracking in Desktop.

- Template & Sample Avatar: `resrec:///U-hazre/R-03862F323FD20FBF7E5154015D67E580586E826AC732BD956239C1A72D084EB8`

## How it works

Basically the way VRCFT works is that it waits for a OSC message that says which json file to load at `C:\Users\{USER}\AppData\LocalLow\VRChat\VRChat\OSC\{USER_UUID}\Avatars` which your VRChat avatar basically generates (If you use VRCFT Template). In this Json file, it includes all the parameters your avatar requires.

Since we aren't using VRCFT for _VRChat_, we need to get creative and create our own JSON file with parameters we need. You can find example of my own parameters file in `/static/vrc_parameters.json`. If you need access to any other parameters, you need to add it by basically copy pasting the same template used for each paramter. For example:

```json
{
  "name": "FT/v2/EyeLeftX", // Paramter name
  "input": {
    "address": "/avatar/parameters/FT/v2/EyeLeftX", // Paramter name address
    "type": "Float"
  },
  "output": {
    "address": "/avatar/parameters/FT/v2/EyeLeftX", // Paramter name address
    "type": "Float"
  }
}
```

> You can find all of the paramters here at [VRCFT Docs](https://docs.vrcft.io/docs/tutorial-avatars/tutorial-avatars-extras/parameters) (FYI: They aren't exactly 1:1 to the one used for the JSON, might need to look into how some VRC avatars do it or ask in their [Discord](https://discord.com/invite/vrcft), You can find me there as well if you need help)

The rest is pretty straight forward, we just _forward_ all the osc messages as Resonite's ValueStream's. Which is accessible using dynamic variable in this format: `User/{Paramter name}`.

You can find all the dynamic variables in a slot called `VRCFTReceiver` in your User Root.

Then you can use those dynamic variables to drive blendshapes or whatever however you want but you can use my prefab as a template.

> This last part of assigning Dynamic variables to Blendshapes is the most tedious part, so I recommend doing it in Desktop Mode.

Last Tested with [VRCFaceTracking v5.1.1](https://github.com/benaclejames/VRCFaceTracking/releases), Headset: Quest Pro, Virtual Desktop/Local ALXR Modules

## Credits

- [Sample Avatar used "Aura" by Meta](https://github.com/oculus-samples/Unity-Movement/tree/main/Samples/Models/Aura)
- [Based on dfgHiatus's VRCFaceTracking Wrapper Code](https://github.com/dfgHiatus/VRCFT-Module-Wrapper/blob/master/VRCFTModuleWrapper/OSC/VRCFTOSC.cs)
- [Bunch of help from art0007i](https://github.com/art0007i)
- [Help from knackrack615](https://github.com/knackrack615)
