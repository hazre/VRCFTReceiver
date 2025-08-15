# VRCFTReceiver

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod, that let's you use [VRCFaceTracking](https://github.com/benaclejames/VRCFaceTracking) Program for Eye and Face Tracking inside [Resonite](https://resonite.com/).

## Installation

1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
2. Place [VRCFTReceiver.dll](https://github.com/hazre/VRCFTReceiver/releases/latest/download/VRCFTReceiver.dll) into your `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create the folder for you.
3. `vrc-oscquery-lib.dll` and `MeaMod.DNS.dll` to rml_libs folder, you find it where resonite is installed.
4. Launch VRCFaceTracking
5. Start the game. If you want to verify that the mod is working you can check your Resonite logs.

## Requirements

- [VRCFaceTracking](https://github.com/benaclejames/VRCFaceTracking) 5.2.3

## How it works

VRCFTReceiver automatically connects VRCFaceTracking to Resonite using OSC (Open Sound Control) communication and OSCQuery for automatic discovery. 

When you start Resonite with this mod installed, it:
1. **Automatically discovers VRCFaceTracking** - Uses OSCQuery to advertise itself as a VRChat-compatible client, no manual setup required
2. **Receives face tracking data** - Gets eye and face movement data in real-time over OSC (default port: 9000)  
3. **Translates to Resonite** - Converts the data into Resonite's native eye and face tracking format
4. **Handles avatar changes** - Automatically notifies VRCFaceTracking when you switch avatars

This provides seamless face tracking in Resonite without needing to configure JSON files or manually set up connections.

## Supported Parameters

VRCFTReceiver translates VRCFaceTracking data into Resonite's native tracking systems:

### Eye Tracking
- Eye position (X/Y coordinates for left and right eyes)
- Eye openness, widening, and squinting
- Eyebrow movements (inner/outer brow vertical positions)
- Combined eye tracking for unified gaze direction

### Face Tracking
- **Mouth movements**: smile, frown, dimple, pout
- **Lip movements**: raise, stretch, press, overturn, suck
- **Jaw**: position (X/Y/Z) and opening amount
- **Tongue**: position (X/Y/Z) and roll
- **Cheek movements**: puff, suck, raise
- **Facial expressions**: nose wrinkle, chin raise

All parameters are processed in real-time and automatically mapped to Resonite's eye and mouth tracking components.

## Tested Configurations

| VRCFT Version | Module       | Device              | Tested By |
|---------------|--------------|--------------------|-----------|
| v5.2.3        | Varjo        | Varjo Aero          | ginjake   |
| v5.2.3        | iFacialMocap | N/A                 | ginjake   |
| v5.2.3        | ALVR         | VIVE Focus Vision   | ginjake   |
| v5.2.3        | LiveLink     | iPad Pro            | hazre     |

## Credits

- [Based on dfgHiatus's VRCFaceTracking Wrapper Code](https://github.com/dfgHiatus/VRCFT-Module-Wrapper/blob/master/VRCFTModuleWrapper/OSC/VRCFTOSC.cs)
- [Bunch of help from art0007i](https://github.com/art0007i)
- [Help from knackrack615](https://github.com/knackrack615)
- [Splittening support by ginjake](https://x.com/sirojake)
