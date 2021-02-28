# Illusion Fixes
A collection of fixes for common issues found in Koikatu, Koikatsu Party, EmotionCreators, AI Girl and HoneySelect2

## How to install
1. Install the latest build of [BepInEx](https://builds.bepis.io/projects/bepinex_be) and latest release of [BepisPlugins](https://github.com/IllusionMods/BepisPlugins/releases).
2. Grab the [latest release](https://github.com/IllusionMods/IllusionFixes/releases) for your game from releases tab above (Koikatu and Koikatsu Party share the same release).
3. Extract the BepInEx folder from the release into your game directory, overwrite when asked.
4. Run the game and look for any errors on the screen, asking you to remove some old plugins. All of the listed plugins have been replaced by their updated versions in this fix pack, and need to be removed after closing the game. *Plugins from this pack will not work until all of the old versions are removed.*

*Note for Koikatsu Party users: You can skip KK_Fix_CharacterListOptimizations.dll as it will not work with party. It can produce an error during load in Koikatsu Party, but the error is harmless and can be safely ignored.*

*Note for AI-Shoujo Steam users: If you are installing this manually for the first time, follow instructions below in the AI_Patch_SteamReleaseCompatibility section or you will have a bad time.*

## Plugin descriptions
### CameraTargetFix
(Koikatsu, PlayHome, AI Girl, HoneySelect2)

Hides the cursor when the camera target is disabled in Studio. In AI Girl, also makes the camera target option in the game settings work properly for the character maker.

### CardImport
(EmotionCreators)

Prevents the game from crashing or stripping some modded data when importing KK cards.

### CharacterListOptimizations 
(Koikatsu)

Makes character lists load faster.

### CenteredHSceneCursor 
(Koikatsu)

Fixes the cursor texture not being properly centeres in H scenes, so it's easier to click on things.

### DownloadRenamer
(EmotionCreators)

Maps, scenes, poses, and characters downloaded in game will have their file names changed to match the ones on the Illusion website.

### DynamicBonesFix
(Koikatsu, EmotionCreators)

Fixes dynamic bones oscillating rapidly if FPS is above 60.

### ExpandShaderDropdown 
(Koikatsu, EmotionCreators)

Makes the shader drop down menu extend down instaed of up and expands it. Necessary to select modded shaders since they run off the screen by default.

### GarbageTruck
(Koikatsu)

Modifies some game functions to be more efficient and produce less garbage, reducing the strain on Unity's garbage collector.

### HairShadows
(Koikatsu, EmotionCreators)

Modifies the render queue of front hairs and front hair accessories so that they can receive shadows. Also modifies the render queue of eyes, eyelashes, eyebrows, etc. so they can still show through hair if the option for it is selected.

### HeterochromiaFix
(Koikatsu, EmotionCreators)

Allows you to load characters with different iris types without them being reset.

### HS2_Fix_UpdateWet
(HoneySelect2)

Fixes fairly serious nullref crashes in UpdateWet caused by issues in some mods.

### InvalidSceneFileProtection
(Koikatsu, AI Girl)

Adds error handling to scene loading and importing. If a scene is invalid or from the wrong game version then a message is shown and the studio doesn't crash.

### LoadingFixes
(AI Girl, HoneySelect2)

Fixes some studio scenes failing to load (sometimes you can't load the scene you've just saved with the stock game, many scenes on uploader are like this). Also fixes color picker breaking in maker because of a similar issue.

### MainGameOptimizations 
(Koikatsu)

Multiple performance optimizations for the story mode. Aimed to reduce stutter and random FPS drops.

### MakerOptimizations 
(Koikatsu, EmotionCreators)

Multiple performance optimizations for the character maker. Can greatly increase FPSMultiple performance optimizations for the character maker. Can greatly increase FPS, makes turning on/off the interface in maker by pressing space much faster (after the 1st press), and more.

### ManifestCorrector
(Koikatsu, EmotionCreators, AI Girl, HoneySelect2)

Prevents mods that use incorrect data in the MainManifest column of item lists from locking up the game in story mode.

### ModdedHeadEyeliner 
(Koikatsu)

Fixes modded head eyeliners not working on other head types than default.

### NewGameShowAllCards
(AI Girl)

Fixes downloaded character cards not appearing in the New Game character selection (so you don't have to go to maker and re-save them).

### NodeEditorUnlock
(EmotionCreators)

### NullChecks 
(Koikatsu, EmotionCreators, AI Girl, HoneySelect2)

Fixes for some questionably made mods causing issues.

### PartyCardCompatibility 
(Koikatsu)

Allows loading of cards saved in Koikatsu Party (Steam release) in Koikatu and Studio.

### PersonalityCorrector 
(Koikatsu)

Prevents cards with invalid or missing personalities from crashing the game. A default personality is set instead.

### PoseLoad
(Koikatsu, AI Girl, HoneySelect2)

Corrects Honey Select poses loaded in Koikatsu and prevents errors.

### ResourceUnloadOptimizations 
(PlayHome, Koikatsu, EmotionCreators, HoneySelect, AI Girl, HoneySelect2)

Improves loading times and eliminates stutter after loading was "finished".

### SettingsVerifier 
(Koikatsu, AI Girl, HoneySelect2)

Prevents corrupted setting from causing issues and forces studio to use the settings.xml file instead of registry.

### ShowerAccessories 
(Koikatsu)

Prevents accessories from being removed in shower peeping mode. No more gaping holes in the head when using hair accessories.

### UnlimitedMapLights 
(Koikatsu)

Allows using an unlimited amount of map lights in studio. Not all items support more than 3 lights.

## Patcher descriptions
### AI_Patch_SteamReleaseCompatibility
(Steam release of AI Shoujo / AI Girl)

Allows using plugins made for the Japanese release of the game, and makes it possible to use Studio.
To work, the following files/folders have to be copied from the official patch for the Japanese release:
```
abdata/studio* (folder and all files)
StudioNEOV2* (folder and .exe)
abdata/chara/mm_base.unity3d (rename to mm_base_studio.unity3d, don't replace)
abdata/chara/oo_base.unity3d (rename to oo_base_studio.unity3d, don't replace)
```

### CultureFix
(EmotionCreators, AI Girl, HoneySelect2)

Set process culture to ja-JP, similarly to a locale emulator. Fixes game crashes and lockups on some system locales.
### MagicCarrot
Prevents the game from locking up when starting
