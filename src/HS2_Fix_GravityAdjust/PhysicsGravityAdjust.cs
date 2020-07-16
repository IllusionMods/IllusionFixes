using BepInEx;
using Common;
using UnityEngine;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class PhysicsGravityAdjust : BaseUnityPlugin
    {
        public const string GUID = "HS2_Fix_GravityAdjust";
        public const string PluginName = "Adjust physics gravity to match AI-Shoujo";

        private void Start() => Physics.gravity = new Vector3(0.0f, -98.1f, 0.0f);
    }
}