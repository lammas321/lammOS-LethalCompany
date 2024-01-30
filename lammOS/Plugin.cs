using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static lammOS.Commands.Commands;
using static lammOS.SyncedConfig.SyncedConfig;
using static lammOS.Variables.Variables;

namespace lammOS
{
    [BepInPlugin("lammas123.lammOS", "lammOS", "1.3.1")]
    [BepInDependency("com.rune580.LethalCompanyInputUtils", MinimumDependencyVersion: "0.6.1")]
    public class lammOS : BaseUnityPlugin
    {
        internal static lammOS Instance;
        internal static new ManualLogSource Logger;
        internal static new ConfigFile Config;

        internal static ConfigEntry<bool> ShowCommandConfirmations;
        public static bool ShowCommandConfirmationsValue { get; internal set; }
        internal static ConfigEntry<int> CharsToAutocomplete;
        public static int CharsToAutocompleteValue { get; internal set; }
        internal static ConfigEntry<bool> ShowMinimumChars;
        public static bool ShowMinimumCharsValue { get; internal set; }
        internal static ConfigEntry<string> ListPaddingChar;
        public static char ListPaddingCharValue { get; internal set; }
        internal static ConfigEntry<string> ShowPercentagesOrRarity;
        public static string ShowPercentagesOrRarityValue { get; internal set; }
        internal static ConfigEntry<int> MaxCommandHistory;
        public static int MaxCommandHistoryValue { get; internal set; }
        internal static ConfigEntry<bool> ShowTerminalClock;
        public static bool ShowTerminalClockValue { get; internal set; }
        internal static ConfigEntry<bool> DisableTextPostProcessMethod;
        public static bool DisableTextPostProcessMethodValue { get; internal set; }

        internal static RawImage bodyHelmetCameraImage = null;
        public static bool hasBodyHelmetCameraMod { get; internal set; } = true;
        internal static void SetupBodyHelmetCamera(Texture texture)
        {
            bodyHelmetCameraImage = Instantiate(Variables.Variables.Terminal.terminalImage, Variables.Variables.Terminal.terminalImage.transform.parent);
            bodyHelmetCameraImage.texture = texture;

            Variables.Variables.Terminal.terminalImage.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            Variables.Variables.Terminal.terminalImage.transform.localPosition = new Vector3(Variables.Variables.Terminal.terminalImage.transform.localPosition.x + 100, Variables.Variables.Terminal.terminalImage.transform.localPosition.y + 75, Variables.Variables.Terminal.terminalImage.transform.localPosition.z);

            bodyHelmetCameraImage.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            bodyHelmetCameraImage.transform.localPosition = new Vector3(bodyHelmetCameraImage.transform.localPosition.x + 100, bodyHelmetCameraImage.transform.localPosition.y - 75, bodyHelmetCameraImage.transform.localPosition.z);
        }
        internal static void SetBodyHelmetCamera()
        {
            if (bodyHelmetCameraImage != null)
            {
                bodyHelmetCameraImage.enabled = Variables.Variables.Terminal.terminalImage.enabled;
                return;
            }
            if (!hasBodyHelmetCameraMod)
            {
                return;
            }

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

            if (bodyHelmetCameraImage != null)
            {
                bodyHelmetCameraImage.enabled = Variables.Variables.Terminal.terminalImage.enabled;
                return;
            }

            hasBodyHelmetCameraMod = false;
        }

        internal static GameObject Clock;
        internal static TextMeshProUGUI ClockText => Clock.GetComponent<TextMeshProUGUI>();
        internal static void SetupClock()
        {
            Transform terminalMainContainer = Variables.Variables.Terminal.transform.parent.parent.Find("Canvas").Find("MainContainer");
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

        public static bool hasSetup { get; internal set; } = false;
        public static readonly string newText = "Powered by lammOS     Created by lammas123\n          Courtesy of the Company\n\nType HELP for a list of available commands.\n\n>";
        internal static void AddCodeCommands()
        {
            foreach (TerminalKeyword keyword in terminalKeywords.Values)
            {
                if (keyword.word.Length == 2)
                {
                    AddCommand(new CodeCommand(keyword.word));
                }
            }
        }
        internal static void AddCompatibilityCommands()
        {
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.malco.lethalcompany.moreshipupgrades"))
            {
                Logger.LogInfo("Adding Lategame Upgrades compatibility commands");
                AddCommand(new DummyCommand("Lategame Upgrades", "lategame", "Displays information related to the Lategame Upgrades mod."));
                AddCommand(new DummyCommand("Lategame Upgrades", "lgu", "Displays the purchasable upgrades from the Lategame Upgrades store."));
            }
        }
        internal static void ReplaceDefaultSetupScreen()
        {
            TerminalNode node = Variables.Variables.Terminal.terminalNodes.specialNodes[0];
            node.displayText = node.displayText.Replace("Halden Electronics Inc.", "lammas123").Replace("FORTUNE-9", "lammOS");

            TerminalNode resultNode = node.terminalOptions[0].result.terminalOptions[0].result;
            resultNode.displayText = resultNode.displayText.Substring(0, resultNode.displayText.IndexOf("Welcome to the FORTUNE-9 OS")) + newText;
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
                ReplaceDefaultSetupScreen();
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