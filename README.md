# KoikatuFixes
A collection of fixes for common issues found in Koikatu, Koikatsu Party and some other Illusion's games made in Unity.

## How to install
0. Install latest BepInEx and BepisPlugins.
1. Grab the latest release for your game from releases tab above (Koikatu and Koikatsu Party share the same release).
2. Extract the BepInEx folder from the release into your game directory, overwrite when asked.
3. Run the game and look for any errors on the screen, asking you to remove some old plugins. All of the listed plugins have been replaced by their updated versions in this fix pack, and need to be removed after closing the game. *Plugins from this pack will not work until all of the old versions are removed.*

*Note for Koikatsu Party users: You can skip KK_Fix_CharacterListOptimizations.dll as it will not work with party. It can produce an error during load in Koikatsu Party, but the error is harmless and can be safely ignored.*

## Plugin descriptions
### CardImport
(EmotionCreators)

Prevents the game from crashing or stripping some modded data when importing KK cards.

### CultureFix
(EmotionCreators)

Set process culture to ja-JP, similarly to a locale emulator. Fixes game crashes and lockups on some system locales.

### CharacterListOptimizations 
(Koikatsu)

Makes character lists load faster.

### CenteredHSceneCursor 
(Koikatsu)

Fixes the cursor texture not being properly centeres in H scenes, so it's easier to click on things.

### DownloadRenamer
(EmotionCreators)

Maps, scenes, poses, and characters downloaded in game will have their file names changed to match the ones on the Illusion website.

### ExpandShaderDropdown 
(Koikatsu, EmotionCreators)

Makes the shader drop down menu extend down instaed of up and expands it. Necessary to select modded shaders since they run off the screen by default.

### MainGameOptimizations 
(Koikatsu)

Multiple performance optimizations for the story mode. Aimed to reduce stutter and random FPS drops.

### MakerOptimizations 
(Koikatsu, EmotionCreators)

Multiple performance optimizations for the character maker. Can greatly increase FPSMultiple performance optimizations for the character maker. Can greatly increase FPS, makes turning on/off the interface in maker by pressing space much faster (after the 1st press), and more.

### ModdedHeadEyeliner 
(Koikatsu)

Fixes modded head eyeliners not working on other head types than default.

### NodeEditorUnlock
(EmotionCreators)

### NullChecks 
(Koikatsu, EmotionCreators)

Fixes for some questionably made mods causing issues.

### PartyCardCompatibility 
(Koikatsu)

Allows loading of cards saved in Koikatsu Party (Steam release) in Koikatu and Studio.

### PersonalityCorrector 
(Koikatsu)

Prevents cards with invalid or missing personalities from crashing the game. A default personality is set instead.

### PoseLoad
(Koikatsu)

Corrects Honey Select poses loaded in Koikatsu and prevents errors.

### ResourceUnloadOptimizations 
(Koikatsu, EmotionCreators, HoneySelect)

Improves loading times and eliminates stutter after loading was "finished".

### SettingsVerifier 
(Koikatsu, AI Girl)

Prevents corrupted setting from causing issues and forces studio to use the settings.xml file instead of registry.

### ShowerAccessories 
(Koikatsu)

Prevents accessories from being removed in shower peeping mode. No more gaping holes in the head when using hair accessories.

### UnlimitedMapLights 
(Koikatsu)

Allows using an unlimited amount of map lights in studio. Not all items support more than 3 lights.
