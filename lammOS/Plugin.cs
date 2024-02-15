using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static lammOS.Commands.Commands;
using static lammOS.SyncedConfig.SyncedConfig;
using static lammOS.Variables.Variables;

namespace lammOS
{
    [BepInPlugin("lammas123.lammOS", "lammOS", "1.5.0")]
    [BepInDependency("com.rune580.LethalCompanyInputUtils", MinimumDependencyVersion: "0.6.3")]
    public class lammOS : BaseUnityPlugin
    {
        internal static lammOS Instance;
        internal static new ManualLogSource Logger;
        internal static new ConfigFile Config;
        
        public static IReadOnlyDictionary<string, string> WeatherColors;

        public static bool ShowCommandConfirmations { get; internal set; }
        public static int CharsToAutocomplete { get; internal set; }
        public static bool ShowMinimumChars { get; internal set; }
        public static char ListPaddingChar { get; internal set; }
        public static string ShowPercentagesOrRarity { get; internal set; }
        public static int MaxCommandHistory { get; internal set; }
        public static bool ShowTerminalClock { get; internal set; }
        public static bool DisableIntroSpeech { get; internal set; }

        internal static AudioClip savedShipIntroSpeechSFX;

        internal static RawImage bodyHelmetCameraImage = null;
        public static bool hasBodyHelmetCameraMod { get; internal set; } = true;
        internal static void SetupBodyHelmetCamera(Texture texture)
        {
            bodyHelmetCameraImage = Instantiate(NewTerminal.NewTerminal.Terminal.terminalImage, NewTerminal.NewTerminal.Terminal.terminalImage.transform.parent);
            bodyHelmetCameraImage.texture = texture;

            NewTerminal.NewTerminal.Terminal.terminalImage.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            NewTerminal.NewTerminal.Terminal.terminalImage.transform.localPosition = new Vector3(NewTerminal.NewTerminal.Terminal.terminalImage.transform.localPosition.x + 100, NewTerminal.NewTerminal.Terminal.terminalImage.transform.localPosition.y + 75, NewTerminal.NewTerminal.Terminal.terminalImage.transform.localPosition.z);

            bodyHelmetCameraImage.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            bodyHelmetCameraImage.transform.localPosition = new Vector3(bodyHelmetCameraImage.transform.localPosition.x + 100, bodyHelmetCameraImage.transform.localPosition.y - 75, bodyHelmetCameraImage.transform.localPosition.z);
        }
        internal static void SetBodyHelmetCameraVisibility()
        {
            if (bodyHelmetCameraImage != null)
            {
                bodyHelmetCameraImage.enabled = NewTerminal.NewTerminal.Terminal.terminalImage.enabled;
                return;
            }
            if (!hasBodyHelmetCameraMod)
            {
                return;
            }

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("ShaosilGaming.GeneralImprovements") && GameObject.Find("Environment/HangarShip/ShipModels2b/MonitorWall/MonitorGroup(Clone)") != null)
            {
                if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("RickArg.lethalcompany.helmetcameras"))
                {
                    Logger.LogWarning("Unable to use RickArg's HelmetCameras with GeneralImprovements by ShaosilGaming enabled.");
                }
                else if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Zaggy1024.OpenBodyCams"))
                {
                    Material material = GameObject.Find("Environment/HangarShip/ShipModels2b/MonitorWall/MonitorGroup(Clone)/Monitors/BigRight/RScreen").GetComponent<MeshRenderer>().material;
                    if (material.name == "BodyCamMaterial (Instance)")
                    {
                        SetupBodyHelmetCamera(material.mainTexture);
                    }
                    else
                    {
                        Logger.LogWarning("Unable to use Zaggy1024's OpenBodyCams with GeneralImprovements by ShaosilGaming if it's not on the bottom right monitor.");
                    }
                }
                else if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("SolosBodycams"))
                {
                    Logger.LogWarning("Unable to use Solo's BodyCams with GeneralImprovements by ShaosilGaming enabled.");
                }
            }
            else
            {
                if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("RickArg.lethalcompany.helmetcameras"))
                {
                    GameObject ricksHelmetCameraObject = GameObject.Find("HelmetCamera");
                    if (ricksHelmetCameraObject == null)
                    {
                        return;
                    }

                    Camera ricksHelmetCamera = ricksHelmetCameraObject.GetComponent<Camera>();
                    if (ricksHelmetCamera == null)
                    {
                        return;
                    }

                    SetupBodyHelmetCamera(ricksHelmetCamera.targetTexture);
                }
                else if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Zaggy1024.OpenBodyCams") || BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("SolosBodycams"))
                {
                    SetupBodyHelmetCamera(GameObject.Find("Environment/HangarShip/ShipModels2b/MonitorWall/Cube.001").GetComponent<MeshRenderer>().materials[2].mainTexture);
                }
            }

            if (bodyHelmetCameraImage != null)
            {
                bodyHelmetCameraImage.enabled = NewTerminal.NewTerminal.Terminal.terminalImage.enabled;
                return;
            }

            hasBodyHelmetCameraMod = false;
        }
        
        internal static GameObject Clock;
        internal static TextMeshProUGUI ClockText => Clock.GetComponent<TextMeshProUGUI>();
        internal static void SetupClock()
        {
            Transform terminalMainContainer = NewTerminal.NewTerminal.Terminal.transform.parent.parent.Find("Canvas").Find("MainContainer");
            try
            {
                Clock = terminalMainContainer.Find("Clock").gameObject;
            }
            catch
            {
                GameObject creditsObject = terminalMainContainer.Find("CurrentCreditsNum").gameObject;
                Clock = Instantiate(creditsObject, terminalMainContainer);
                Clock.name = "Clock";
                Clock.transform.localPosition = new Vector3(195f, creditsObject.transform.localPosition.y + 9f, creditsObject.transform.localPosition.z);
                Clock.transform.localScale = new Vector3(0.75f, 0.75f, 1);
                ClockText.text = "";
                ClockText.alignment = TextAlignmentOptions.TopRight;
            }
        }

        public static void Load()
        {
            LoadConfigValues();

            LoadKeywords();
            LoadMoons();
            LoadPurchasables();
            LoadEntities();
            PostLoadingEntities();
            LoadLogs();

            LoadSyncedConfigValues();
            Macros.Macros.Load();
        }

        public static void Setup()
        {
            Load();

            SetupClock();

            if (!hasSetup)
            {
                hasSetup = true;
                AddCodeCommands();
                AddCompatibilityCommands();
            }

            SetupSyncedConfig();
        }

        internal void Awake()
        {
            Instance = this;
            Logger = BepInEx.Logging.Logger.CreateLogSource("lammOS");
            Config = new(Utility.CombinePaths(Paths.ConfigPath, "lammas123.lammOS.cfg"), false, MetadataHelper.GetMetadata(this));
            new SyncedConfig.SyncedConfig();

            commands = new();
            shortcuts = new();

            LoadConfigValues();
            AddCommands();
            Keybinds.Keybinds.Setup();
            Macros.Macros.Load();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Logger.LogInfo("lammas123.lammOS Loaded");
        }
    }
}