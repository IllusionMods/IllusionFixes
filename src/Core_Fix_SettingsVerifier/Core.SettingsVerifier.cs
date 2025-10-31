using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using UnityEngine;

namespace IllusionFixes
{
    public partial class SettingsVerifier
    {
        public const string PluginName = "Settings Fix";

        internal void Awake()
        {
            //Test setup.xml for validity, delete if it has junk data
            if (File.Exists("UserData/setup.xml"))
                TestSetupXml();

            //Create a setup.xml if there isn't one
            if (!File.Exists("UserData/setup.xml"))
                CreateSetupXml();

            Hooks.Apply();
        }
        /// <summary>
        /// Read a copy of the setup.xml from the plugin's Resources folder and write it to disk
        /// </summary>
        private void CreateSetupXml()
        {
            var resourceName = $"{nameof(IllusionFixes)}.Resources.setup.xml";

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
                        case "Display":
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

        internal static void ReadSetupXml()
        {
            XElement _xml;
            int _width = 1280;
            int _height = 720;
            int _quality = 2;
            bool _full = true;

            if (File.Exists("UserData/setup.xml"))
            {
                try
                {
                    _xml = XElement.Load("UserData/setup.xml");
                    if (_xml != null)
                    {
                        IEnumerable<XElement> enumerable = _xml.Elements();
                        foreach (XElement item in enumerable)
                        {
                            switch (item.Name.ToString())
                            {
                                case "Width":
                                    _width = int.Parse(item.Value);
                                    break;
                                case "Height":
                                    _height = int.Parse(item.Value);
                                    break;
                                case "FullScreen":
                                    _full = bool.Parse(item.Value);
                                    break;
                                case "Quality":
                                    _quality = int.Parse(item.Value);
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
                }
                catch
                {
                    File.Delete("UserData/setup.xml");
                }
            }
        }
    }
}
