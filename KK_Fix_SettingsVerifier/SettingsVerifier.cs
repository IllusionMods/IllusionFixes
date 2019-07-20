using System;
using BepInEx;
using Common;
using Harmony;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace KK_Fix_SettingsVerifier
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class SettingsVerifier : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.settingsfix";
        public const string PluginName = "Settings Fix";

        private static SettingsVerifier _instance;

        private void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            //Test setup.xml for validity, delete if it has junk data
            if (File.Exists("UserData/setup.xml"))
                TestSetupXml();

            //Create a setup.xml if there isn't one
            if (!File.Exists("UserData/setup.xml"))
                CreateSetupXml();

            _instance = this;

            HarmonyInstance.Create(GUID).PatchAll(typeof(SettingsVerifier));
        }
        /// <summary>
        /// Run the code for reading setup.xml when inside studio. Done in a Manager.Config.Start hook because the xmlRead method needs stuff to be initialized first.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(Manager.Config), "Start")]
        public static void ManagerConfigStart()
        {
            if (CommonCode.InsideStudio)
            {
                var xmlr = typeof(InitScene).GetMethod("xmlRead", BindingFlags.NonPublic | BindingFlags.Instance);
                if (xmlr == null) throw new ArgumentException("Could not find InitScene.xmlRead");
                var initScene = _instance.gameObject.AddComponent<InitScene>();
                xmlr.Invoke(initScene, null);
                DestroyImmediate(initScene);
            }
        }
        /// <summary>
        /// Read a copy of the setup.xml from the plugin's Resources folder and write it to disk
        /// </summary>
        private void CreateSetupXml()
        {
            var resourceName = $"{nameof(KK_Fix_SettingsVerifier)}.Resources.setup.xml";

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null) throw new ArgumentException("Failed to load resource " + resourceName);

                using (FileStream fileStream = File.Create("UserData/setup.xml", (int)stream.Length))
                {
                    byte[] bytesInStream = new byte[stream.Length];
                    stream.Read(bytesInStream, 0, bytesInStream.Length);
                    fileStream.Write(bytesInStream, 0, bytesInStream.Length);
                }
            }
        }
        /// <summary>
        /// Try reading the xml, catch exceptions, delete if any invalid data
        /// </summary>
        private static void TestSetupXml()
        {
            try
            {
                var dataXml = XElement.Load("UserData/setup.xml");
                if (!dataXml.HasElements) throw new IOException();

                foreach (XElement xelement in dataXml.Elements())
                {
                    string text = xelement.Name.ToString();
                    switch (text)
                    {
                        case "Width":
                        case "Height":
                        case "Quality":
                        case "Language":
                            var val = int.Parse(xelement.Value);
                            if (val < 0) throw new ArgumentOutOfRangeException();
                            break;
                        case "FullScreen":
                            var unused = bool.Parse(xelement.Value);
                            break;
                    }
                }
            }
            catch
            {
                File.Delete("UserData/setup.xml");
            }
        }
    }
}
