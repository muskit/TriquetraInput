using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Triquetra.Input
{
    public class Plugin : VTOLMOD
    {

        public static Plugin Instance;

        private GameObject imguiObject;
        private static string bindingsPath;

        public static void Write(object msg)
        {
            Instance.Log(msg);
        }

        public override void ModLoaded()
        {
            Instance = this;
            base.ModLoaded();
            Enable();
        }

        public void Enable()
        {
            Log("Creating Triquetra Input Object");
            imguiObject = new GameObject();
            imguiObject.AddComponent<TriquetraInputBinders>();
            GameObject.DontDestroyOnLoad(imguiObject);

            bindingsPath = PilotSaveManager.saveDataPath;
            LoadBindings("triquetrainput.xml");
        }

        public void Disable()
        {
            Log("Destroying Triquetra Input Object");
            GameObject.Destroy(imguiObject);
        }

        public static bool IsFlyingScene()
        {
            int buildIndex = SceneManager.GetActiveScene().buildIndex;
            return buildIndex == 7 || buildIndex == 11;
        }

        public static void SaveBindings(string filename)
        {
            XmlSerializer serializer = new XmlSerializer(Binding.Bindings.GetType());
            using (StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, Binding.Bindings);
                Instance.Log(writer.ToString());
            }
            using (TextWriter writer = new StreamWriter($"{bindingsPath}/{filename}"))
            {
                serializer.Serialize(writer, Binding.Bindings);
            }
        }

        public static void LoadBindings(string filename)
        {
            Binding.Bindings.Clear();
            XmlSerializer serializer = new XmlSerializer(Binding.Bindings.GetType());
            if (File.Exists($"{bindingsPath}/{filename}"))
            {
                using (Stream reader = new FileStream($"{bindingsPath}/{filename}", FileMode.Open))
                {
                    lock (Binding.Bindings)
                    {
                        Binding.Bindings = (List<Binding>)serializer.Deserialize(reader);
                    }
                }

                var devices = Binding.directInput.GetDevices().Where(Binding.IsJoystick).ToList();
                foreach (var binding in Binding.Bindings)
                {
                    binding.CreateController(devices);
                }
            }
        }
    }
}