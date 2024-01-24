using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LethalCompanyInputUtils.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace lammOS
{
    [BepInPlugin("lammas123.lammOS", "lammOS", "1.2.0")]
    [BepInDependency("com.rune580.LethalCompanyInputUtils", MinimumDependencyVersion: "0.6.0")]
    public class lammOS : BaseUnityPlugin
    {
        public static Dictionary<string, TerminalKeyword> terminalKeywords;
        public static Dictionary<string, int> terminalSyncedSounds = new()
        {
            { "buy", 0 },
            { "error", 1 },
            { "loading", 2 },
            { "warning", 3 }
        };
        public static Dictionary<string, Moon> moons;
        public static Dictionary<string, string> moonNameToMoonNode;
        public static Dictionary<string, PurchaseableItem> purchaseableItems;
        public static Dictionary<string, PurchaseableUnlockable> purchaseableUnlockables;
        public static Dictionary<string, Entity> entities;
        public static List<EnemyType> entitiesWithoutEntry;
        public static List<TerminalNode> entriesWithoutEntity;
        public static Dictionary<string, Log> logs;

        public static bool isSetup { get; internal set; } = false;
        public static Command currentCommand { get; internal set; } = null;
        public static bool runningMacro { get; internal set; } = false;

        internal static ConfigEntry<bool> ShowTerminalClock;
        public static bool ShowTerminalClockValue { get; internal set; }
        internal static ConfigEntry<bool> ShowMinimumChars;
        public static bool ShowMinimumCharsValue { get; internal set; }
        internal static ConfigEntry<int> CharsToAutocomplete;
        public static int CharsToAutocompleteValue { get; internal set; }
        internal static ConfigEntry<bool> ShowCommandConfirmations;
        public static bool ShowCommandConfirmationsValue { get; internal set; }
        internal static ConfigEntry<int> MaxCommandHistory;
        public static int MaxCommandHistoryValue { get; internal set; }
        internal static ConfigEntry<string> ShowPercentagesOrRarity;
        public static string ShowPercentagesOrRarityValue { get; internal set; }
        internal static ConfigEntry<bool> DisableTextPostProcessMethod;
        public static bool DisableTextPostProcessMethodValue { get; internal set; }

        internal static Dictionary<string, Command> commands;
        internal static Dictionary<string, string> shortcuts;

        internal static lammOS Instance;
        internal static new ManualLogSource Logger;
        internal static new ConfigFile Config;

        internal static List<string> commandHistory = new();
        internal static int commandHistoryIndex = -1;
        internal static string lastTypedCommand = "";

        internal static readonly string newStartupText = "Powered by lammOS     Created by lammas123\n          Courtesy of the Company\n\nType HELP for a list of available commands.\n\n>";
        internal static string currentLoadedNode = "";
        internal static string startupNodeText = "";
        internal static string helpNodeText = "";

        internal static GameObject Clock;
        internal static TextMeshProUGUI ClockText => Clock.GetComponent<TextMeshProUGUI>();

        internal void Awake()
        {
            Instance = this;
            Logger = BepInEx.Logging.Logger.CreateLogSource("lammOS");
            Config = new(Utility.CombinePaths(Paths.ConfigPath, "lammas123.lammOS.cfg"), false, MetadataHelper.GetMetadata(this));
            new SyncedConfig();

            commands = new();
            shortcuts = new();

            AddCommand(new HelpCommand());
            AddCommand(new ShortcutsCommand());

            AddCommand(new MoonsCommand());
            AddCommand(new MoonCommand());
            AddCommand(new RouteCommand());

            AddCommand(new StoreCommand());
            AddCommand(new BuyCommand());
            AddCommand(new StorageCommand());
            AddCommand(new RetrieveCommand());

            AddCommand(new BestiaryCommand());
            AddCommand(new SigurdCommand());

            AddCommand(new MonitorCommand());
            AddCommand(new TargetsCommand());
            AddCommand(new SwitchCommand());
            AddCommand(new PingCommand());
            AddCommand(new FlashCommand());

            AddCommand(new DoorCommand());
            AddCommand(new LightsCommand());
            AddCommand(new TeleporterCommand());
            AddCommand(new InverseTeleporterCommand());

            AddCommand(new ScanCommand());
            AddCommand(new TransmitCommand());
            AddCommand(new ClearCommand());
            AddCommand(new CodesCommand());
            AddCommand(new ReloadCommand());
            AddCommand(new EjectCommand());

            AddCommand(new MacrosCommand());
            AddCommand(new RunMacroCommand());
            AddCommand(new CreateMacroCommand());
            AddCommand(new InfoMacroCommand());
            AddCommand(new EditMacroCommand());
            AddCommand(new DeleteMacroCommand());

            AddCommand(new DebugCommand());

            Keybinds.Setup();
            LoadConfigValues();
            Macros.Load();
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Logger.LogInfo("lammas123.lammOS Loaded");
        }

        public static bool AddCommand(Command command)
        {
            if (HasCommand(command.id))
            {
                Logger.LogError("A command with the id '" + command.id + "' has already been added.");
                return false;
            }
            if (IsShortcut(command.id))
            {
                Logger.LogWarning("There is a shortcut that will clash with the added command with the id '" + command.id + "'.");
            }
            commands.Add(command.id, command);
            return true;
        }
        public static bool HasCommand(string id)
        {
            return commands.ContainsKey(id);
        }
        public static Command GetCommand(string id)
        {
            if (!HasCommand(id))
            {
                return null;
            }
            return commands[id];
        }
        public static List<string> GetCommandIds()
        {
            return new(commands.Keys);
        }
        public static List<Command> GetCommands()
        {
            return new(commands.Values);
        }
        public static bool RemoveCommand(string id)
        {
            return commands.Remove(id);
        }

        public static bool AddShortcut(string shortcut, string id)
        {
            if (IsShortcut(shortcut))
            {
                Logger.LogError("A shortcut using the string '" + shortcut + "' already exists.");
                return false;
            }
            if (HasCommand(shortcut))
            {
                Logger.LogWarning("There is a command with the id '" + shortcut + "' that will be overruled by the added shortcut.");
            }
            shortcuts.Add(shortcut, id);
            return true;
        }
        public static bool IsShortcut(string shortcut)
        {
            return shortcuts.ContainsKey(shortcut);
        }
        public static bool CommandIdHasShortcut(string id)
        {
            return shortcuts.ContainsValue(id);
        }
        public static string GetShortcutByCommandID(string id)
        {
            foreach (string shortcut in GetShortcuts())
            {
                if (id == shortcuts[shortcut])
                {
                    return shortcut;
                }
            }
            return null;
        }
        public static string GetCommandIdByShortcut(string shortcut)
        {
            if (!IsShortcut(shortcut))
            {
                return null;
            }
            return shortcuts[shortcut];
        }
        public static List<string> GetShortcuts()
        {
            return new(shortcuts.Keys);

        }
        public static bool RemoveShortcut(string shortcut)
        {
            return shortcuts.Remove(shortcut);
        }

        public static int GetMoonCost(string moonId)
        {
            float multiplier = SyncedConfig.Instance.MoonPriceMultipliers.TryGetValue(moonId, out float value) ? value : -1f;
            if (multiplier == -1)
            {
                return moons[moonId].GetNode().itemCost;
            }
            if (moons[moonId].GetNode().itemCost == 0)
            {
                return (int)multiplier;
            }
            return (int)(moons[moonId].GetNode().itemCost * multiplier);
        }
        public static int GetItemCost(string itemId)
        {
            float multiplier = SyncedConfig.Instance.ItemPriceMultipliers.TryGetValue(itemId, out float value) ? value : -1f;
            Terminal terminal = FindObjectOfType<Terminal>();
            if (multiplier == -1)
            {
                return purchaseableItems[itemId].GetItem(ref terminal).creditsWorth;
            }
            if (purchaseableItems[itemId].GetItem(ref terminal).creditsWorth == 0)
            {
                return (int)multiplier;
            }
            return (int)(purchaseableItems[itemId].GetItem(ref terminal).creditsWorth * multiplier);
        }
        public static int GetUnlockableCost(string unlockableId)
        {
            float multiplier = SyncedConfig.Instance.UnlockablePriceMultipliers.TryGetValue(unlockableId, out float value) ? value : -1f;
            if (multiplier == -1)
            {
                return purchaseableUnlockables[unlockableId].GetNode().itemCost;
            }
            if (purchaseableUnlockables[unlockableId].GetNode().itemCost == 0)
            {
                return (int)multiplier;
            }
            return (int)(purchaseableUnlockables[unlockableId].GetNode().itemCost * multiplier);
        }

        public class Moon
        {
            public int levelIndex;
            public int nodeIndex;
            public string shortestChars;

            public Moon(int levelIndex, int nodeIndex)
            {
                this.levelIndex = levelIndex;
                this.nodeIndex = nodeIndex;
            }

            public SelectableLevel GetMoon()
            {
                return StartOfRound.Instance.levels[levelIndex];
            }

            public TerminalNode GetNode()
            {
                return terminalKeywords["route"].compatibleNouns[nodeIndex].result.terminalOptions[1].result;
            }
        }
        public class PurchaseableItem
        {
            public int index;
            public string shortestChars;

            public PurchaseableItem(int index)
            {
                this.index = index;
            }

            public Item GetItem(ref Terminal terminal)
            {
                return terminal.buyableItemsList[index];
            }

            public int GetSalePercentage(ref Terminal terminal)
            {
                return terminal.itemSalesPercentages[index];
            }
        }
        public class PurchaseableUnlockable
        {
            public int unlockableIndex;
            public int nodeIndex;
            public string shortestChars;

            public PurchaseableUnlockable(int unlockableIndex, int nodeIndex)
            {
                this.unlockableIndex = unlockableIndex;
                this.nodeIndex = nodeIndex;
            }

            public UnlockableItem GetUnlockable()
            {
                return StartOfRound.Instance.unlockablesList.unlockables[unlockableIndex];
            }

            public TerminalNode GetNode()
            {
                return terminalKeywords["buy"].compatibleNouns[nodeIndex].result;
            }
        }
        public class Entity
        {
            public EnemyType entityType;
            public int entryIndex;
            public string displayName;
            public string shortestChars;

            public Entity(ref EnemyType entityType, int entryIndex, ref Terminal terminal, string displayName = null)
            {
                this.entityType = entityType;
                this.entryIndex = entryIndex;
                this.displayName = displayName;
                if (this.displayName == null)
                {
                    this.displayName = this.entryIndex == -1 ? this.entityType.enemyName : terminal.enemyFiles[entryIndex].creatureName;
                }
            }

            public EnemyType GetEntity()
            {
                return entityType;
            }

            public TerminalNode GetNode(ref Terminal terminal)
            {
                if (entryIndex == -1)
                {
                    return null;
                }
                return terminal.enemyFiles[entryIndex];
            }
        }
        public class Log
        {
            public int logIndex;
            public string shortestChars;

            public Log(int logIndex)
            {
                this.logIndex = logIndex;
            }

            public TerminalNode GetNode(ref Terminal terminal)
            {
                return terminal.logEntryFiles[logIndex];
            }
        }

        internal void LoadConfigValues()
        {
            if (File.Exists(Config.ConfigFilePath))
            {
                Config.Reload();
            }
            else
            {
                Config.Clear();
            }

            ShowTerminalClock = Config.Bind("General", "ShowTerminalClock", true, "If the terminal clock should be shown in the top right corner or not.");
            ShowTerminalClockValue = ShowTerminalClock.Value;

            ShowMinimumChars = Config.Bind("General", "ShowMinimumChars", false, "If the minimum characters required for the terminal to autocomplete an argument should be shown. For example: 'p' when buying a pro flashlight, or 'te' for the teleporter, while 'telev' is the minimum for the television. Having this on all the time doesn't look the greatest, but it helps when learning typing shortcuts.");
            ShowMinimumCharsValue = ShowMinimumChars.Value;

            CharsToAutocomplete = Config.Bind("General", "CharsToAutocomplete", 3, "The amount of characters required to autocomplete an argument, such as when buying an item, the minimum value is 1. Values over the length of an argument will be treated as the length of the argument to avoid errors, meaning you'll have to type the full name of the argument and no autocompleting can occur.");
            if (CharsToAutocomplete.Value < 1)
            {
                CharsToAutocomplete.Value = 1;
            }
            CharsToAutocompleteValue = CharsToAutocomplete.Value;

            ShowCommandConfirmations = Config.Bind("General", "ShowCommandConfirmations", true, "If commands like >BUY should ask you for confirmation before doing something.");
            ShowCommandConfirmationsValue = ShowCommandConfirmations.Value;

            MaxCommandHistory = Config.Bind("General", "MaxCommandHistory", 25, "How far back the terminal should remember commands you've typed in, the minimum value is 1 and the maximum is 100.");
            if (MaxCommandHistory.Value < 1)
            {
                MaxCommandHistory.Value = 1;
            }
            else if (MaxCommandHistory.Value > 100)
            {
                MaxCommandHistory.Value = 100;
            }
            MaxCommandHistoryValue = MaxCommandHistory.Value;

            ShowPercentagesOrRarity = Config.Bind("General", "ShowPercentagesOrRarity", "Percentage", "Whether a percentage (%) or rarity (fraction) should be shown next to things that have a chance of happening. Percentage or Rarity");
            if (ShowPercentagesOrRarity.Value != "Percentage" && ShowPercentagesOrRarity.Value != "Rarity")
            {
                ShowPercentagesOrRarity.Value = "Percentage";
            }
            ShowPercentagesOrRarityValue = ShowPercentagesOrRarity.Value;

            DisableTextPostProcessMethod = Config.Bind("General", "DisableTextPostProcessMethod", true, "If the terminal's TextPostProcess method should be disabled. lammOS does not use this method so it is disabled by default to make running commands a bit faster, but this option is here in case any other mods utilize it.");
            DisableTextPostProcessMethodValue = DisableTextPostProcessMethod.Value;

            Config.Save();
        }
        internal void LoadKeywords(ref Terminal terminal)
        {
            terminalKeywords = new();
            foreach (TerminalKeyword terminalKeyword in terminal.terminalNodes.allKeywords)
            {
                if (terminalKeywords.ContainsKey(terminalKeyword.word))
                {
                    Logger.LogWarning("A terminal keyword has already been added with the name: '" + terminalKeyword.word + "'");
                    continue;
                }
                terminalKeywords.Add(terminalKeyword.word, terminalKeyword);
            }
        }
        internal void LoadMoons()
        {
            moons = new();
            moonNameToMoonNode = new();
            entitiesWithoutEntry = new();
            for (int i = 0; i < StartOfRound.Instance.levels.Length; i++)
            {
                for (int j = 0; j < terminalKeywords["route"].compatibleNouns.Length; j++)
                {
                    if (StartOfRound.Instance.levels[i].PlanetName.Substring(StartOfRound.Instance.levels[i].PlanetName.IndexOf(" ") + 1).ToLower() == terminalKeywords["route"].compatibleNouns[j].noun.word || (StartOfRound.Instance.levels[i].PlanetName == "71 Gordion" && terminalKeywords["route"].compatibleNouns[j].noun.word == "company"))
                    {
                        Moon moon = new(i, j);
                        moons.Add(terminalKeywords["route"].compatibleNouns[j].noun.word, moon);
                        moonNameToMoonNode.Add(StartOfRound.Instance.levels[i].PlanetName.ToLower(), terminalKeywords["route"].compatibleNouns[j].noun.word);
                        SelectableLevel level = moon.GetMoon();
                        foreach (SpawnableEnemyWithRarity entity in level.Enemies)
                        {
                            if (!entitiesWithoutEntry.Contains(entity.enemyType))
                            {
                                entitiesWithoutEntry.Add(entity.enemyType);
                            }
                        }
                        foreach (SpawnableEnemyWithRarity entity in level.OutsideEnemies)
                        {
                            if (!entitiesWithoutEntry.Contains(entity.enemyType))
                            {
                                entitiesWithoutEntry.Add(entity.enemyType);
                            }
                        }
                        foreach (SpawnableEnemyWithRarity entity in level.DaytimeEnemies)
                        {
                            if (!entitiesWithoutEntry.Contains(entity.enemyType))
                            {
                                entitiesWithoutEntry.Add(entity.enemyType);
                            }
                        }
                        break;
                    }
                }
            }

            foreach (string moonId in moons.Keys)
            {
                for (int i = Mathf.Min(moonId.Length, CharsToAutocomplete.Value); i <= moonId.Length; i++)
                {
                    string shortestChars = moonId.Substring(0, i);
                    bool shortest = true;
                    foreach (string moonId2 in moons.Keys)
                    {
                        if (moonId == moonId2)
                        {
                            break;
                        }
                        else if (moonId2.StartsWith(shortestChars))
                        {
                            shortest = false;
                            break;
                        }
                    }
                    if (shortest)
                    {
                        moons[moonId].shortestChars = shortestChars;
                        break;
                    }
                }
                if (moons[moonId].shortestChars == null)
                {
                    // TODO fix, an earlier name conflicts by being too similar (or the same) and of the same or longer length than this name
                    moons[moonId].shortestChars = "ERR";
                }
            }
        }
        internal void LoadPurchaseables(ref Terminal terminal)
        {
            purchaseableItems = new();
            for (int i = 0; i < terminal.buyableItemsList.Length; i++)
            {
                purchaseableItems.Add(terminal.buyableItemsList[i].itemName.ToLower(), new PurchaseableItem(i));
            }

            foreach (string itemId in purchaseableItems.Keys)
            {
                for (int i = Mathf.Min(itemId.Length, CharsToAutocomplete.Value); i < itemId.Length; i++)
                {
                    string shortestChars = itemId.Substring(0, i);
                    bool shortest = true;
                    foreach (string itemId2 in purchaseableItems.Keys)
                    {
                        if (itemId == itemId2)
                        {
                            break;
                        }

                        if (itemId2.StartsWith(shortestChars))
                        {
                            shortest = false; ;
                            break;
                        }
                    }
                    if (shortest)
                    {
                        purchaseableItems[itemId].shortestChars = shortestChars;
                        break;
                    }
                }
                if (purchaseableItems[itemId].shortestChars == null)
                {
                    // TODO fix, an earlier name conflicts by being too similar (or the same) and of the same or longer length than this name
                    purchaseableItems[itemId].shortestChars = "ERR";
                }
            }

            purchaseableUnlockables = new();
            for (int i = 0; i < StartOfRound.Instance.unlockablesList.unlockables.Count; i++)
            {
                bool foundMatch = false;
                for (int j = 0; j < terminalKeywords["buy"].compatibleNouns.Length; j++)
                {
                    if (StartOfRound.Instance.unlockablesList.unlockables[i].unlockableName.ToLower().Replace("-", " ").StartsWith(terminalKeywords["buy"].compatibleNouns[j].noun.word.Replace("-", " ")))
                    {
                        purchaseableUnlockables.Add(terminalKeywords["buy"].compatibleNouns[j].result.creatureName.ToLower(), new PurchaseableUnlockable(i, j));
                        foundMatch = true;
                        break;
                    }
                }
                if (!foundMatch && !StartOfRound.Instance.unlockablesList.unlockables[i].alreadyUnlocked && !StartOfRound.Instance.unlockablesList.unlockables[i].unlockedInChallengeFile)
                {
                    Logger.LogWarning("Could not find a matching buy node for the unlockable: '" + StartOfRound.Instance.unlockablesList.unlockables[i].unlockableName + "'");
                }
            }

            foreach (string unlockableId in purchaseableUnlockables.Keys)
            {
                for (int i = Mathf.Min(unlockableId.Length, CharsToAutocomplete.Value); i < unlockableId.Length; i++)
                {
                    string shortestChars = unlockableId.Substring(0, i);
                    bool shortest = true;
                    foreach (string itemId2 in purchaseableItems.Keys)
                    {
                        if (unlockableId == itemId2)
                        {
                            break;
                        }

                        if (itemId2.StartsWith(shortestChars))
                        {
                            shortest = false;
                            break;
                        }
                    }
                    if (shortest)
                    {
                        foreach (string unlockableId2 in purchaseableUnlockables.Keys)
                        {
                            if (unlockableId == unlockableId2)
                            {
                                break;
                            }

                            if (unlockableId2.StartsWith(shortestChars))
                            {
                                shortest = false;
                                break;
                            }
                        }
                        if (shortest)
                        {
                            purchaseableUnlockables[unlockableId].shortestChars = shortestChars;
                            break;
                        }
                    }
                }
                if (purchaseableUnlockables[unlockableId].shortestChars == null)
                {
                    // TODO fix, an earlier name conflicts by being too similar (or the same) and of the same or longer length than this name
                    purchaseableUnlockables[unlockableId].shortestChars = "ERR";
                }
            }
        }
        internal void LoadEntities(ref Terminal terminal)
        {
            int entityIndex = 0;
            entities = new();
            entriesWithoutEntity = new(terminal.enemyFiles);
            while (entityIndex < entitiesWithoutEntry.Count)
            {
                EnemyType entity = entitiesWithoutEntry[entityIndex];
                switch (entity.enemyName)
                {
                    case "Centipede":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, 0, ref terminal));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureFileID == 0)
                                {
                                    entriesWithoutEntity.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        }
                    case "Flowerman":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, 1, ref terminal));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureFileID == 1)
                                {
                                    entriesWithoutEntity.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        }
                    case "Crawler":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, 2, ref terminal));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureFileID == 2)
                                {
                                    entriesWithoutEntity.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        }
                    case "MouthDog":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, 3, ref terminal));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureFileID == 3)
                                {
                                    entriesWithoutEntity.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        }
                    case "Hoarding bug":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, 4, ref terminal));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureFileID == 4)
                                {
                                    entriesWithoutEntity.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        }
                    case "Blob":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, 5, ref terminal));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureFileID == 5)
                                {
                                    entriesWithoutEntity.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        }
                    case "ForestGiant":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, 6, ref terminal));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureFileID == 6)
                                {
                                    entriesWithoutEntity.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        }
                    case "Spring":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, 7, ref terminal));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureFileID == 7)
                                {
                                    entriesWithoutEntity.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        }
                    case "Lasso":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, 8, ref terminal));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureFileID == 8)
                                {
                                    entriesWithoutEntity.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        }
                    case "Earth Leviathan":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, 9, ref terminal));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureFileID == 9)
                                {
                                    entriesWithoutEntity.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        }
                    case "Jester":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, 10, ref terminal));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureFileID == 10)
                                {
                                    entriesWithoutEntity.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        }
                    case "Puffer":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, 11, ref terminal));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureFileID == 11)
                                {
                                    entriesWithoutEntity.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        }
                    case "Bunker Spider":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, 12, ref terminal));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureFileID == 12)
                                {
                                    entriesWithoutEntity.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        }
                    case "Manticoil":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, 13, ref terminal));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureFileID == 13)
                                {
                                    entriesWithoutEntity.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        }
                    case "Red Locust Bees":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, 14, ref terminal));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureFileID == 14)
                                {
                                    entriesWithoutEntity.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        }
                    case "Docile Locust Bees":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, 15, ref terminal));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureFileID == 15)
                                {
                                    entriesWithoutEntity.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        }
                    case "Baboon hawk":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, 16, ref terminal));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureFileID == 16)
                                {
                                    entriesWithoutEntity.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        }
                    case "Nutcracker":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, 17, ref terminal));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureFileID == 17)
                                {
                                    entriesWithoutEntity.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        }
                    case "Girl":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, -1, ref terminal, "Ghost Girl"));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            break;
                        }
                    case "Masked":
                        {
                            entities.Add(entity.enemyName, new Entity(ref entity, -1, ref terminal));
                            entitiesWithoutEntry.RemoveAt(entityIndex);
                            break;
                        }
                    default:
                        {
                            bool foundMatch = false;
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureName.ToLower().Replace(" ", "").Replace("-", "").StartsWith(entity.enemyName.ToLower().Replace(" ", "").Replace("-", "")))
                                {
                                    entities.Add(entity.enemyName, new Entity(ref entity, entriesWithoutEntity[i].creatureFileID, ref terminal));
                                    entitiesWithoutEntry.RemoveAt(entityIndex);
                                    entriesWithoutEntity.RemoveAt(i);
                                    foundMatch = true;
                                    break;
                                }
                            }
                            if (!foundMatch)
                            {
                                entityIndex++;
                            }
                            break;
                        }
                }
            }

            foreach (Entity entity in entities.Values)
            {
                for (int i = Mathf.Min(entity.displayName.Length, CharsToAutocomplete.Value); i < entity.displayName.Length; i++)
                {
                    string shortestChars = entity.displayName.Substring(0, i).ToLower();
                    bool shortest = true;
                    foreach (Entity entity2 in entities.Values)
                    {
                        if (entity.displayName == entity2.displayName)
                        {
                            break;
                        }

                        if (entity2.displayName.ToLower().StartsWith(shortestChars))
                        {
                            shortest = false;
                            break;
                        }
                    }
                    if (shortest)
                    {
                        entity.shortestChars = shortestChars;
                        break;
                    }
                }
                if (entity.shortestChars == null)
                {
                    // TODO fix, an earlier name conflicts by being too similar (or the same) and of the same or longer length than this name
                    entity.shortestChars = "ERR";
                }
            }
        }
        internal void PostLoadingEntities()
        {
            foreach (EnemyType entity in entitiesWithoutEntry)
            {
                Logger.LogWarning("The entity '" + entity.enemyName + "' could not successfully be loaded.");
            }

            foreach (TerminalNode node in entriesWithoutEntity)
            {
                Logger.LogWarning("The bestiary entry for '" + node.creatureName + "' could not be matched to an entity.");
            }
        }
        internal void LoadLogs(ref Terminal terminal)
        {
            logs = new();
            for (int i = 0; i < terminal.logEntryFiles.Count; i++)
            {
                logs.Add(terminal.logEntryFiles[i].creatureName, new Log(i));
            }

            foreach (string logId in logs.Keys)
            {
                for (int i = Mathf.Min(logId.Length, CharsToAutocomplete.Value); i <= logId.Length; i++)
                {
                    string shortestChars = logId.Substring(0, i);
                    bool shortest = true;
                    foreach (string logId2 in logs.Keys)
                    {
                        if (logId == logId2)
                        {
                            break;
                        }

                        if (logId2.StartsWith(shortestChars))
                        {
                            shortest = false;
                            break;
                        }
                    }
                    if (shortest)
                    {
                        logs[logId].shortestChars = shortestChars;
                        break;
                    }
                }
                if (logs[logId].shortestChars == null)
                {
                    // TODO fix, an earlier name conflicts by being too similar (or the same) and of the same or longer length than this name
                    logs[logId].shortestChars = "ERR";
                }
            }
        }
        internal void SyncConfigValues()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                SyncedConfig.SetValues();

                foreach (ulong clientId in StartOfRound.Instance.ClientPlayerList.Keys)
                {
                    SyncedConfig.SyncWithClient(clientId);
                }
            }
        }
        internal void LoadVariables(ref Terminal terminal)
        {
            LoadConfigValues();

            LoadKeywords(ref terminal);
            LoadMoons();
            LoadPurchaseables(ref terminal);
            LoadEntities(ref terminal);
            PostLoadingEntities();
            LoadLogs(ref terminal);
            
            SyncConfigValues();
            Macros.Load();
        }

        internal void SetupClock(ref Terminal terminal)
        {
            Transform terminalMainContainer = terminal.transform.parent.parent.Find("Canvas").Find("MainContainer");
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
        internal void AddCodeCommands()
        {
            foreach (TerminalKeyword keyword in terminalKeywords.Values)
            {
                if (keyword.word.Length == 2)
                {
                    AddCommand(new CodeCommand(keyword.word));
                }
            }
        }
        internal void AddCompatibilityCommands()
        {
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.malco.lethalcompany.moreshipupgrades"))
            {
                Logger.LogInfo("Adding Lategame Upgrades compatibility commands");
                AddCommand(new DummyCommand("Lategame Upgrades", "lategame", "Displays information related to the Lategame Upgrades mod."));
                AddCommand(new DummyCommand("Lategame Upgrades", "lgu", "Displays the purchaseable upgrades from the Lategame Upgrades store."));
            }
        }
        internal void ReplaceDefaultScreen(ref Terminal terminal)
        {
            TerminalNode node = terminal.terminalNodes.specialNodes[0];
            node.displayText = node.displayText.Replace("Halden Electronics Inc.", "lammas123").Replace("FORTUNE-9", "lammOS");

            TerminalNode resultNode = node.terminalOptions[0].result.terminalOptions[0].result;
            resultNode.displayText = resultNode.displayText.Substring(0, resultNode.displayText.IndexOf("Welcome to the FORTUNE-9 OS")) + newStartupText;
        }
        
        internal void HandleCommand(string input, ref Terminal terminal, ref TerminalNode node)
        {
            if (currentCommand == null)
            {
                int offset = input.IndexOf(' ');
                if (offset == -1)
                {
                    offset = input.Length;
                }

                Command.Handle(input.Substring(0, offset), input.Substring(offset == input.Length ? offset : offset + 1), ref terminal, ref node);
            }
            else if (currentCommand is ConfirmationCommand)
            {
                ConfirmationCommand.Handle(input, ref terminal, ref node);
                currentCommand = null;
            }
        }
        internal void HandleCommandResult(ref Terminal terminal, ref TerminalNode node)
        {
            if (!node.clearPreviousText)
            {
                node.clearPreviousText = true;
                node.displayText = terminal.screenText.text + "\n\n" + node.displayText;
            }

            node.maxCharactersToType = 99999;
            node.displayText = node.displayText.TrimStart('\n');

            if (node.displayText == ">")
            {
                return;
            }

            if (node.displayText == "")
            {
                node.displayText = ">";
                return;
            }

            if (!node.displayText.EndsWith(">"))
            {
                node.displayText = node.displayText.TrimEnd('\n') + "\n\n>";
            }
        }

        [HarmonyPatch(typeof(Terminal))]
        public static partial class TerminalPatches
        {
            [HarmonyPatch("Start")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostStart(ref Terminal __instance)
            {
                Instance.LoadVariables(ref __instance);

                Instance.SetupClock(ref __instance);

                if (!isSetup)
                {
                    Instance.AddCodeCommands();
                    Instance.AddCompatibilityCommands();
                    Instance.ReplaceDefaultScreen(ref __instance);
                    isSetup = true;
                }

                SyncedConfig.Setup();
            }

            [HarmonyPatch("ParsePlayerSentence")]
            [HarmonyPrefix]
            [HarmonyPriority(2147483647)]
            public static bool PreParsePlayerSentence(ref Terminal __instance, ref TerminalNode __result)
            {
                string input = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded);

                commandHistory.Add(input);
                while (commandHistory.Count > MaxCommandHistoryValue)
                {
                    commandHistory.RemoveAt(0);
                }

                TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
                node.displayText = "";
                node.clearPreviousText = true;
                node.terminalEvent = "";

                Instance.HandleCommand(input, ref __instance, ref node);

                __result = node;
                return false;
            }

            [HarmonyPatch("ParsePlayerSentence")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostParsePlayerSentence(ref Terminal __instance, ref TerminalNode __result)
            {
                Instance.HandleCommandResult(ref __instance, ref __result);
            }

            [HarmonyPatch("BuyItemsServerRpc")]
            [HarmonyPrefix]
            [HarmonyPriority(2147483647)]
            public static bool PreBuyItemsServerRpc(ref Terminal __instance, ref int[] boughtItems, ref int newGroupCredits, ref int numItemsInShip)
            {
                if (!__instance.IsHost)
                {
                    return true;
                }

                if (numItemsInShip > SyncedConfig.Instance.MaxDropshipItemsValue)
                {
                    Logger.LogWarning("The item amount purchased by a client goes over max dropship items, canceling purchase");
                    __instance.SyncGroupCreditsServerRpc(__instance.groupCredits, __instance.numberOfItemsInDropship);
                    return false;
                }

                int cost = 0;
                foreach (int itemIndex in boughtItems)
                {
                    foreach (string itemId in purchaseableItems.Keys)
                    {
                        if (purchaseableItems[itemId].index == itemIndex)
                        {
                            cost += GetItemCost(itemId);
                        }
                    }
                }

                if (__instance.groupCredits - cost != newGroupCredits)
                {
                    Logger.LogWarning("Items were bought by a client for the incorrect price");
                    if (__instance.groupCredits - cost < 0)
                    {
                        Logger.LogWarning("Resulting credits of this purchase is negative, canceling purchase");
                        __instance.SyncGroupCreditsServerRpc(__instance.groupCredits, __instance.numberOfItemsInDropship);
                        return false;
                    }
                    Logger.LogWarning("Resulting credits of this purchase is positive, fix and sync credits but allow purchase");
                    newGroupCredits = Mathf.Clamp(__instance.groupCredits - cost, 0, 10000000);
                }

                return true;
            }

            [HarmonyPatch("LoadNewNode")]
            [HarmonyPrefix]
            [HarmonyPriority(2147483647)]
            public static void PreLoadNewNode(ref Terminal __instance, ref TerminalNode node)
            {
                if (node.displayText.StartsWith("Welcome to the FORTUNE-9 OS"))
                {
                    startupNodeText = node.displayText;
                    node.displayText = newStartupText;
                    node.maxCharactersToType = 99999;
                    currentLoadedNode = "startup";
                }
                else if (node.displayText.StartsWith(">MOONS\n"))
                {
                    helpNodeText = node.displayText;
                    node.displayText = newStartupText;
                    node.maxCharactersToType = 99999;
                    currentLoadedNode = "help";
                }
            }

            [HarmonyPatch("LoadNewNode")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostLoadNewNode(ref Terminal __instance, ref TerminalNode node)
            {
                if (currentLoadedNode == "startup")
                {
                    node.displayText = startupNodeText;
                    startupNodeText = "";
                }
                else if (currentLoadedNode == "help")
                {
                    node.displayText = helpNodeText;
                    helpNodeText = "";
                }
            }

            [HarmonyPatch("BeginUsingTerminal")]
            [HarmonyPrefix]
            [HarmonyPriority(2147483647)]
            public static void PreBeginUsingTerminal()
            {
                commandHistoryIndex = -1;
            }

            [HarmonyPatch("TextChanged")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostTextChanged(ref Terminal __instance)
            {
                if (commandHistoryIndex != -1 && commandHistory[commandHistoryIndex] != __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded))
                {
                    commandHistoryIndex = -1;
                }
            }

            [HarmonyPatch("TextPostProcess")]
            [HarmonyPrefix]
            [HarmonyPriority(2147483647)]
            public static bool PreTextPostProcess(ref string __result, string modifiedDisplayText)
            {
                if (DisableTextPostProcessMethod.Value)
                {
                    __result = modifiedDisplayText;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(StartOfRound))]
        public static partial class StartOfRoundPatches
        {
            [HarmonyPatch("ChangeLevelServerRpc")]
            [HarmonyPrefix]
            [HarmonyPriority(2147483647)]
            public static bool PreChangeLevelServerRpc(ref StartOfRound __instance, ref int levelID, ref int newGroupCreditsAmount)
            {
                if (!__instance.IsHost)
                {
                    return true;
                }

                string moonId = null;
                foreach (string m in moons.Keys)
                {
                    if (moons[m].GetNode().buyRerouteToMoon == levelID)
                    {
                        moonId = m;
                        break;
                    }
                }
                if (moonId == null)
                {
                    return true;
                }

                Terminal terminal = FindObjectOfType<Terminal>();
                int cost = GetMoonCost(moonId);
                if (terminal.groupCredits - cost != newGroupCreditsAmount)
                {
                    Logger.LogWarning("Routing to moon was bought by a client for an incorrect price");

                    if (terminal.groupCredits - cost < 0)
                    {
                        Logger.LogWarning("Resulting credits of routing is negative, canceling purchase");
                        terminal.SyncGroupCreditsServerRpc(terminal.groupCredits, terminal.numberOfItemsInDropship);
                        return false;
                    }
                    Logger.LogWarning("Resulting credits of routing is positive, fix and sync credits but allow purchase");
                    newGroupCreditsAmount = Mathf.Clamp(terminal.groupCredits - cost, 0, 10000000);
                }

                return true;
            }

            [HarmonyPatch("BuyShipUnlockableServerRpc")]
            [HarmonyPrefix]
            [HarmonyPriority(2147483647)]
            public static bool PreBuyShipUnlockableServerRpc(ref StartOfRound __instance, ref int unlockableID, ref int newGroupCreditsAmount)
            {
                if (!__instance.IsHost)
                {
                    return true;
                }

                string unlockableId = null;
                foreach (string u in purchaseableUnlockables.Keys)
                {
                    if (purchaseableUnlockables[u].GetNode().shipUnlockableID == unlockableID)
                    {
                        unlockableId = u;
                        break;
                    }
                }
                if (unlockableId == null)
                {
                    return true;
                }

                Terminal terminal = FindObjectOfType<Terminal>();
                int cost = GetUnlockableCost(unlockableId);
                if (terminal.groupCredits - cost != newGroupCreditsAmount)
                {
                    Logger.LogWarning("Unlockable bought by a client for an incorrect price");
                    if (terminal.groupCredits - cost < 0)
                    {
                        Logger.LogWarning("Resulting credits of purchase is negative, canceling purchase");
                        terminal.SyncGroupCreditsServerRpc(terminal.groupCredits, terminal.numberOfItemsInDropship);
                        return false;
                    }
                    Logger.LogWarning("Resulting credits of purchase is positive, fix and sync credits but allow purchase");
                    newGroupCreditsAmount = Mathf.Clamp(terminal.groupCredits - cost, 0, 10000000);
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(HUDManager))]
        public static class HUDManagerPatches
        {
            [HarmonyPatch("SetClock")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void SetClock(ref HUDManager __instance)
            {
                if (ShowTerminalClock.Value)
                {
                    ClockText.text = __instance.clockNumber.text.Replace("\n", " ");
                    return;
                }
                ClockText.text = "";
            }

            [HarmonyPatch("FillEndGameStats")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void FillEndGameStats()
            {
                ClockText.text = "";
            }
        }

        public class HelpCommand : Command
        {
            public HelpCommand()
            {
                category = "Helpful";
                id = "help";
                description = "View helpful information about available commands. Why did you need help to know what help does?";
                AddShortcut("?", id);
                AddShortcut("h", id);
                args = "(Command)";
            }

            public void GenerateHelpPage(ref TerminalNode node)
            {
                Dictionary<string, List<Command>> categories = new();
                foreach (Command command in GetCommands())
                {
                    if (command.hidden)
                    {
                        continue;
                    }

                    if (!categories.ContainsKey(command.category))
                    {
                        categories[command.category] = new();
                    }

                    categories[command.category].Add(command);
                }

                foreach (string category in categories.Keys)
                {
                    node.displayText += "\n [" + category + "]\n";

                    foreach (Command command in categories[category])
                    {
                        node.displayText += ">" + command.id.ToUpper();
                        if (command.args != "")
                        {
                            node.displayText += " " + command.args;
                        }
                        node.displayText += "\n";
                    }
                }
            }
            public void GenerateCommandHelp(string commandId, Command command, ref TerminalNode node)
            {
                node.displayText = " [" + command.category + "]\n>" + commandId.ToUpper();
                if (command.args != "")
                {
                    node.displayText += " " + command.args;
                }

                List<string> shortcuts = new();
                foreach (string shortcut in GetShortcuts())
                {
                    if (GetCommandIdByShortcut(shortcut) == commandId)
                    {
                        shortcuts.Add(">" + shortcut.ToUpper());
                    }
                }
                if (shortcuts.Count != 0)
                {
                    node.displayText += "   (" + string.Join(", ", shortcuts) + ")";
                }

                node.displayText += "\n* " + command.description;
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    GenerateHelpPage(ref node);
                    return;
                }

                input = input.ToLower();
                if (IsShortcut(input))
                {
                    input = GetCommandIdByShortcut(input);
                }
                Command command = GetCommand(input);
                if (command == null)
                {
                    ErrorResponse("No command command found with id: '" + input + "'", ref node);
                    return;
                }

                GenerateCommandHelp(input, command, ref node);
            }
        }

        public class ShortcutsCommand : Command
        {
            public ShortcutsCommand()
            {
                category = "Helpful";
                id = "shortcuts";
                description = "View all of the shortcuts there are for commands.";
                AddShortcut("sh", id);
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                Dictionary<string, List<string>> categories = new();
                foreach (string shortcut in GetShortcuts())
                {
                    Command command = GetCommand(GetCommandIdByShortcut(shortcut));

                    if (command.hidden)
                    {
                        continue;
                    }

                    if (!categories.ContainsKey(command.category))
                    {
                        categories[command.category] = new();
                    }

                    categories[command.category].Add(">" + command.id.ToUpper() + "  ===  >" + shortcut.ToUpper() + "\n");
                }

                node.displayText = "Shortcuts:";
                foreach (string category in categories.Keys)
                {
                    node.displayText += "\n [" + category + "]\n";

                    foreach (string shortcut in categories[category])
                    {
                        node.displayText += shortcut;
                    }
                }
                if (node.displayText == "Shortcuts:")
                {
                    node.displayText += "\nThere are no shortcuts.";
                }
            }
        }

        public class MoonsCommand : Command
        {
            public MoonsCommand()
            {
                category = "Moons";
                id = "moons";
                description = "Lists the available moons you can travel to.";
                AddShortcut("ms", id);
            }

            public void GenerateMoonIndexWeather(string moonId, SelectableLevel level, ref TerminalNode node)
            {
                if (level.currentWeather != LevelWeatherType.None)
                {
                    node.displayText += " (" + level.currentWeather.ToString() + ")";
                }
            }
            public void GenerateMoonIndexCost(string moonId, SelectableLevel level, ref TerminalNode node)
            {
                int cost = GetMoonCost(moonId);
                if (cost != 0)
                {
                    node.displayText += "   $" + cost.ToString();
                }
            }
            public void GenerateMoonIndex(SelectableLevel level, ref TerminalNode node)
            {
                string moonId = moonNameToMoonNode[level.PlanetName.ToLower()];
                if (!moons.ContainsKey(moonId))
                {
                    return;
                }

                node.displayText += "\n*";
                if (ShowMinimumChars.Value)
                {
                    node.displayText += " (" + moons[moonId].shortestChars + ")";
                }
                node.displayText += level.PlanetName.Substring(level.PlanetName.IndexOf(" "));

                GenerateMoonIndexWeather(moonId, level, ref node);
                GenerateMoonIndexCost(moonId, level, ref node);
            }
            public void GenerateMoonsList(SelectableLevel[] levels, ref TerminalNode node)
            {
                foreach (SelectableLevel level in levels)
                {
                    GenerateMoonIndex(level, ref node);
                }
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                node.displayText = "Moons:\n* ";
                if (ShowMinimumChars.Value)
                {
                    node.displayText += "(" + moons["company"].shortestChars + ") ";
                }
                node.displayText += "The Company Building (" + ((int)(StartOfRound.Instance.companyBuyingRate * 100f)).ToString() + "%)";

                int cost = GetMoonCost("company");
                if (cost != 0)
                {
                    node.displayText += "   $" + cost.ToString();
                }

                GenerateMoonsList(terminal.moonsCatalogueList, ref node);
            }
        }

        public class MoonCommand : Command
        {
            public MoonCommand()
            {
                category = "Moons";
                id = "moon";
                description = "View information about a moon.";
                AddShortcut("m", id);
                args = "(Moon)";
            }

            public void DetailedPercentageOrRarity(int rarity, int totalRarity, ref TerminalNode node)
            {
                if (ShowPercentagesOrRarity.Value == "Percentage")
                {
                    node.displayText += (rarity * 100 / (float)totalRarity).ToString() + "%";
                    return;
                }
                node.displayText += rarity.ToString() + "/" + totalRarity.ToString();
            }

            public void GenerateDetailedWeather(SelectableLevel level, ref TerminalNode node)
            {
                node.displayText += "\n\nTypes of Weather:";
                bool hasWeather = false;
                foreach (RandomWeatherWithVariables weather in level.randomWeathers)
                {
                    if (weather.weatherType == LevelWeatherType.None)
                    {
                        continue;
                    }
                    node.displayText += "\n* " + weather.weatherType.ToString();
                    hasWeather = true;
                }
                if (!hasWeather)
                {
                    node.displayText += "\n* None";
                }
            }
            public void GenerateDetailedIndoors(SelectableLevel level, ref TerminalNode node)
            {
                node.displayText += "\n\nIndoor Size Multiplier: x" + level.factorySizeMultiplier.ToString() + "\n\nIndoor Type Rarities:";
                if (level.dungeonFlowTypes.Length == 0)
                {
                    node.displayText += "\n* 0: ";
                    DetailedPercentageOrRarity(1, 1, ref node);
                    return;
                }

                int dungeonRarity = 0;
                foreach (IntWithRarity rarity in level.dungeonFlowTypes)
                {
                    dungeonRarity += rarity.rarity;
                }
                foreach (IntWithRarity rarity in level.dungeonFlowTypes)
                {
                    if (rarity.rarity == 0)
                    {
                        continue;
                    }
                    node.displayText += "\n* " + rarity.id.ToString() + ": ";
                    DetailedPercentageOrRarity(rarity.rarity, dungeonRarity, ref node);
                }
            }
            public void GenerateDetailedScrap(SelectableLevel level, ref TerminalNode node)
            {
                node.displayText += "\n\nMin/Max Scrap: " + level.minScrap.ToString() + "/" + level.maxScrap.ToString() +
                    "\nMin/Max Scrap Value: " + level.minTotalScrapValue.ToString() + "/" + level.maxTotalScrapValue.ToString() +
                    "\n\nSpawnable Scrap:";

                int itemRarity = 0;
                foreach (SpawnableItemWithRarity item in level.spawnableScrap)
                {
                    itemRarity += item.rarity;
                }
                foreach (SpawnableItemWithRarity item in level.spawnableScrap)
                {
                    if (item.rarity == 0)
                    {
                        continue;
                    }
                    node.displayText += "\n* " + item.spawnableItem.itemName + "   ";
                    DetailedPercentageOrRarity(item.rarity, itemRarity, ref node);
                }
            }
            public void GenerateDetailedEntities(SelectableLevel level, ref TerminalNode node)
            {
                node.displayText += "\n\nMax Entity Power:\n* Daytime: " + level.maxDaytimeEnemyPowerCount.ToString() +
                    "\n* Indoor: " + level.maxEnemyPowerCount.ToString() +
                    "\n* Outdoor: " + level.maxOutsideEnemyPowerCount.ToString() +
                    "\n\nDaytime Entities: (Power : Max)";

                if (level.DaytimeEnemies.Count == 0)
                {
                    node.displayText += "\nNo daytime entities spawn on this moon.";
                }
                else
                {
                    int daytimeEntityRarity = 0;
                    foreach (SpawnableEnemyWithRarity entity in level.DaytimeEnemies)
                    {
                        daytimeEntityRarity += entity.rarity;
                    }
                    foreach (SpawnableEnemyWithRarity entity in level.DaytimeEnemies)
                    {
                        if (entity.rarity == 0)
                        {
                            continue;
                        }
                        node.displayText += "\n* " + entities[entity.enemyType.enemyName].displayName + " (" + entity.enemyType.PowerLevel + " : " + entity.enemyType.MaxCount + ")   ";
                        DetailedPercentageOrRarity(entity.rarity, daytimeEntityRarity, ref node);
                    }
                }

                node.displayText += "\n\nInside Entities: (Power : Max)";
                if (level.Enemies.Count == 0)
                {
                    node.displayText += "\nNo inside entities spawn on this moon.";
                }
                else
                {
                    int insideEntityRarity = 0;
                    foreach (SpawnableEnemyWithRarity entity in level.Enemies)
                    {
                        insideEntityRarity += entity.rarity;
                    }
                    foreach (SpawnableEnemyWithRarity entity in level.Enemies)
                    {
                        if (entity.rarity == 0)
                        {
                            continue;
                        }
                        node.displayText += "\n* " + entities[entity.enemyType.enemyName].displayName + " (" + entity.enemyType.PowerLevel + " : " + entity.enemyType.MaxCount + ")   ";
                        DetailedPercentageOrRarity(entity.rarity, insideEntityRarity, ref node);
                    }
                }

                node.displayText += "\n\nOutside Entities: (Power : Max)";
                if (level.Enemies.Count == 0)
                {
                    node.displayText += "\nNo outside entities spawn on this moon.";
                }
                else
                {
                    int outsideEntityRarity = 0;
                    foreach (SpawnableEnemyWithRarity entity in level.OutsideEnemies)
                    {
                        outsideEntityRarity += entity.rarity;
                    }
                    foreach (SpawnableEnemyWithRarity entity in level.OutsideEnemies)
                    {
                        if (entity.rarity == 0)
                        {
                            continue;
                        }
                        node.displayText += "\n* " + entities[entity.enemyType.enemyName].displayName + " (" + entity.enemyType.PowerLevel + " : " + entity.enemyType.MaxCount + ")   ";
                        DetailedPercentageOrRarity(entity.rarity, outsideEntityRarity, ref node);
                    }
                }
            }
            public void GenerateDetailedSafety(SelectableLevel level, ref TerminalNode node)
            {
                bool indoorsEvaluated = false;
                bool outdoorsEvaluated = false;
                for (float i = 0; i < 1; i += 0.05f)
                {
                    if (indoorsEvaluated && outdoorsEvaluated)
                    {
                        break;
                    }

                    if (!indoorsEvaluated && level.enemySpawnChanceThroughoutDay.Evaluate(i) > 0)
                    {
                        float timeNormalized = (i * 0.75f + 0.25f) * 24;
                        int hour = (int)timeNormalized;
                        string minute = ((int)((timeNormalized - hour) * 60)).ToString();
                        string end = "AM";
                        if (hour >= 12)
                        {
                            hour -= 12;
                            end = "PM";
                        }
                        node.displayText += "\n\nIndoors Safe Until Around: " + hour.ToString() + ":" + (minute.Length == 1 ? "0" : "") + minute + " " + end;
                        indoorsEvaluated = true;
                    }

                    if (!outdoorsEvaluated && level.outsideEnemySpawnChanceThroughDay.Evaluate(i) > 0)
                    {
                        float timeNormalized = (i * 0.75f + 0.25f) * 24;
                        int hour = (int)timeNormalized;
                        string minute = ((int)((timeNormalized - hour) * 60)).ToString();
                        string end = "AM";
                        if (hour >= 12)
                        {
                            hour -= 12;
                            end = "PM";
                        }
                        node.displayText += "\nOutdoors Safe Until Around: " + hour.ToString() + ":" + (minute.Length == 1 ? "0" : "") + minute + " " + end;
                        outdoorsEvaluated = true;
                    }
                }
            }

            public void GenerateDetailedResult(SelectableLevel level, ref TerminalNode node)
            {
                GenerateDetailedWeather(level, ref node);
                GenerateDetailedIndoors(level, ref node);
                GenerateDetailedScrap(level, ref node);
                GenerateDetailedEntities(level, ref node);
                GenerateDetailedSafety(level, ref node);
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                SelectableLevel level = null;
                if (input == "")
                {
                    level = StartOfRound.Instance.currentLevel;
                }
                else
                {
                    input = input.ToLower();
                    Moon moon = null;
                    string resultId = null;
                    foreach (string moonId in moons.Keys)
                    {
                        if (moonId.StartsWith(input))
                        {
                            moon = moons[moonId];
                            resultId = moonId;
                            break;
                        }
                    }

                    if (moon == null)
                    {
                        ErrorResponse("No moon goes by the name: '" + input + "'", ref node);
                        return;
                    }
                    if (CheckNotEnoughChars(input.Length, resultId.Length, ref node))
                    {
                        return;
                    }

                    level = moon.GetMoon();
                }

                node.displayText = level.PlanetName.Replace(" ", "-");
                if (level.currentWeather != LevelWeatherType.None)
                {
                    node.displayText += " (" + level.currentWeather.ToString() + ")";
                }

                int cost = GetMoonCost(moonNameToMoonNode[level.PlanetName.ToLower()]);
                if (cost != 0)
                {
                    node.displayText += "   $" + cost.ToString();
                }

                node.displayText += "\n\n" + level.LevelDescription;

                if (level.spawnEnemiesAndScrap)
                {
                    GenerateDetailedResult(level, ref node);
                }
            }
        }

        public class RouteCommand : ConfirmationCommand
        {
            public string moonId = null;

            public RouteCommand()
            {
                category = "Moons";
                id = "route";
                description = "Travel to the specified moon.";
                AddShortcut("r", id);
                args = "[Moon]";
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                if (StartOfRound.Instance.isChallengeFile)
                {
                    ErrorResponse("You cannot route to another moon while on a challenge moon save file.", ref node);
                    return;
                }

                StartOfRound startOfRound = FindObjectOfType<StartOfRound>();
                if (!startOfRound.inShipPhase)
                {
                    ErrorResponse("You are only able to route to a moon while in orbit.", ref node);
                    return;
                }
                if (startOfRound.travellingToNewLevel)
                {
                    ErrorResponse("You are already travelling elsewhere, please wait.", ref node);
                    return;
                }

                if (terminal.useCreditsCooldown)
                {
                    ErrorResponse("You're on a credit usage cooldown.", ref node);
                    return;
                }

                if (input == "")
                {
                    ErrorResponse("Please enter a moon to route the autopilot to.", ref node);
                    return;
                }

                input = input.ToLower();
                Moon moon = null;
                string resultId = null;
                foreach (string moonId in moons.Keys)
                {
                    if (moonId.StartsWith(input))
                    {
                        moon = moons[moonId];
                        resultId = moonId;
                        break;
                    }
                }
                if (moon == null)
                {
                    ErrorResponse("No moon goes by the name: '" + input + "'", ref node);
                    return;
                }
                if (CheckNotEnoughChars(input.Length, resultId.Length, ref node))
                {
                    return;
                }

                TerminalNode terminalNode = moon.GetNode();
                if (StartOfRound.Instance.levels[terminalNode.buyRerouteToMoon] == StartOfRound.Instance.currentLevel)
                {
                    ErrorResponse("You are already at that moon.", ref node);
                    return;
                }

                moonId = resultId;

                if (ShowCommandConfirmationsValue)
                {
                    currentCommand = this;
                    node.displayText = "Would you like to route to " + moon.GetMoon().PlanetName + " for $" + GetMoonCost(resultId).ToString() + "?\nType CONFIRM to confirm routing.";
                    return;
                }

                Route(ref terminal, ref node);
                moonId = null;
            }

            public void Route(ref Terminal terminal, ref TerminalNode node)
            {
                Moon moon = moons[moonId];

                int cost = GetMoonCost(moonId);
                if (terminal.groupCredits < cost)
                {
                    ErrorResponse("You do not have enough credits to go to that moon.", ref node);
                    return;
                }

                if (terminal.IsHost)
                {
                    FindObjectOfType<StartOfRound>().ChangeLevelServerRpc(moon.GetNode().buyRerouteToMoon, terminal.groupCredits - cost);
                }
                else
                {
                    terminal.groupCredits -= cost;
                    FindObjectOfType<StartOfRound>().ChangeLevelServerRpc(moon.GetNode().buyRerouteToMoon, terminal.groupCredits);
                }
                node.displayText = "Routing autopilot to " + moon.GetMoon().PlanetName + ".";
            }

            public override void Confirmed(ref Terminal terminal, ref TerminalNode node)
            {
                node.clearPreviousText = false;

                Route(ref terminal, ref node);

                moonId = null;
            }
        }

        public class StoreCommand : Command
        {
            public StoreCommand()
            {
                category = "Items";
                id = "store";
                description = "View the items available to buy.";
                AddShortcut("x", id);
            }

            public void GeneratePurchaseableItems(ref Terminal terminal, ref TerminalNode node)
            {
                node.displayText = "Welcome to the Company store!\n" + terminal.numberOfItemsInDropship.ToString() + "/" + SyncedConfig.Instance.MaxDropshipItemsValue + " items are in the dropship.\nUse the BUY command to buy any items listed here:\n------------------------------";
                foreach (PurchaseableItem purchaseableItem in purchaseableItems.Values)
                {
                    Item item = purchaseableItem.GetItem(ref terminal);
                    node.displayText += "\n* ";
                    if (ShowMinimumChars.Value)
                    {
                        node.displayText += "(" + purchaseableItem.shortestChars + ") ";
                    }

                    int salePercentage = purchaseableItem.GetSalePercentage(ref terminal);
                    node.displayText += item.itemName + "   $" + GetItemCost(item.itemName.ToLower()).ToString();
                    if (salePercentage != 100)
                    {
                        node.displayText += "     " + (100 - salePercentage).ToString() + "% OFF";
                    }
                }
            }
            public void GenerateUpgrades(ref TerminalNode node)
            {
                List<PurchaseableUnlockable> upgrades = new();
                foreach (PurchaseableUnlockable unlockable in purchaseableUnlockables.Values)
                {
                    if (unlockable.GetUnlockable().alwaysInStock && !unlockable.GetUnlockable().hasBeenUnlockedByPlayer && !unlockable.GetUnlockable().alreadyUnlocked)
                    {
                        upgrades.Add(unlockable);
                    }
                }
                upgrades.Sort((x, y) => GetUnlockableCost(x.GetNode().creatureName.ToLower()) - GetUnlockableCost(y.GetNode().creatureName.ToLower()));

                node.displayText += "\n\nShip Upgrades:\n------------------------------";
                if (upgrades.Count == 0)
                {
                    node.displayText += "\nAll ship upgrades have been purchased.";
                    return;
                }

                foreach (PurchaseableUnlockable unlockable in upgrades)
                {
                    node.displayText += "\n* ";
                    if (ShowMinimumChars.Value)
                    {
                        node.displayText += "(" + unlockable.shortestChars + ") ";
                    }
                    node.displayText += unlockable.GetNode().creatureName + "   $" + GetUnlockableCost(unlockable.GetNode().creatureName.ToLower()).ToString();
                }
            }
            public void GenerateDecorSelection(ref Terminal terminal, ref TerminalNode node)
            {
                node.displayText += "\n\nShip Decor:\n------------------------------";
                bool decorAvailable = false;
                foreach (TerminalNode decorNode in terminal.ShipDecorSelection)
                {
                    PurchaseableUnlockable purchaseableUnlockable = purchaseableUnlockables[decorNode.creatureName.ToLower()];
                    if (!purchaseableUnlockable.GetUnlockable().hasBeenUnlockedByPlayer && !purchaseableUnlockable.GetUnlockable().alreadyUnlocked)
                    {
                        node.displayText += "\n* ";
                        if (ShowMinimumChars.Value)
                        {
                            node.displayText += "(" + purchaseableUnlockable.shortestChars + ") ";
                        }
                        node.displayText += decorNode.creatureName + "   $" + GetUnlockableCost(purchaseableUnlockable.GetNode().creatureName.ToLower()).ToString();

                        decorAvailable = true;
                    }
                }
                if (!decorAvailable)
                {
                    node.displayText += "\nNo decor items available.";
                    return;
                }
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                GeneratePurchaseableItems(ref terminal, ref node);
                GenerateUpgrades(ref node);
                GenerateDecorSelection(ref terminal, ref node);
            }
        }

        public class BuyCommand : ConfirmationCommand
        {
            public PurchaseableItem purchaseableItem = null;
            public int amount = 0;
            public PurchaseableUnlockable purchaseableUnlockable = null;
            
            public BuyCommand()
            {
                category = "Items";
                id = "buy";
                description = "Purchase any available item in the store.";
                AddShortcut("b", id);
                args = "[Item] (Amount)";
            }

            public void ParseInput(string input, out string itemInput, out int amount)
            {
                int split = input.LastIndexOf(' ');
                if (split == -1)
                {
                    itemInput = input;
                    amount = 1;
                    return;
                }

                if (int.TryParse(input.Substring(split + 1), out amount))
                {
                    itemInput = input.Substring(0, split);
                    return;
                }

                itemInput = input;
                amount = 1;
            }

            public void PurchaseItem(string input, string itemInput, int amount, ref Terminal terminal, ref TerminalNode node)
            {
                PurchaseableItem purchaseableItem = null;
                foreach (PurchaseableItem item in purchaseableItems.Values)
                {
                    if (item.GetItem(ref terminal).itemName.ToLower().StartsWith(itemInput))
                    {
                        purchaseableItem = item;
                        break;
                    }
                }
                if (purchaseableItem == null)
                {
                    PurchaseUnlockable(input, ref terminal, ref node);
                    return;
                }
                if (CheckNotEnoughChars(itemInput.Length, purchaseableItem.GetItem(ref terminal).itemName.Length, ref node))
                {
                    return;
                }

                if (amount < 1)
                {
                    ErrorResponse("You must buy at least 1 item when purchasing items.", ref node);
                    return;
                }

                this.purchaseableItem = purchaseableItem;
                this.amount = amount;

                if (ShowCommandConfirmationsValue)
                {
                    currentCommand = this;
                    node.displayText = "Would you like to purchase " + purchaseableItem.GetItem(ref terminal).itemName + " x" + amount.ToString() + " for $" + (GetItemCost(purchaseableItem.GetItem(ref terminal).itemName.ToLower()) * amount).ToString() + "?\nType CONFIRM to confirm your purchase.";
                    return;
                }
                
                TryPurchaseItem(ref terminal, ref node);
                purchaseableItem = null;
                amount = 0;
            }
            public void PurchaseUnlockable(string input, ref Terminal terminal, ref TerminalNode node)
            {
                PurchaseableUnlockable purchaseableUnlockable = null;
                foreach (PurchaseableUnlockable unlockable in purchaseableUnlockables.Values)
                {
                    if (unlockable.GetNode().creatureName.ToLower().StartsWith(input))
                    {
                        purchaseableUnlockable = unlockable;
                        break;
                    }
                }
                if (purchaseableUnlockable == null)
                {
                    ErrorResponse("No purchaseable item or unlockable goes by the name: '" + input + "'", ref node);
                    return;
                }
                if (CheckNotEnoughChars(input.Length, purchaseableUnlockable.GetNode().creatureName.Length, ref node))
                {
                    return;
                }

                if (!purchaseableUnlockable.GetUnlockable().alwaysInStock && !terminal.ShipDecorSelection.Contains(purchaseableUnlockable.GetUnlockable().shopSelectionNode))
                {
                    ErrorResponse("This unlockable is not for sale.", ref node);
                    return;
                }
                if (purchaseableUnlockable.GetUnlockable().hasBeenUnlockedByPlayer || purchaseableUnlockable.GetUnlockable().alreadyUnlocked)
                {
                    ErrorResponse("You already have this unlockable.", ref node);
                    return;
                }

                this.purchaseableUnlockable = purchaseableUnlockable;

                if (ShowCommandConfirmationsValue)
                {
                    currentCommand = this;
                    node.displayText = "Would you like to purchase a " + purchaseableUnlockable.GetNode().creatureName + " for $" + GetUnlockableCost(purchaseableUnlockable.GetNode().creatureName.ToLower()).ToString() + "?\nType CONFIRM to confirm your purchase.";
                    return;
                }

                TryPurchaseUnlockable(ref terminal, ref node);
                purchaseableUnlockable = null;
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    ErrorResponse("Please enter an item to purchase.", ref node);
                    return;
                }
                if (terminal.useCreditsCooldown)
                {
                    ErrorResponse("You're on a credit usage cooldown.", ref node);
                    return;
                }

                input = input.ToLower();
                ParseInput(input, out string itemInput, out int amount);

                PurchaseItem(input, itemInput, amount, ref terminal, ref node);
            }

            public void TryPurchaseItem(ref Terminal terminal, ref TerminalNode node)
            {
                int cost = GetItemCost(purchaseableItem.GetItem(ref terminal).itemName.ToLower());
                if (terminal.groupCredits < cost * amount)
                {
                    ErrorResponse("You do not have enough credits to purchase that item.", ref node);
                    return;
                }
                if (amount + terminal.numberOfItemsInDropship > SyncedConfig.Instance.MaxDropshipItemsValue)
                {
                    ErrorResponse("There is not enough space on the dropship for these items, there are currently " + terminal.numberOfItemsInDropship.ToString() + "/" + SyncedConfig.Instance.MaxDropshipItemsValue.ToString() + " items en route.", ref node);
                    return;
                }

                for (int i = 0; i < amount; i++)
                {
                    terminal.orderedItemsFromTerminal.Add(purchaseableItem.index);
                    terminal.numberOfItemsInDropship++;
                    terminal.groupCredits = Mathf.Clamp(terminal.groupCredits - cost, 0, 10000000);

                    if (!terminal.IsHost && terminal.orderedItemsFromTerminal.Count == 12)
                    {
                        terminal.BuyItemsServerRpc(terminal.orderedItemsFromTerminal.ToArray(), terminal.groupCredits, terminal.numberOfItemsInDropship);
                        terminal.orderedItemsFromTerminal.Clear();
                    }
                }

                if (terminal.IsServer)
                {
                    terminal.SyncGroupCreditsClientRpc(terminal.groupCredits, terminal.numberOfItemsInDropship);
                }
                else
                {
                    if (terminal.orderedItemsFromTerminal.Count > 1)
                    {
                        terminal.BuyItemsServerRpc(terminal.orderedItemsFromTerminal.ToArray(), terminal.groupCredits, terminal.numberOfItemsInDropship);
                        terminal.orderedItemsFromTerminal.Clear();
                    }
                }

                node.displayText = "Purchased " + purchaseableItem.GetItem(ref terminal).itemName + " x" + amount.ToString() + " for $" + (cost * amount).ToString() + ".";
                node.playSyncedClip = terminalSyncedSounds["buy"];
            }
            public void TryPurchaseUnlockable(ref Terminal terminal, ref TerminalNode node)
            {
                int cost = GetUnlockableCost(purchaseableUnlockable.GetNode().creatureName.ToLower());
                if (terminal.groupCredits < cost)
                {
                    ErrorResponse("You do not have enough credits to purchase that unlockable.", ref node);
                    return;
                }
                if ((!StartOfRound.Instance.inShipPhase && !StartOfRound.Instance.shipHasLanded) || StartOfRound.Instance.shipAnimator.GetCurrentAnimatorStateInfo(0).tagHash != Animator.StringToHash("ShipIdle"))
                {
                    ErrorResponse("You cannot purchase that unlockable currently.", ref node);
                    return;
                }

                HUDManager.Instance.DisplayTip("Tip", "Press B to move and place objects in the ship, E to cancel.", false, true, "LC_MoveObjectsTip");
                if (terminal.IsHost)
                {
                    FindObjectOfType<StartOfRound>().BuyShipUnlockableServerRpc(purchaseableUnlockable.GetNode().shipUnlockableID, terminal.groupCredits - cost);
                }
                else
                {
                    terminal.groupCredits = Mathf.Clamp(terminal.groupCredits - cost, 0, 10000000);
                    FindObjectOfType<StartOfRound>().BuyShipUnlockableServerRpc(purchaseableUnlockable.GetNode().shipUnlockableID, terminal.groupCredits);
                }

                node.displayText = "Purchased " + purchaseableUnlockable.GetNode().creatureName + " for $" + cost.ToString() + ".";
                node.playSyncedClip = terminalSyncedSounds["buy"];
            }

            public override void Confirmed(ref Terminal terminal, ref TerminalNode node)
            {
                node.clearPreviousText = false;
                if (purchaseableItem != null)
                {
                    TryPurchaseItem(ref terminal, ref node);
                    purchaseableItem = null;
                    amount = 0;
                }
                else if (purchaseableUnlockable != null)
                {
                    TryPurchaseUnlockable(ref terminal, ref node);
                    purchaseableUnlockable = null;
                }
            }
        }

        public class StorageCommand : Command
        {
            public StorageCommand()
            {
                category = "Items";
                id = "storage";
                description = "View unlockables stored away in storage.";
                AddShortcut("st", id);
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                node.displayText = "Use the RETRIEVE command to take unlockables out of storage.\nStored Unlockables:";
                foreach (PurchaseableUnlockable unlockable in purchaseableUnlockables.Values)
                {
                    if (unlockable.GetUnlockable().inStorage)
                    {
                        node.displayText += "\n* ";
                        if (ShowMinimumChars.Value)
                        {
                            node.displayText += "(" + unlockable.shortestChars + ") ";
                        }
                        node.displayText += unlockable.GetNode().creatureName;
                    }
                }
                if (node.displayText == "Use the RETRIEVE command to take unlockables out of storage.\nStored Unlockables:")
                {
                    node.displayText += "\nThere are no unlockables in storage.";
                }
            }
        }

        public class RetrieveCommand : Command
        {
            public RetrieveCommand()
            {
                category = "Items";
                id = "retrieve";
                description = "Retrieve an unlockable from storage.";
                AddShortcut("re", id);
                args = "[Item]";
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    ErrorResponse("Please enter an unlockable to take out of storage.", ref node);
                    return;
                }

                input = input.ToLower();
                PurchaseableUnlockable purchaseableUnlockable = null;
                foreach (PurchaseableUnlockable unlockable in purchaseableUnlockables.Values)
                {
                    if (unlockable.GetNode().creatureName.ToLower().StartsWith(input))
                    {
                        purchaseableUnlockable = unlockable;
                        break;
                    }
                }
                if (purchaseableUnlockable == null)
                {
                    ErrorResponse("No unlockable goes by the name: '" + input + "'", ref node);
                    return;
                }
                if (CheckNotEnoughChars(input.Length, purchaseableUnlockable.GetNode().creatureName.Length, ref node))
                {
                    return;
                }

                if (!purchaseableUnlockable.GetUnlockable().inStorage)
                {
                    ErrorResponse("That unlockable is not in storage.", ref node);
                    return;
                }
                if (!purchaseableUnlockable.GetUnlockable().hasBeenUnlockedByPlayer && !purchaseableUnlockable.GetUnlockable().alreadyUnlocked)
                {
                    ErrorResponse("You do not own that unlockable.", ref node);
                    return;
                }

                FindObjectOfType<StartOfRound>().ReturnUnlockableFromStorageServerRpc(purchaseableUnlockable.GetNode().shipUnlockableID);
                node.displayText = "Returned unlockable from storage.";
            }
        }

        public class BestiaryCommand : Command
        {
            public BestiaryCommand()
            {
                category = "Informational";
                id = "bestiary";
                description = "List or view information about scanned entities.";
                AddShortcut("e", id);
                args = "(Entity)";
            }

            public void GenerateBestiaryPage(ref Terminal terminal, ref TerminalNode node)
            {
                node.displayText = "BESTIARY\n------------------------------";
                if (terminal.scannedEnemyIDs == null || terminal.scannedEnemyIDs.Count <= 0)
                {
                    node.displayText += "\nNo data collected on wildlife.";
                    return;
                }
                foreach (Entity entity in entities.Values)
                {
                    TerminalNode entityNode = entity.GetNode(ref terminal);
                    if (entityNode == null || !terminal.scannedEnemyIDs.Contains(entityNode.creatureFileID))
                    {
                        continue;
                    }

                    node.displayText += "\n* ";
                    if (terminal.newlyScannedEnemyIDs.Contains(entityNode.creatureFileID))
                    {
                        node.displayText += "(!) ";
                    }
                    if (ShowMinimumChars.Value)
                    {
                        node.displayText += "(" + entity.shortestChars + ") ";
                    }
                    node.displayText += entity.displayName;
                }
            }
            public void GenerateEntryPage(EnemyType entityType, ref TerminalNode node)
            {
                node.displayText = node.displayText.TrimEnd('\n');
                node.displayText += "\n\nPower: " + entityType.PowerLevel.ToString() +
                    "\nMax Count: " + entityType.MaxCount.ToString() +
                    "\nCan Die: " + entityType.canDie.ToString() +
                    "\nEntity Type: " + (entityType.isDaytimeEnemy ? "Daytime" : (entityType.isOutsideEnemy ? "Outside" : "Inside")) +
                    "\n\nStunnable: " + entityType.canBeStunned.ToString();

                if (entityType.canBeStunned)
                {
                    node.displayText += "\nStun Time Multiplier: " + entityType.stunTimeMultiplier.ToString() + "x";
                }
                if (!entityType.isDaytimeEnemy && !entityType.isOutsideEnemy)
                {
                    node.displayText += "\n\nDoor Open Speed: " + (1 / entityType.doorSpeedMultiplier).ToString() + "s";
                }
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    GenerateBestiaryPage(ref terminal, ref node);
                    return;
                }

                input = input.ToLower();
                EnemyType entityType = null;
                TerminalNode entityNode = null;
                string resultName = null;
                foreach (Entity entity in entities.Values)
                {
                    if (entity.entryIndex != -1 && entity.displayName.ToLower().StartsWith(input) && terminal.scannedEnemyIDs.Contains(entity.GetNode(ref terminal).creatureFileID))
                    {
                        entityType = entity.GetEntity();
                        entityNode = entity.GetNode(ref terminal);
                        resultName = entity.displayName;
                        break;
                    }
                }
                if (entityType == null)
                {
                    ErrorResponse("No entity exists with the name: '" + input + "'", ref node);
                    return;
                }
                if (CheckNotEnoughChars(input.Length, resultName.Length, ref node))
                {
                    return;
                }

                terminal.newlyScannedEnemyIDs.Remove(entityNode.creatureFileID);
                GenerateEntryPage(entityType, ref node);
            }
        }

        public class SigurdCommand : Command
        {
            public SigurdCommand()
            {
                category = "Informational";
                id = "sigurd";
                description = "View all of the logs from Sigurd you have collected.";
                AddShortcut("logs", id);
                AddShortcut("log", id);
                args = "(Log)";
            }

            public void GenerateSigurdPage(ref Terminal terminal, ref TerminalNode node)
            {
                node.displayText = "Sigurd's Log Entries\n------------------------------";
                if (terminal.unlockedStoryLogs == null || terminal.unlockedStoryLogs.Count <= 0)
                {
                    node.displayText = "ERROR, DATA HAS BEEN CORRUPTED OR OVERWRITTEN.";
                    return;
                }

                foreach (string logId in logs.Keys)
                {
                    if (terminal.unlockedStoryLogs.Contains(logs[logId].logIndex))
                    {
                        node.displayText += "\n* ";
                        if (terminal.newlyUnlockedStoryLogs.Contains(logs[logId].logIndex))
                        {
                            node.displayText += "(!) ";
                        }
                        if (ShowMinimumChars.Value)
                        {
                            node.displayText += "(" + logs[logId].shortestChars + ") ";
                        }
                        node.displayText += logId;
                    }
                }
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    GenerateSigurdPage(ref terminal, ref node);
                    return;
                }

                input = input.ToLower();
                Log log = null;
                foreach (string logId in logs.Keys)
                {
                    if (logId.ToLower().StartsWith(input) && terminal.unlockedStoryLogs.Contains(logs[logId].logIndex))
                    {
                        log = logs[logId];
                        break;
                    }
                }
                if (log == null)
                {
                    ErrorResponse("No log exists with the name: '" + input + "'", ref node);
                    return;
                }
                if (CheckNotEnoughChars(input.Length, log.GetNode(ref terminal).creatureName.Length, ref node))
                {
                    return;
                }

                terminal.newlyUnlockedStoryLogs.Remove(log.logIndex);
                node = log.GetNode(ref terminal);
            }
        }

        public class MonitorCommand : Command
        {
            public MonitorCommand()
            {
                category = "Radar";
                id = "monitor";
                description = "View the main monitor on the terminal.";
                AddShortcut("v", id);
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                foreach (CompatibleNoun cn in terminalKeywords["view"].compatibleNouns)
                {
                    if (cn.noun.word == "monitor")
                    {
                        node.displayTexture = cn.result.displayTexture;
                        node.persistentImage = cn.result.persistentImage;
                        return;
                    }
                }
                ErrorResponse("Failed to view monitor.", ref node);
            }
        }

        public class TargetsCommand : Command
        {
            public TargetsCommand()
            {
                category = "Radar";
                id = "targets";
                description = "View all of the radar targets you can monitor.";
                AddShortcut("rt", id);
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                node.displayText = "Radar Targets:";
                for (int i = 0; i < StartOfRound.Instance.mapScreen.radarTargets.Count; i++)
                {
                    if (StartOfRound.Instance.mapScreen.radarTargets[i].name.StartsWith("Player #"))
                    {
                        continue;
                    }
                    node.displayText += "\n* " + StartOfRound.Instance.mapScreen.radarTargets[i].name;
                }
            }
        }

        public class SwitchCommand : Command
        {
            public SwitchCommand()
            {
                category = "Radar";
                id = "switch";
                description = "Switch to another player or radar booster on the monitor.";
                AddShortcut("s", id);
                args = "(Radar target)";
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    StartOfRound.Instance.mapScreen.SwitchRadarTargetAndSync((StartOfRound.Instance.mapScreen.targetTransformIndex + 1) % StartOfRound.Instance.mapScreen.radarTargets.Count);
                    return;
                }
                input = input.ToLower();
                int target = -1;
                for (int i = 0; i < StartOfRound.Instance.mapScreen.radarTargets.Count; i++)
                {
                    if (StartOfRound.Instance.mapScreen.radarTargets[i].name.ToLower().StartsWith(input))
                    {
                        target = i;
                        break;
                    }
                }
                if (target == -1)
                {
                    ErrorResponse("No radar target goes by the name: '" + input + "'", ref node);
                    return;
                }
                if (CheckNotEnoughChars(input.Length, StartOfRound.Instance.mapScreen.radarTargets[target].name.Length, ref node))
                {
                    return;
                }

                StartOfRound.Instance.mapScreen.SwitchRadarTargetAndSync(target);
            }
        }

        public class PingCommand : Command
        {
            public PingCommand()
            {
                category = "Radar";
                id = "ping";
                description = "Ping a radar booster playing a sound.";
                AddShortcut("p", id);
                args = "(Radar booster)";
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    if (!StartOfRound.Instance.mapScreen.radarTargets[StartOfRound.Instance.mapScreen.targetTransformIndex].isNonPlayer)
                    {
                        ErrorResponse("You can only ping radar boosters.", ref node);
                        return;
                    }
                    StartOfRound.Instance.mapScreen.PingRadarBooster(StartOfRound.Instance.mapScreen.targetTransformIndex);
                    return;
                }

                input = input.ToLower();
                int target = -1;
                for (int i = 0; i < StartOfRound.Instance.mapScreen.radarTargets.Count; i++)
                {
                    if (StartOfRound.Instance.mapScreen.radarTargets[i].name.ToLower().StartsWith(input))
                    {
                        target = i;
                        break;
                    }
                }
                if (target == -1)
                {
                    ErrorResponse("No radar booster goes by the name: '" + input + "'", ref node);
                    return;
                }
                if (CheckNotEnoughChars(input.Length, StartOfRound.Instance.mapScreen.radarTargets[target].name.Length, ref node))
                {
                    return;
                }

                if (!StartOfRound.Instance.mapScreen.radarTargets[target].isNonPlayer)
                {
                    ErrorResponse("You can only ping radar boosters.", ref node);
                    return;
                }
                StartOfRound.Instance.mapScreen.PingRadarBooster(target);
            }
        }

        public class FlashCommand : Command
        {
            public FlashCommand()
            {
                category = "Radar";
                id = "flash";
                description = "Flash a radar booster blinding and stunning nearby crew and entities temporarily.";
                AddShortcut("f", id);
                args = "(Radar booster)";
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    if (!StartOfRound.Instance.mapScreen.radarTargets[StartOfRound.Instance.mapScreen.targetTransformIndex].isNonPlayer)
                    {
                        ErrorResponse("You can only flash radar boosters.", ref node);
                        return;
                    }
                    StartOfRound.Instance.mapScreen.FlashRadarBooster(StartOfRound.Instance.mapScreen.targetTransformIndex);
                    return;
                }

                input = input.ToLower();
                int target = -1;
                for (int i = 0; i < StartOfRound.Instance.mapScreen.radarTargets.Count; i++)
                {
                    if (StartOfRound.Instance.mapScreen.radarTargets[i].name.ToLower().StartsWith(input))
                    {
                        target = i;
                        break;
                    }
                }
                if (target == -1)
                {
                    ErrorResponse("No radar booster goes by the name: '" + input + "'", ref node);
                    return;
                }
                if (CheckNotEnoughChars(input.Length, StartOfRound.Instance.mapScreen.radarTargets[target].name.Length, ref node))
                {
                    return;
                }

                if (!StartOfRound.Instance.mapScreen.radarTargets[target].isNonPlayer)
                {
                    ErrorResponse("You can only flash radar boosters.", ref node);
                    return;
                }
                StartOfRound.Instance.mapScreen.FlashRadarBooster(target);
            }
        }

        public class DoorCommand : Command
        {
            public DoorCommand()
            {
                category = "Interactive";
                id = "door";
                description = "Toggle the ship's door.";
                AddShortcut("d", id);
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                GameObject.Find(StartOfRound.Instance.hangarDoorsClosed ? "StartButton" : "StopButton").GetComponentInChildren<InteractTrigger>().onInteract.Invoke(GameNetworkManager.Instance.localPlayerController);
            }
        }

        public class LightsCommand : Command
        {
            public LightsCommand()
            {
                category = "Interactive";
                id = "lights";
                description = "Toggle the ship's lights.";
                AddShortcut("l", id);
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                StartOfRound.Instance.shipRoomLights.ToggleShipLights();
            }
        }

        public class TeleporterCommand : Command
        {
            public TeleporterCommand()
            {
                category = "Interactive";
                id = "tp";
                description = "Remotely activate the teleporter.";
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                foreach (ShipTeleporter teleporter in FindObjectsOfType<ShipTeleporter>())
                {
                    if (!teleporter.isInverseTeleporter)
                    {
                        if (teleporter.IsSpawned && teleporter.isActiveAndEnabled)
                        {
                            if (teleporter.buttonTrigger.interactable)
                            {
                                teleporter.PressTeleportButtonOnLocalClient();
                                return;
                            }
                            ErrorResponse("The teleporter is on cooldown.", ref node);
                            return;
                        }
                        ErrorResponse("The teleporter is in storage.", ref node);
                        return;
                    }
                }
                ErrorResponse("You do not own a teleporter.", ref node);
            }
        }

        public class InverseTeleporterCommand : Command
        {
            public InverseTeleporterCommand()
            {
                category = "Interactive";
                id = "itp";
                description = "Remotely activate the inverse teleporter.";
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                foreach (ShipTeleporter teleporter in FindObjectsOfType<ShipTeleporter>())
                {
                    if (teleporter.isInverseTeleporter)
                    {
                        if (teleporter.IsSpawned && teleporter.isActiveAndEnabled)
                        {
                            if (teleporter.buttonTrigger.interactable)
                            {
                                teleporter.PressTeleportButtonOnLocalClient();
                                return;
                            }
                            ErrorResponse("The inverse teleporter is on cooldown.", ref node);
                            return;
                        }
                        ErrorResponse("The inverse teleporter is in storage.", ref node);
                        return;
                    }
                }
                ErrorResponse("You do not own an inverse teleporter.", ref node);
            }
        }

        public class ScanCommand : Command
        {
            public ScanCommand()
            {
                category = "Other";
                id = "scan";
                description = "View the amount of items and total value remaining outside the ship.";
                AddShortcut("sc", id);
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                int shipCount = 0, shipValue = 0, indoorCount = 0, indoorValue = 0, outdoorCount = 0, outdoorValue = 0;
                string outdoorItems = "", indoorItems = "";

                foreach (GrabbableObject item in FindObjectsOfType<GrabbableObject>())
                {
                    if (!item.itemProperties.isScrap)
                    {
                        continue;
                    }

                    if (item.isInShipRoom)
                    {
                        shipCount++;
                        shipValue += item.scrapValue;
                        continue;
                    }

                    if (item.isInFactory)
                    {
                        indoorCount++;
                        indoorValue += item.scrapValue;
                        indoorItems += "\n* " + item.itemProperties.itemName + " $" + item.scrapValue.ToString();
                        continue;
                    }

                    outdoorCount++;
                    outdoorValue += item.scrapValue;
                    outdoorItems += "\n* " + item.itemProperties.itemName + " $" + item.scrapValue.ToString();
                }

                node.displayText = "Ship: " + shipCount.ToString() + " objects with a value of $" + shipValue.ToString() +
                    "\n\nIndoors: " + indoorCount.ToString() + " objects with a value of $" + indoorValue.ToString() + indoorItems +
                    "\n\nOutdoors: " + outdoorCount.ToString() + " objects with a value of $" + outdoorValue.ToString() + outdoorItems;
            }
        }

        public class TransmitCommand : Command
        {
            public TransmitCommand()
            {
                category = "Other";
                id = "transmit";
                description = "Transmit at most a 9 character message using the signal transmitter to your crew.";
                AddShortcut("t", id);
                args = "[9 characters]";
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                SignalTranslator signalTranslator = FindObjectOfType<SignalTranslator>();
                if (signalTranslator == null)
                {
                    ErrorResponse("You do not own a signal translator.", ref node);
                    return;
                }
                if (Time.realtimeSinceStartup - signalTranslator.timeLastUsingSignalTranslator <= 8f)
                {
                    ErrorResponse("The signal translator is still in use.", ref node);
                    return;
                }

                string text = input.Substring(0, Mathf.Min(input.Length, 9));
                if (string.IsNullOrEmpty(text))
                {
                    ErrorResponse("Please enter a 9 character or less message to send.", ref node);
                    return;
                }

                node.displayText = "Transmitting message...";
                if (!terminal.IsServer)
                {
                    signalTranslator.timeLastUsingSignalTranslator = Time.realtimeSinceStartup;
                }
                HUDManager.Instance.UseSignalTranslatorServerRpc(text);
            }
        }

        public class ClearCommand : Command
        {
            public ClearCommand()
            {
                category = "Other";
                id = "clr";
                description = "Clear all text from the terminal.";
                AddShortcut("c", id);
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                node.displayText = ">";
            }
        }

        public class CodesCommand : Command
        {
            public CodesCommand()
            {
                category = "Other";
                id = "codes";
                description = "View all of the alphanumeric codes and what objects they correspond to within the building.";
                AddShortcut("co", id);
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                node.displayText = "Alphanumeric Codes:";
                foreach (TerminalAccessibleObject tao in FindObjectsOfType<TerminalAccessibleObject>())
                {
                    node.displayText += "\n* " + tao.objectCode + ": ";
                    if (tao.isBigDoor)
                    {
                        node.displayText += "Door";
                        continue;
                    }
                    if (tao.codeAccessCooldownTimer == 3.2f)
                    {
                        node.displayText += "Landmine";
                        continue;
                    }
                    node.displayText += "Turret";
                }

                if (node.displayText == "Alphanumeric Codes:")
                {
                    node.displayText += "\nThere are no alphanumeric codes.";
                }
            }
        }

        public class ReloadCommand : Command
        {
            public ReloadCommand()
            {
                category = "Other";
                id = "reload";
                description = "Reloads the mod's config and the terminal, updating any potentially outdated information.";
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                Instance.LoadVariables(ref terminal);

                node.displayText = "Reloaded.";
            }
        }

        public class EjectCommand : ConfirmationCommand
        {
            public EjectCommand()
            {
                category = "Other";
                id = "eject";
                description = "Eject your crew into space.";
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                if (StartOfRound.Instance.isChallengeFile)
                {
                    ErrorResponse("You are unable to be ejected on challenge moons.", ref node);
                    return;
                }
                if (!StartOfRound.Instance.inShipPhase)
                {
                    ErrorResponse("You must be in orbit to be ejected.", ref node);
                    return;
                }
                if (StartOfRound.Instance.firingPlayersCutsceneRunning)
                {
                    ErrorResponse("Your crew is already being ejected.", ref node);
                    return;
                }

                if (ShowCommandConfirmationsValue)
                {
                    currentCommand = this;
                    node.displayText = "Are you sure you want to eject your crew? There is no going back.\nType CONFIRM to confirm routing.";
                    return;
                }

                StartOfRound.Instance.ManuallyEjectPlayersServerRpc();
            }

            public override void Confirmed(ref Terminal terminal, ref TerminalNode node)
            {
                StartOfRound.Instance.ManuallyEjectPlayersServerRpc();
            }
        }

        public class MacrosCommand : Command
        {
            public MacrosCommand()
            {
                category = "Macros";
                id = "macros";
                description = "Lists all of your macros.";
                AddShortcut("l-", id);
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                node.displayText = "Your Macros:";
                foreach (string macroId in Macros.GetMacroIds())
                {
                    node.displayText += "\n* " + macroId;
                }
                if (node.displayText == "Your Macros:")
                {
                    node.displayText += "\nYou have no macros, create one with the >CREATEMACRO command.";
                }
            }
        }

        public class RunMacroCommand : Command
        {
            public RunMacroCommand()
            {
                category = "Macros";
                id = "runmacro";
                description = "Run one of your macros.";
                AddShortcut("-", id);
                args = "[Macro]";
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                if (runningMacro)
                {
                    ErrorResponse("You cannot run macros within macros.", ref node);
                    return;
                }

                if (input == "")
                {
                    ErrorResponse("You must enter a macro id.", ref node);
                    return;
                }

                input = input.ToLower();
                if (!Macros.HasMacro(input))
                {
                    ErrorResponse("No macro exists with that id.", ref node);
                    return;
                }

                runningMacro = true;
                string output = ">";
                foreach (string instruction in Macros.GetMacro(input))
                {
                    TerminalNode dummyNode = ScriptableObject.CreateInstance<TerminalNode>();
                    dummyNode.displayText = "";
                    dummyNode.clearPreviousText = true;
                    dummyNode.terminalEvent = "";

                    output += instruction;

                    Instance.HandleCommand(instruction, ref terminal, ref dummyNode);
                    dummyNode.clearPreviousText = true;
                    Instance.HandleCommandResult(ref terminal, ref dummyNode);

                    output += "\n\n" + dummyNode.displayText;

                    terminal.LoadNewNode(dummyNode);
                }
                runningMacro = false;

                node.displayText = "Executed the instructions of the macro with the id '" + input + "'. Macro output:\n\n" + output;
            }
        }

        public class CreateMacroCommand : Command
        {
            public CreateMacroCommand()
            {
                category = "Macros";
                id = "createmacro";
                description = "Create a new macro.";
                AddShortcut("c-", id);
                args = "[Macro] [Instructions split by ';']";
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    ErrorResponse("You must enter a macro id.", ref node);
                    return;
                }

                int offset = input.IndexOf(' ');
                if (offset == -1)
                {
                    ErrorResponse("You must enter intructions for the macro to run, each one separated by ';'.", ref node);
                    return;
                }

                string macroId = input.Substring(0, offset).ToLower();
                if (Macros.HasMacro(macroId))
                {
                    ErrorResponse("A macro already exists with that id.", ref node);
                    return;
                }

                List<string> instructions = new(input.Substring(offset + 1).Split(';'));
                List<string> finalInstructions = new();
                foreach (string instruction in instructions)
                {
                    string trimmedInstruction = instruction.TrimStart(' ').TrimEnd(' ');
                    if (trimmedInstruction != "")
                    {
                        finalInstructions.Add(trimmedInstruction);
                    }
                }

                if (finalInstructions.Count == 0)
                {
                    ErrorResponse("You must enter intructions for the macro to run, each one separated by ';'.", ref node);
                    return;
                }

                Macros.AddMacro(macroId, finalInstructions);
                Macros.Save();

                node.displayText = "Created a new macro with the id '" + macroId + "' with the following instructions:";
                foreach (string instruction in finalInstructions)
                {
                    node.displayText += "\n* " + instruction;
                }
            }
        }

        public class InfoMacroCommand : Command
        {
            public InfoMacroCommand()
            {
                category = "Macros";
                id = "macro";
                description = "View the instructions that are ran for one of your macros.";
                AddShortcut("m-", id);
                args = "[Macro]";
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    ErrorResponse("You must enter a macro id.", ref node);
                    return;
                }

                input = input.ToLower();
                if (!Macros.HasMacro(input))
                {
                    ErrorResponse("No macro exists with that id.", ref node);
                    return;
                }

                node.displayText = "Macro: " + input + "\nInstructions:";
                foreach (string instruction in Macros.GetMacro(input))
                {
                    node.displayText += "\n* " + instruction;
                }
            }
        }

        public class EditMacroCommand : Command
        {
            public EditMacroCommand()
            {
                category = "Macros";
                id = "editmacro";
                description = "Edit one of your macros.";
                AddShortcut("e-", id);
                args = "[Macro] [Instructions split by ';']";
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    ErrorResponse("You must enter a macro id.", ref node);
                    return;
                }

                int offset = input.IndexOf(' ');
                if (offset == -1)
                {
                    ErrorResponse("You must enter intructions for the macro to run, each one separated by ';'.", ref node);
                    return;
                }

                string macroId = input.Substring(0, offset).ToLower();
                if (!Macros.HasMacro(macroId))
                {
                    ErrorResponse("No macro exists with that id.", ref node);
                    return;
                }

                List<string> instructions = new(input.Substring(offset + 1).Split(';'));
                List<string> finalInstructions = new();
                foreach (string instruction in instructions)
                {
                    string trimmedInstruction = instruction.TrimStart(' ').TrimEnd(' ');
                    if (trimmedInstruction != "")
                    {
                        finalInstructions.Add(trimmedInstruction);
                    }
                }

                if (finalInstructions.Count == 0)
                {
                    ErrorResponse("You must enter intructions for the macro to run, each one separated by ';'.", ref node);
                    return;
                }

                Macros.ModifyMacro(macroId, finalInstructions);
                Macros.Save();

                node.displayText = "Edited the macro with the id '" + macroId + "' giving it the new instructions:";
                foreach (string instruction in finalInstructions)
                {
                    node.displayText += "\n* " + instruction;
                }
            }
        }

        public class DeleteMacroCommand : Command
        {
            public DeleteMacroCommand()
            {
                category = "Macros";
                id = "deletemacro";
                description = "Delete one of your macros.";
                AddShortcut("d-", id);
                args = "[Macro]";
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    ErrorResponse("You must enter a macro id.", ref node);
                    return;
                }

                input = input.ToLower();
                if (!Macros.HasMacro(input))
                {
                    ErrorResponse("No macro exists with that id.", ref node);
                    return;
                }

                Macros.RemoveMacro(input);
                Macros.Save();

                node.displayText = "Deleted the macro with the id '" + input + "'.";
            }
        }

        public class DebugCommand : Command
        {
            public DebugCommand()
            {
                id = "debug";
                description = "A debug command, how did you find this?";
                hidden = true;
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                if (!terminal.IsServer)
                {
                    return;
                }

                input = input.ToLower();
                int split = input.IndexOf(" ");
                if (split == -1)
                {
                    split = input.Length;
                }
                string args = input.Substring(split);
                input = input.Substring(0, split);

                switch (input)
                {
                    case "shortest_chars":
                        {
                            string text = "- Shortest Characters -\nEntities:";
                            foreach (Entity entity in entities.Values)
                            {
                                text += "\n* " + entity.displayName + "   (" + entity.shortestChars + ")";
                            }
                            text += "\n\nMoons:";
                            foreach (Moon moon in moons.Values)
                            {
                                text += "\n* " + moon.GetMoon().PlanetName + "   (" + moon.shortestChars + ")";
                            }
                            text += "\n\nPurchaseable Items:";
                            foreach (PurchaseableItem item in purchaseableItems.Values)
                            {
                                text += "\n* " + item.GetItem(ref terminal).itemName + "   (" + item.shortestChars + ")";
                            }
                            text += "\n\nPurchaseable Unlockables:";
                            foreach (PurchaseableUnlockable unlockable in purchaseableUnlockables.Values)
                            {
                                text += "\n* " + unlockable.GetNode().creatureName + "   (" + unlockable.shortestChars + ")";
                            }
                            text += "\n\nSigurd Logs:";
                            foreach (Log log in logs.Values)
                            {
                                text += "\n* " + log.GetNode(ref terminal).creatureName + "   (" + log.shortestChars + ")";
                            }
                            Logger.LogInfo(text);
                            break;
                        }
                    case "reboot":
                        {
                            ES3.Save("HasUsedTerminal", value: false, "LCGeneralSaveData");
                            break;
                        }
                    case "play_synced":
                        {
                            int index = -1;
                            if (args == "")
                            {
                                break;
                            }

                            int.TryParse(args, out index);
                            if (index < 0)
                            {
                                break;
                            }
                            if (index >= terminal.syncedAudios.Length)
                            {
                                break;
                            }
                            terminal.PlayTerminalAudioServerRpc(index);
                            break;
                        }
                    case "money":
                        {
                            int money = -1;
                            if (args == "")
                            {
                                money = 100000;
                            }
                            else
                            {
                                int.TryParse(args, out money);
                                if (money < 0)
                                {
                                    break;
                                }
                            }
                            terminal.groupCredits = Mathf.Min(money, 10000000);
                            terminal.SyncGroupCreditsServerRpc(terminal.groupCredits, terminal.numberOfItemsInDropship);
                            break;
                        }
                    case "keywords":
                        {
                            string keywords = "Keywords:";
                            for (int i = 0; i < terminal.terminalNodes.allKeywords.Length; i++)
                            {
                                keywords += "\n" + i.ToString() + ": " + terminal.terminalNodes.allKeywords[i].word;
                            }
                            Logger.LogInfo(keywords);
                            break;
                        }
                    case "keyword":
                        {
                            int index = -1;
                            int.TryParse(args, out index);
                            if (index < 0 || index >= terminal.terminalNodes.allKeywords.Length)
                            {
                                return;
                            }
                            TerminalKeyword k = terminal.terminalNodes.allKeywords[index];
                            string text = "Keyword: " + k.word +
                            "\n  accessTerminalObjects: " + k.accessTerminalObjects.ToString() +
                            "\n  defaultVerb: " + (k.defaultVerb == null ? "null" : k.defaultVerb.word) +
                            "\n  isVerb: " + k.isVerb.ToString() + "\n  specialKeywordResult:\n  displayText: " + k.specialKeywordResult.displayText +
                            "\n  clearPreviousText: " + k.specialKeywordResult.clearPreviousText.ToString() +
                            "\n  terminalEvent: " + k.specialKeywordResult.terminalEvent +
                            "\n  acceptAnything: " + k.specialKeywordResult.acceptAnything.ToString() +
                            "\n  isConfirmationNode: " + k.specialKeywordResult.isConfirmationNode.ToString() +
                            "\n  maxCharactersToType" + k.specialKeywordResult.maxCharactersToType.ToString() +

                            "\n  itemCost: " + k.specialKeywordResult.itemCost.ToString() +
                            "\n  buyItemIndex: " + k.specialKeywordResult.buyItemIndex.ToString() +
                            "\n  buyRerouteToMoon: " + k.specialKeywordResult.buyRerouteToMoon.ToString() +

                            "\n  buyUnlockable: " + k.specialKeywordResult.buyUnlockable.ToString() +
                            "\n  returnFromStorage: " + k.specialKeywordResult.returnFromStorage.ToString() +
                            "\n  shipUnlockableID: " + k.specialKeywordResult.shipUnlockableID.ToString() +

                            "\n  displayPlanetInfo: " + k.specialKeywordResult.displayPlanetInfo.ToString() +
                            "\n  storyLogFileID: " + k.specialKeywordResult.storyLogFileID.ToString() +
                            "\n  creatureFileID: " + k.specialKeywordResult.creatureFileID.ToString() +
                            "\n  creatureName: " + k.specialKeywordResult.creatureName +

                            "\n  playClip(null?): " + (k.specialKeywordResult.playClip == null ? "null" : "not null") +
                            "\n  playSyncedClip: " + k.specialKeywordResult.playSyncedClip.ToString() +
                            "\n  displayTexture(null?):" + (k.specialKeywordResult.displayTexture == null ? "null" : "not null") +
                            "\n  loadImageSlowly: " + k.specialKeywordResult.loadImageSlowly.ToString() +
                            "\n  persistentImage: " + k.specialKeywordResult.persistentImage.ToString() +
                            "\n  displayVideo(null?):" + (k.specialKeywordResult.displayVideo == null ? "null" : "not null") +

                            "\n  overrideOptions: " + k.specialKeywordResult.overrideOptions.ToString() +
                            "\n  terminalOptions:";
                            foreach (CompatibleNoun cn in k.specialKeywordResult.terminalOptions)
                            {
                                text += "\n    " + cn.noun.word;
                            }
                            Logger.LogInfo(text);
                            break;
                        }
                    case "nodes":
                        {
                            Logger.LogInfo("Nodes: " + terminal.terminalNodes.terminalNodes.Count.ToString());
                            break;
                        }
                    case "specialnodes":
                        {
                            Logger.LogInfo("Special Nodes: " + terminal.terminalNodes.specialNodes.Count.ToString());
                            break;
                        }
                    case "node":
                        {
                            int index = -1;
                            int.TryParse(args, out index);
                            if (index < 0 || index >= terminal.terminalNodes.terminalNodes.Count)
                            {
                                return;
                            }
                            TerminalNode n = terminal.terminalNodes.terminalNodes[index];
                            string text = "Node Info:\n  displayText: " + n.displayText +
                            "\n  clearPreviousText: " + n.clearPreviousText.ToString() +
                            "\n  terminalEvent: " + n.terminalEvent +
                            "\n  acceptAnything: " + n.acceptAnything.ToString() +
                            "\n  isConfirmationNode: " + n.isConfirmationNode.ToString() +
                            "\n  maxCharactersToType" + n.maxCharactersToType.ToString() +

                            "\n  itemCost: " + n.itemCost.ToString() +
                            "\n  buyItemIndex: " + n.buyItemIndex.ToString() +
                            "\n  buyRerouteToMoon: " + n.buyRerouteToMoon.ToString() +

                            "\n  buyUnlockable: " + n.buyUnlockable.ToString() +
                            "\n  returnFromStorage: " + n.returnFromStorage.ToString() +
                            "\n  shipUnlockableID: " + n.shipUnlockableID.ToString() +

                            "\n  displayPlanetInfo: " + n.displayPlanetInfo.ToString() +
                            "\n  storyLogFileID: " + n.storyLogFileID.ToString() +
                            "\n  creatureFileID: " + n.creatureFileID.ToString() +
                            "\n  creatureName: " + n.creatureName +

                            "\n  playClip(null?): " + (n.playClip == null ? "null" : "not null") +
                            "\n  playSyncedClip: " + n.playSyncedClip.ToString() +
                            "\n  displayTexture(null?):" + (n.displayTexture == null ? "null" : "not null") +
                            "\n  loadImageSlowly: " + n.loadImageSlowly.ToString() +
                            "\n  persistentImage: " + n.persistentImage.ToString() +
                            "\n  displayVideo(null?):" + (n.displayVideo == null ? "null" : "not null") +

                            "\n  overrideOptions: " + n.overrideOptions.ToString() +
                            "\n  terminalOptions:";
                            foreach (CompatibleNoun cn in n.terminalOptions)
                            {
                                text += "\n    " + cn.noun.word;
                            }
                            Logger.LogInfo(text);
                            break;
                        }
                    case "specialnode":
                        {
                            int index = -1;
                            int.TryParse(args, out index);
                            if (index < 0 || index >= terminal.terminalNodes.specialNodes.Count)
                            {
                                return;
                            }
                            TerminalNode n = terminal.terminalNodes.specialNodes[index];
                            string text = "Special Node Info:\n  displayText: " + n.displayText +
                            "\n  clearPreviousText: " + n.clearPreviousText.ToString() +
                            "\n  terminalEvent: " + n.terminalEvent +
                            "\n  acceptAnything: " + n.acceptAnything.ToString() +
                            "\n  isConfirmationNode: " + n.isConfirmationNode.ToString() +
                            "\n  maxCharactersToType" + n.maxCharactersToType.ToString() +

                            "\n  itemCost: " + n.itemCost.ToString() +
                            "\n  buyItemIndex: " + n.buyItemIndex.ToString() +
                            "\n  buyRerouteToMoon: " + n.buyRerouteToMoon.ToString() +

                            "\n  buyUnlockable: " + n.buyUnlockable.ToString() +
                            "\n  returnFromStorage: " + n.returnFromStorage.ToString() +
                            "\n  shipUnlockableID: " + n.shipUnlockableID.ToString() +

                            "\n  displayPlanetInfo: " + n.displayPlanetInfo.ToString() +
                            "\n  storyLogFileID: " + n.storyLogFileID.ToString() +
                            "\n  creatureFileID: " + n.creatureFileID.ToString() +
                            "\n  creatureName: " + n.creatureName +

                            "\n  playClip(null?): " + (n.playClip == null ? "null" : "not null") +
                            "\n  playSyncedClip: " + n.playSyncedClip.ToString() +
                            "\n  displayTexture(null?):" + (n.displayTexture == null ? "null" : "not null") +
                            "\n  loadImageSlowly: " + n.loadImageSlowly.ToString() +
                            "\n  persistentImage: " + n.persistentImage.ToString() +
                            "\n  displayVideo(null?):" + (n.displayVideo == null ? "null" : "not null") +

                            "\n  overrideOptions: " + n.overrideOptions.ToString() +
                            "\n  terminalOptions:";
                            foreach (CompatibleNoun cn in n.terminalOptions)
                            {
                                text += "\n    " + cn.noun.word;
                            }
                            Logger.LogInfo(text);
                            break;
                        }
                }
            }
        }

        public class CodeCommand : Command
        {
            public CodeCommand(string alphanumericCode)
            {
                id = alphanumericCode;
                description = "An alphanumeric code command.";
                hidden = true;
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                TerminalAccessibleObject[] array = FindObjectsOfType<TerminalAccessibleObject>();
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].objectCode == id)
                    {
                        array[i].CallFunctionFromTerminal();
                    }
                }

                terminal.codeBroadcastAnimator.SetTrigger("display");
                terminal.terminalAudio.PlayOneShot(terminal.codeBroadcastSFX, 1f);
            }
        }

        public class DummyCommand : Command
        {
            public DummyCommand(string category, string id, string description, string args = "")
            {
                this.category = category;
                this.id = id;
                this.description = description;
                this.args = args;
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                
            }
        }

        abstract public class ConfirmationCommand : Command
        {
            public abstract void Confirmed(ref Terminal terminal, ref TerminalNode node);

            internal static void Handle(string input, ref Terminal terminal, ref TerminalNode node)
            {
                if ("confirm".StartsWith(input))
                {
                    if (currentCommand.CheckNotEnoughChars(input.Length, 7, ref node))
                    {
                        return;
                    }
                    (currentCommand as ConfirmationCommand).Confirmed(ref terminal, ref node);
                    return;
                }
                node.displayText = ">";
            }
        }

        abstract public class Command
        {
            public string category = "Uncategorized";
            public string id { get; internal set; } = "";
            public string description = "";
            public string args = "";
            public bool hidden = false;

            public void ErrorResponse(string response, ref TerminalNode node)
            {
                node.displayText = response;
                node.playSyncedClip = terminalSyncedSounds["error"];
            }
            public bool CheckNotEnoughChars(int inputLength, int resultLength, ref TerminalNode node)
            {
                if (inputLength < Mathf.Min(CharsToAutocomplete.Value, resultLength))
                {
                    ErrorResponse("Not enough characters were input to autocomplete a result. The current requirement is " + CharsToAutocomplete.Value.ToString() + " characters.", ref node);
                    return true;
                }
                return false;
            }

            public abstract void Execute(ref Terminal terminal, string input, ref TerminalNode node);

            internal static void Handle(string commandId, string input, ref Terminal terminal, ref TerminalNode node)
            {
                if (IsShortcut(commandId))
                {
                    commandId = GetCommandIdByShortcut(commandId);
                    if (!runningMacro)
                    {
                        terminal.screenText.text = terminal.screenText.text.Substring(0, terminal.screenText.text.Length - terminal.textAdded) + commandId;
                        if (input != "")
                        {
                            terminal.screenText.text += " " + input;
                        }
                    }
                }

                Command command = GetCommand(commandId);
                if (command == null)
                {
                    node.displayText = "Unknown command: '" + commandId + "'\n\n>";
                    node.playSyncedClip = terminalSyncedSounds["error"];
                    return;
                }

                try
                {
                    command.Execute(ref terminal, input, ref node);
                }
                catch (Exception e)
                {
                    node = ScriptableObject.CreateInstance<TerminalNode>();
                    node.displayText = "An error occurred executing the command: '" + commandId + "'\n\n>";
                    node.clearPreviousText = true;
                    node.terminalEvent = "";
                    node.playSyncedClip = terminalSyncedSounds["error"];
                    Logger.LogError("An error occurred executing the command: '" + commandId + "'\n" + e.ToString());
                }
            }
        }

        [Serializable]
        public class SyncedConfig
        {
            public static readonly int DefaultMaxDropshipItems = 12;
            public int MaxDropshipItemsValue { get; internal set; }
            internal static ConfigEntry<int> MaxDropshipItems;

            internal Dictionary<string, float> MoonPriceMultipliers;
            internal Dictionary<string, float> ItemPriceMultipliers;
            internal Dictionary<string, float> UnlockablePriceMultipliers;

            internal static ConfigFile Config;

            internal static SyncedConfig Instance;
            internal SyncedConfig()
            {
                Instance = this;
                Config = new(Utility.CombinePaths(Paths.ConfigPath, "lammas123.lammOS.Synced.cfg"), false, MetadataHelper.GetMetadata(lammOS.Instance));
            }

            internal static void SetValues()
            {
                if (File.Exists(Config.ConfigFilePath))
                {
                    Config.Reload();
                }
                else
                {
                    Config.Clear();
                }

                MaxDropshipItems = Config.Bind("Synced - General", "MaxDropshipItems", DefaultMaxDropshipItems, "The maximum amount of items the dropship can have in it at a time.");
                if (MaxDropshipItems.Value < 1)
                {
                    MaxDropshipItems.Value = 1;
                }
                Instance.MaxDropshipItemsValue = MaxDropshipItems.Value;

                Instance.MoonPriceMultipliers = new();
                Instance.ItemPriceMultipliers = new();
                Instance.UnlockablePriceMultipliers = new();

                foreach (string moonId in moons.Keys)
                {
                    ConfigEntry<float> priceMultiplier = Config.Bind("Synced - Moon Price Multipliers", moonId + "_PriceMultiplier", moons[moonId].GetNode().itemCost == 0 ? 0f : 1f);
                    Instance.MoonPriceMultipliers.Add(moonId, priceMultiplier.Value >= 0 ? priceMultiplier.Value : 0);
                }

                foreach (string itemId in purchaseableItems.Keys)
                {
                    Terminal terminal = FindObjectOfType<Terminal>();
                    ConfigEntry<float> priceMultiplier = Config.Bind("Synced - Item Price Multipliers", itemId + "_PriceMultiplier", purchaseableItems[itemId].GetItem(ref terminal).creditsWorth == 0 ? 0f : 1f);
                    Instance.ItemPriceMultipliers.Add(itemId, priceMultiplier.Value >= 0 ? priceMultiplier.Value : 0);
                }

                foreach (string unlockableId in purchaseableUnlockables.Keys)
                {
                    ConfigEntry<float> priceMultiplier = Config.Bind("Synced - Unlockable Price Multipliers", unlockableId + "_PriceMultiplier", purchaseableUnlockables[unlockableId].GetNode().itemCost == 0 ? 0f : 1f);
                    Instance.UnlockablePriceMultipliers.Add(unlockableId, priceMultiplier.Value >= 0 ? priceMultiplier.Value : 0);
                }

                Config.Save();
            }

            internal static void Setup()
            {
                if (NetworkManager.Singleton.IsHost)
                {
                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("lammOS_OnRequestConfigSync", OnRequestSync);
                    return;
                }

                Instance.MaxDropshipItemsValue = DefaultMaxDropshipItems;
                Instance.MoonPriceMultipliers = new();
                Instance.ItemPriceMultipliers = new();
                Instance.UnlockablePriceMultipliers = new();

                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("lammOS_OnReceiveConfigSync", OnReceiveSync);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("lammOS_OnRequestConfigSync", 0uL, new(4, Allocator.Temp));
            }

            internal static void OnRequestSync(ulong clientId, FastBufferReader _)
            {
                SyncWithClient(clientId);
            }
            internal static void SyncWithClient(ulong clientId)
            {
                if (!NetworkManager.Singleton.IsHost) return;

                byte[] array = SerializeToBytes(Instance);
                int value = array.Length;

                FastBufferWriter stream = new(array.Length + 4, Allocator.Temp);
                try
                {
                    stream.WriteValueSafe(in value, default);
                    stream.WriteBytesSafe(array);

                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("lammOS_OnReceiveConfigSync", clientId, stream, NetworkDelivery.ReliableFragmentedSequenced);
                }
                catch (Exception e)
                {
                    Logger.LogError($"Error occurred syncing config with client: {clientId}\n{e}");
                }
            }
            internal static void OnReceiveSync(ulong _, FastBufferReader reader)
            {
                if (!reader.TryBeginRead(4))
                {
                    Logger.LogError("Config sync error: Could not begin reading buffer.");
                    return;
                }

                reader.ReadValueSafe(out int val, default);
                if (!reader.TryBeginRead(val))
                {
                    Logger.LogError("Config sync error: Host could not sync.");
                    return;
                }

                byte[] data = new byte[val];
                reader.ReadBytesSafe(ref data, val);

                SyncWithHost(DeserializeFromBytes(data));
            }
            internal static void SyncWithHost(SyncedConfig newConfig)
            {
                Instance.MaxDropshipItemsValue = newConfig.MaxDropshipItemsValue;
                Instance.MoonPriceMultipliers = newConfig.MoonPriceMultipliers;
                Instance.ItemPriceMultipliers = newConfig.ItemPriceMultipliers;
                Instance.UnlockablePriceMultipliers = newConfig.UnlockablePriceMultipliers;
            }

            internal static byte[] SerializeToBytes(SyncedConfig val)
            {
                BinaryFormatter bf = new();
                using MemoryStream stream = new();

                try
                {
                    bf.Serialize(stream, val);
                    return stream.ToArray();
                }
                catch (Exception e)
                {
                    Logger.LogError($"Error serializing instance: {e}");
                    return null;
                }
            }
            internal static SyncedConfig DeserializeFromBytes(byte[] data)
            {
                BinaryFormatter bf = new();
                using MemoryStream stream = new(data);

                try
                {
                    return (SyncedConfig)bf.Deserialize(stream);
                }
                catch (Exception e)
                {
                    Logger.LogError($"Error deserializing instance: {e}");
                    return default;
                }
            }
        }

        public class Macros
        {
            internal static Dictionary<string, List<string>> macros = new();

            public static bool AddMacro(string id, List<string> inputs)
            {
                if (HasMacro(id))
                {
                    return false;
                }
                macros.Add(id, new List<string>(inputs));
                return true;
            }
            public static bool HasMacro(string id)
            {
                return macros.ContainsKey(id);
            }
            public static List<string> GetMacro(string id)
            {
                if (!HasMacro(id))
                {
                    return null;
                }
                return new List<string>(macros[id]);
            }
            public static List<string> GetMacroIds()
            {
                return new List<string>(macros.Keys);
            }
            public static bool ModifyMacro(string id, List<string> inputs)
            {
                if (!HasMacro(id))
                {
                    return false;
                }
                macros[id] = new List<string>(inputs);
                return true;
            }
            public static bool RemoveMacro(string id)
            {
                if (!HasMacro(id))
                {
                    return false;
                }
                macros.Remove(id);
                return true;
            }

            public static void Save()
            {
                ES3.Save("lammOS_Macros", macros, Utility.CombinePaths(Paths.ConfigPath, "lammas123.lammOS.Macros.es3"));
            }
            public static void Load()
            {
                if (File.Exists(Utility.CombinePaths(Paths.ConfigPath, "lammas123.lammOS.Macros.es3")))
                {
                    macros = ES3.Load<Dictionary<string, List<string>>>("lammOS_Macros", Utility.CombinePaths(Paths.ConfigPath, "lammas123.lammOS.Macros.es3"));
                    return;
                }

                macros = new();
                Save();
            }
        }

        internal class Keybinds : LcInputActions
        {
            [InputAction("<Keyboard>/leftArrow", Name = "Previous Radar Target")]
            public InputAction PreviousRadarTargetKey { get; set; }
            [InputAction("<Keyboard>/rightArrow", Name = "Next Radar Target")]
            public InputAction NextRadarTargetKey { get; set; }

            [InputAction("<Keyboard>/upArrow", Name = "Previous Command History")]
            public InputAction PreviousCommandHistoryKey { get; set; }
            [InputAction("<Keyboard>/downArrow", Name = "Next Command History")]
            public InputAction NextCommandHistoryKey { get; set; }

            public static Keybinds Instance;
            public static bool isSetup = false;

            public static void Setup()
            {
                if (isSetup)
                {
                    return;
                }
                isSetup = true;

                Instance = new Keybinds();
                Instance.PreviousRadarTargetKey.performed += OnPreviousRadarTargetKeyPressed;
                Instance.NextRadarTargetKey.performed += OnNextRadarTargetKeyPressed;
                Instance.PreviousCommandHistoryKey.performed += OnPreviousCommandHistoryKeyPressed;
                Instance.NextCommandHistoryKey.performed += OnNextCommandHistoryKeyPressed;
            }

            public static void OnPreviousRadarTargetKeyPressed(InputAction.CallbackContext context)
            {
                if (!GameNetworkManager.Instance.localPlayerController.inTerminalMenu)
                {
                    return;
                }
                int index = StartOfRound.Instance.mapScreen.targetTransformIndex - 1;
                for (int i = 0; i < StartOfRound.Instance.mapScreen.radarTargets.Count; i++)
                {
                    if (index == -1)
                    {
                        index = StartOfRound.Instance.mapScreen.radarTargets.Count - 1;
                    }

                    if (StartOfRound.Instance.mapScreen.radarTargets[index] == null)
                    {
                        index--;
                        continue;
                    }
                    if (StartOfRound.Instance.mapScreen.radarTargets[index].isNonPlayer)
                    {
                        break;
                    }

                    PlayerControllerB component = StartOfRound.Instance.mapScreen.radarTargets[index].transform.gameObject.GetComponent<PlayerControllerB>();
                    if (component == null || !component.isPlayerControlled || component.isPlayerDead || component.redirectToEnemy != null)
                    {
                        index--;
                        continue;
                    }
                    break;
                }
                
                StartOfRound.Instance.mapScreen.SwitchRadarTargetAndSync(index);
            }
            public static void OnNextRadarTargetKeyPressed(InputAction.CallbackContext context)
            {
                if (GameNetworkManager.Instance.localPlayerController.inTerminalMenu)
                {
                    StartOfRound.Instance.mapScreen.SwitchRadarTargetAndSync((StartOfRound.Instance.mapScreen.targetTransformIndex + 1) % StartOfRound.Instance.mapScreen.radarTargets.Count);
                }
            }

            public static void OnPreviousCommandHistoryKeyPressed(InputAction.CallbackContext context)
            {
                if (!GameNetworkManager.Instance.localPlayerController.inTerminalMenu || commandHistoryIndex == 0 || commandHistory.Count == 0)
                {
                    return;
                }

                Terminal terminal = FindObjectOfType<Terminal>();
                if (commandHistoryIndex == -1)
                {
                    lastTypedCommand = terminal.screenText.text.Substring(terminal.screenText.text.Length - terminal.textAdded);
                    commandHistoryIndex = commandHistory.Count;
                }

                commandHistoryIndex--;
                terminal.screenText.text = terminal.screenText.text.Substring(0, terminal.screenText.text.Length - terminal.textAdded) + commandHistory[commandHistoryIndex];
            }
            public static void OnNextCommandHistoryKeyPressed(InputAction.CallbackContext context)
            {
                if (!GameNetworkManager.Instance.localPlayerController.inTerminalMenu || commandHistoryIndex == -1)
                {
                    return;
                }
                commandHistoryIndex++;

                Terminal terminal = FindObjectOfType<Terminal>();
                if (commandHistoryIndex == commandHistory.Count)
                {
                    commandHistoryIndex = -1;
                    terminal.screenText.text = terminal.screenText.text.Substring(0, terminal.screenText.text.Length - terminal.textAdded) + lastTypedCommand;
                    return;
                }

                terminal.screenText.text = terminal.screenText.text.Substring(0, terminal.screenText.text.Length - terminal.textAdded) + commandHistory[commandHistoryIndex];
            }
        }
    }
}