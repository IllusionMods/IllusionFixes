using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace IllusionFixes
{
    public abstract class CameraTargetFixCore : BaseUnityPlugin
    {
        public const string PluginName = "Camera Target Fix";
        private const string HarmonyID = "Fix_CameraTarget_Harmony";

        protected static new ManualLogSource Logger;
        protected Harmony Harmony;

        protected virtual void Awake()
        {
            Logger = base.Logger;
            Harmony = new Harmony(HarmonyID);
        }

#if DEBUG
        internal void OnDestroy() => Harmony.UnpatchAll();
#endif

        protected static IEnumerable<CodeInstruction> StudioPatch(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            for (int i = codes.Count - 1; i >= 0; i--)
            {
                if (codes[i].opcode == OpCodes.Call && codes[i].operand.ToString().Contains("get_isOutsideTargetTex()"))
                {
                    codes[i - 1].opcode = OpCodes.Nop;
                    codes[i].opcode = OpCodes.Ldc_I4_1;
                    break;
                }
            }

            return codes;
        }
    }
}
