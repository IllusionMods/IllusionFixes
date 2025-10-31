using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Common;

namespace IllusionFixes
{
    /// <summary>
    /// Prevents accessories from being disabled in the shower peeping mode
    /// </summary>
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class ShowerAccessories : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_ShowerAccessories";
        public const string PluginName = "Shower Accessories Fix";

        // Disable these when entering shower even if they are set as main accessories
        private static readonly List<AccessoryEntry> _accessoryBlacklist = new List<AccessoryEntry>
        {
            new AccessoryEntry(128, 0, "a_n_shoulder"),
            new AccessoryEntry(128, 1, "a_n_shoulder"),
            new AccessoryEntry(129, 0, "a_n_hand"),
            new AccessoryEntry(125, 0, "a_n_back"),
            new AccessoryEntry(125, 1, "a_n_back"),
            new AccessoryEntry(125, 2, "a_n_back"),
            new AccessoryEntry(125, 12, "a_n_back")
        };

        private void Awake() => Hooks.ApplyHooks(GUID);

        private static void FixAccessoryState(ChaControl chaControl)
        {
            //Turn accessories back on and then turn off only Sub accessories
            chaControl.SetAccessoryStateAll(true);
            chaControl.SetAccessoryStateCategory(1, false);

            // Turn off backpacks that are set as main accessories by default
            for (var index = 0; index < chaControl.nowCoordinate.accessory.parts.Length; index++)
            {
                var acc = chaControl.nowCoordinate.accessory.parts[index];
                if (_accessoryBlacklist.Any(x =>
                    acc.type == x.Type && acc.id == x.Id && acc.parentKey.StartsWith(x.ParentKey)))
                    chaControl.fileStatus.showAccessory[index] = false;
            }
        }

        private readonly struct AccessoryEntry
        {
            public readonly int Type;
            public readonly int Id;
            public readonly string ParentKey;

            public AccessoryEntry(int type, int id, string parentKey)
            {
                Type = type;
                Id = id;
                ParentKey = parentKey;
            }
        }
    }
}