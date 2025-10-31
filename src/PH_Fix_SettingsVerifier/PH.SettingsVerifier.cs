using System;
using System.IO;
using System.Reflection;
using System.Xml;
using BepInEx;
using Common;
using HarmonyLib;
using UnityEngine;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessName32bit)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInProcess(Constants.StudioProcessName32bit)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class SettingsVerifier_PH : BaseUnityPlugin
    {
        public const string PluginName = "Settings Fix";
        public const string GUID = "PH_Fix_SettingsVerifier";
        private const string resourceName = "PH_Fix_SettingsVerifier.Resources.setup.xml";

        internal void Awake()
        {
            //Test setup.xml for validity, delete if it has junk data
            if (File.Exists("UserData/setup.xml"))
                TestSetupXml();

            //Create a setup.xml if there isn't one
            if (!File.Exists("UserData/setup.xml"))
                CreateSetupXml();

            Harmony.CreateAndPatchAll(typeof(SettingsVerifier_PH));
        }

        /// <summary>
        /// Run the code for reading setup.xml when inside studio.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(Studio.Scene), "Awake")]
        internal static void StudioSceneAwake()
        {
            ReadSetupXml();
        }

        /// <summary>
        /// Run the code for reading setup.xml when inside main game.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(CautionScene), "Start")]
        internal static void CautionSceneStart()
        {
            ReadSetupXml();
        }

        /// <summary>
        /// Read a copy of the setup.xml from the plugin's Resources folder and write it to disk
        /// </summary>
        private static void CreateSetupXml()
        {
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
                XmlDocument file = new XmlDocument();
                file.Load("UserData/setup.xml");

                XmlNode node = file.SelectSingleNode("Setting");
                if (node == null)
                    throw new IOException();

                foreach (string item in new[] { "Width", "Height", "Quality", "Display", "Language" })
                {
                    XmlNode prop = node.SelectSingleNode(item);
                    if (prop == null)
                        continue;

                    if (int.Parse(prop.InnerText) < 0)
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch
            {
                File.Delete("UserData/setup.xml");
            }
        }

        private static void ReadSetupXml()
        {
            int _width = 1280;
            int _height = 720;
            int _quality = 2;
            bool _full = true;

            if (!File.Exists("UserData/setup.xml"))
                return;

            try
            {
                XmlDocument file = new XmlDocument();
                file.Load("UserData/setup.xml");

                XmlNode node = file.SelectSingleNode("Setting");
                if (node == null)
                    return;

                foreach (string item in new[] { "Width", "Height", "Quality", "Display", "Language", "FullScreen" })
                {
                    XmlNode prop = node.SelectSingleNode(item);
                    if (prop == null)
                        continue;

                    switch (item)
                    {
                        case "Width":
                            _width = int.Parse(prop.InnerText);
                            break;
                        case "Height":
                            _height = int.Parse(prop.InnerText);
                            break;
                        case "FullScreen":
                            _full = bool.Parse(prop.InnerText);
                            break;
                        case "Quality":
                            _quality = int.Parse(prop.InnerText);
                            break;
                    }
                }

                Screen.SetResolution(_width, _height, _full);
                switch (_quality)
                {
                    case 0:
                        QualitySettings.SetQualityLevel(0);
                        break;
                    case 1:
                        QualitySettings.SetQualityLevel(2);
                        break;
                    case 2:
                        QualitySettings.SetQualityLevel(4);
                        break;
                }
            }
            catch
            {
                File.Delete("UserData/setup.xml");
            }
        }
    }
}