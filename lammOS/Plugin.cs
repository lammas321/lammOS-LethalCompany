using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace lammOS
{
    [BepInPlugin("lammas123.lammOS", "lammOS", "1.0.1")]
    public class lammOS : BaseUnityPlugin
    {
        public static lammOS Instance;

        public static Dictionary<string, TerminalKeyword> terminalKeywords = new Dictionary<string, TerminalKeyword>();
        public static Dictionary<string, int> terminalSyncedSounds = new Dictionary<string, int>();
        public static Dictionary<string, Moon> moons = new Dictionary<string, Moon>();
        public static Dictionary<string, PurchaseableItem> purchaseableItems = new Dictionary<string, PurchaseableItem>();
        public static Dictionary<string, PurchaseableUnlockable> purchaseableUnlockables = new Dictionary<string, PurchaseableUnlockable>();
        public static Dictionary<string, Entity> entities = new Dictionary<string, Entity>();
        public static Dictionary<string, Log> logs = new Dictionary<string, Log>();

        public static int maxDropshipItems = 12;

        private static bool addedCodeCommands = false;
        private static readonly Dictionary<string, Command> commands = new Dictionary<string, Command>();
        private static readonly Dictionary<string, string> shortcuts = new Dictionary<string, string>();

        internal static List<EnemyType> entitiesWithoutEntry = new List<EnemyType>();
        internal static List<TerminalNode> entriesWithoutEntity = new List<TerminalNode>();

        internal static string oldSetupText = "";
        internal static int setupNodeIndex = -1;
        internal static string oldWelcomeText = "";
        internal static readonly string newStartupText = "Powered by lammOS     Created by lammas123\n          Courtesy of the Company\n\nType HELP for a list of available commands.\n\n>";
        internal static string oldStartupText = "";
        internal static int startupNodeIndex = -1;
        internal static string oldHelpText = "";
        internal static int helpNodeIndex = -1;

        internal static GameObject Clock;
        internal static TextMeshProUGUI ClockText => Clock.GetComponent<TextMeshProUGUI>();

        internal static ConfigEntry<bool> DisableNewTerminalOS;
        internal static ConfigEntry<int> CharsToAutocomplete;
        internal static ConfigEntry<bool> ShowMinimumChars;
        internal static ConfigEntry<string> ShowPercentagesOrRarity;
        internal static ConfigEntry<bool> DisableTextPostProcessMethod;

        void Awake()
        {
            Instance = this;

            AddCommand("help", new HelpCommand());
            AddCommand("moons", new MoonsCommand());
            AddCommand("moon", new MoonCommand());
            AddCommand("route", new RouteCommand());
            AddCommand("store", new StoreCommand());
            AddCommand("buy", new BuyCommand());
            AddCommand("bestiary", new BestiaryCommand());
            AddCommand("storage", new StorageCommand());
            AddCommand("retrieve", new RetrieveCommand());
            AddCommand("monitor", new MonitorCommand());
            AddCommand("scan", new ScanCommand());
            AddCommand("sigurd", new SigurdCommand());
            AddCommand("targets", new TargetsCommand());
            AddCommand("switch", new SwitchCommand());
            AddCommand("ping", new PingCommand());
            AddCommand("flash", new FlashCommand());
            AddCommand("transmit", new TransmitCommand());
            AddCommand("eject", new EjectCommand());
            AddCommand("clr", new ClearCommand());
            AddCommand("codes", new CodesCommand());
            AddCommand("door", new DoorCommand());
            AddCommand("lights", new LightsCommand());
            AddCommand("tp", new TeleporterCommand());
            AddCommand("itp", new InverseTeleporterCommand());
            AddCommand("shortcuts", new ShortcutsCommand());
            AddCommand("reload", new ReloadCommand());
            AddCommand("debug", new DebugCommand());

            LoadConfigValues(false);
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Logger.LogInfo("lammas123.ShipDuty Loaded");
        }

        public void LoadConfigValues(bool reload = true)
        {
            if (reload)
            {
                Config.Reload();
            }

            DisableNewTerminalOS = Config.Bind("General", "DisableNewTerminalOS", false, "If the changes that lammOS makes should be disabled, in case you want the old terminal back.");

            CharsToAutocomplete = Config.Bind("General", "CharsToAutocomplete", 3, "The amount of characters required to autocomplete a parameter, such as when buying an item, the minimum value is 1. Values over the length of a parameter will be treated as the length of the parameter to avoid errors, meaning you'll have to type the full name of a parameter and no autocompleting can occur.");
            if (CharsToAutocomplete.Value < 1)
            {
                CharsToAutocomplete.Value = 1;
            }

            ShowMinimumChars = Config.Bind("General", "ShowMinimumChars", false, "If the minimum characters required for the terminal to autocomplete a parameter should be shown. For example: 'p' when buying a pro flashlight, or 'te' for the teleporter, while 'telev' is the minimum for the television. Having this on all the time doesn't look the greatest, but it helps when learning typing shortcuts.");

            ShowPercentagesOrRarity = Config.Bind("General", "ShowPercentagesOrRarity", "Percentage", "Whether a percentage (%) or rarity (fraction) should be shown next to things that have a chance of happening. Percentage or Rarity");
            if (ShowPercentagesOrRarity.Value != "Percentage" && ShowPercentagesOrRarity.Value != "Rarity")
            {
                ShowPercentagesOrRarity.Value = "Percentage";
            }

            DisableTextPostProcessMethod = Config.Bind("General", "DisableTextPostProcessMethod", true, "If the terminal's TextPostProcess method should be disabled. lammOS does not use this method so it is disabled by default to make running commands a bit faster, but this option is here in case any other mods utilize it.");

            Config.Save();
        }

        public void LoadKeywords(ref Terminal terminal)
        {
            terminalKeywords = new Dictionary<string, TerminalKeyword>();
            foreach (TerminalKeyword terminalKeyword in terminal.terminalNodes.allKeywords)
            {
                if (terminalKeywords.ContainsKey(terminalKeyword.word))
                {
                    Logger.LogWarning("A terminal keyword has already been added with the name: '" + terminalKeyword.word + "'");
                    continue;
                }
                terminalKeywords.Add(terminalKeyword.word, terminalKeyword);
            }

            terminalSyncedSounds = new Dictionary<string, int>
            {
                { "buy", 0 },
                { "error", 1 },
                { "loading", 2 },
                { "warning", 3 }
            };
        }

        public void LoadMoons()
        {
            moons = new Dictionary<string, Moon>();
            entitiesWithoutEntry = new List<EnemyType>();
            for (int i = 0; i < StartOfRound.Instance.levels.Length; i++)
            {
                for (int j = 0; j < terminalKeywords["route"].compatibleNouns.Length; j++)
                {
                    if (StartOfRound.Instance.levels[i].PlanetName.Substring(StartOfRound.Instance.levels[i].PlanetName.IndexOf(" ") + 1).ToLower() == terminalKeywords["route"].compatibleNouns[j].noun.word || (StartOfRound.Instance.levels[i].PlanetName == "71 Gordion" && terminalKeywords["route"].compatibleNouns[j].noun.word == "company"))
                    {
                        Moon moon = new Moon(i, j);
                        moons.Add(terminalKeywords["route"].compatibleNouns[j].noun.word, moon);
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

        public void LoadPurchaseables(ref Terminal terminal)
        {
            purchaseableItems = new Dictionary<string, PurchaseableItem>();
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

            purchaseableUnlockables = new Dictionary<string, PurchaseableUnlockable>();
            for (int i = 0; i < StartOfRound.Instance.unlockablesList.unlockables.Count; i++)
            {
                for (int j = 0; j < terminalKeywords["buy"].compatibleNouns.Length; j++)
                {
                    if (StartOfRound.Instance.unlockablesList.unlockables[i].unlockableName.ToLower().StartsWith(terminalKeywords["buy"].compatibleNouns[j].noun.word))
                    {
                        purchaseableUnlockables.Add(terminalKeywords["buy"].compatibleNouns[j].result.creatureName.ToLower(), new PurchaseableUnlockable(i, j));
                        break;
                    }
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

        public void LoadEntities(ref Terminal terminal)
        {
            int entityIndex = 0;
            entities = new Dictionary<string, Entity>();
            entriesWithoutEntity = new List<TerminalNode>(terminal.enemyFiles);
            while (entityIndex < entitiesWithoutEntry.Count)
            {
                EnemyType entity = entitiesWithoutEntry[0];
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
                        entityIndex++;
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

        public void PostLoadingEntities()
        {
            foreach (EnemyType entity in entitiesWithoutEntry)
            {
                Logger.LogWarning("The entity '" + entity.enemyName + "' could not successfully be loaded.");
            }
            entitiesWithoutEntry = new List<EnemyType>();

            foreach (TerminalNode node in entriesWithoutEntity)
            {
                Logger.LogWarning("The bestiary entry '" + node.creatureName + "' could not be matched to an entity.");
            }
            entriesWithoutEntity = new List<TerminalNode>();
        }

        public void LoadLogs(ref Terminal terminal)
        {
            logs = new Dictionary<string, Log>();
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

        public void LoadVariables(ref Terminal terminal)
        {
            Logger.LogInfo("Loading Variables...");

            LoadConfigValues();

            LoadKeywords(ref terminal);
            LoadMoons();
            LoadPurchaseables(ref terminal);
            LoadEntities(ref terminal);
            PostLoadingEntities();
            LoadLogs(ref terminal);
            
            Logger.LogInfo("Loaded Variables.");
        }

        public bool AddCommand(string id, Command command)
        {
            if (HasCommand(id))
            {
                return false;
            }
            commands.Add(id, command);
            return true;
        }

        public bool HasCommand(string id)
        {
            return commands.ContainsKey(id);
        }

        public Command GetCommand(string id)
        {
            if (!HasCommand(id))
            {
                return null;
            }
            return commands[id];
        }

        public List<string> GetCommandIds()
        {
            return new List<string>(commands.Keys);
        }

        public List<Command> GetCommands()
        {
            return new List<Command>(commands.Values);
        }

        public bool RemoveCommand(string id)
        {
            return commands.Remove(id);
        }

        public bool AddShortcut(string shortcut, string id)
        {
            if (IsShortcut(shortcut))
            {
                return false;
            }
            shortcuts.Add(shortcut, id);
            return true;
        }

        public bool IsShortcut(string shortcut)
        {
            return shortcuts.ContainsKey(shortcut);
        }

        public bool CommandIdHasShortcut(string id)
        {
            return shortcuts.ContainsValue(id);
        }

        public string GetShortcutByCommandID(string id)
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

        public string GetCommandIdByShortcut(string shortcut)
        {
            if (!IsShortcut(shortcut))
            {
                return null;
            }
            return shortcuts[shortcut];
        }

        public List<string> GetShortcuts()
        {
            return new List<string>(shortcuts.Keys);

        }

        public bool RemoveShortcut(string shortcut)
        {
            return shortcuts.Remove(shortcut);
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

        [HarmonyPatch(typeof(Terminal))]
        public static partial class TerminalEvents
        {
            [HarmonyPatch("Start")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostStart(ref Terminal __instance)
            {
                Instance.Logger.LogInfo("Terminal Start method called");

                Instance.LoadVariables(ref __instance);

                if (DisableNewTerminalOS.Value)
                {
                    Instance.Logger.LogInfo("lammOS is disabled.");
                    return;
                }

                for (int i = 0; i < __instance.terminalNodes.specialNodes.Count; i++)
                {
                    if (setupNodeIndex != -1 && startupNodeIndex != -1 && helpNodeIndex != -1)
                    {
                        break;
                    }
                    
                    if(__instance.terminalNodes.specialNodes[i].displayText.StartsWith("BG IG, A System-Act Ally"))
                    {
                        oldSetupText = __instance.terminalNodes.specialNodes[i].displayText;
                        oldWelcomeText = __instance.terminalNodes.specialNodes[i].terminalOptions[0].result.terminalOptions[0].result.displayText;
                        setupNodeIndex = i;
                        __instance.terminalNodes.specialNodes[i].displayText = __instance.terminalNodes.specialNodes[i].displayText.Replace("Halden Electronics Inc.", "lammas123").Replace("FORTUNE-9", "lammOS");
                        __instance.terminalNodes.specialNodes[i].terminalOptions[0].result.terminalOptions[0].result.displayText = __instance.terminalNodes.specialNodes[i].terminalOptions[0].result.terminalOptions[0].result.displayText.Substring(0, __instance.terminalNodes.specialNodes[i].terminalOptions[0].result.terminalOptions[0].result.displayText.IndexOf("Welcome to the FORTUNE-9 OS")) + newStartupText;
                    }
                    else if (__instance.terminalNodes.specialNodes[i].displayText.StartsWith("Welcome to the FORTUNE-9 OS"))
                    {
                        oldStartupText = __instance.terminalNodes.specialNodes[i].displayText;
                        startupNodeIndex = i;
                        __instance.terminalNodes.specialNodes[i].displayText = newStartupText;
                        __instance.terminalNodes.specialNodes[i].maxCharactersToType = 99999;
                    }
                    else if(__instance.terminalNodes.specialNodes[i].displayText.StartsWith(">MOONS\n"))
                    {
                        oldHelpText = __instance.terminalNodes.specialNodes[i].displayText;
                        helpNodeIndex = i;
                        __instance.terminalNodes.specialNodes[i].displayText = newStartupText;
                        __instance.terminalNodes.specialNodes[i].maxCharactersToType = 99999;
                    }
                }

                Transform terminalMainContainer = __instance.transform.parent.parent.Find("Canvas").Find("MainContainer");
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

                if (addedCodeCommands)
                {
                    return;
                }
                addedCodeCommands = true;
                foreach (TerminalKeyword keyword in terminalKeywords.Values)
                {
                    if (keyword.word.Length == 2)
                    {
                        Instance.AddCommand(keyword.word, new CodeCommand(keyword.word));
                    }
                }
            }

            [HarmonyPatch("ParsePlayerSentence")]
            [HarmonyPrefix]
            [HarmonyPriority(2147483647)]
            public static bool PreParsePlayerSentence(ref Terminal __instance, ref TerminalNode __result)
            {
                if (DisableNewTerminalOS.Value)
                {
                    return true;
                }

                TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
                node.displayText = "";
                node.clearPreviousText = true;
                node.terminalEvent = "";

                string input = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded);
                int offset = input.IndexOf(' ');
                if (offset == -1)
                {
                    offset = input.Length;
                }
                string commandId = input.Substring(0, offset);

                if (Instance.IsShortcut(commandId))
                {
                    commandId = Instance.GetCommandIdByShortcut(commandId);
                    __instance.screenText.text = __instance.screenText.text.Substring(0, __instance.screenText.text.Length - __instance.textAdded) + commandId + input.Substring(offset);
                }

                Command command = Instance.GetCommand(commandId);
                if (command == null)
                {
                    node.displayText = "Unknown command: '" + commandId + "'\n\n>";
                    node.playSyncedClip = terminalSyncedSounds["error"];
                    __result = node;
                    return false;
                }
                
                try
                {
                    command.Execute(ref __instance, input.Substring(offset == input.Length ? offset : offset + 1), ref node);
                }
                catch (Exception e)
                {
                    node = ScriptableObject.CreateInstance<TerminalNode>();
                    node.displayText = "An error occurred executing the command: '" + commandId + "'\n\n>";
                    node.clearPreviousText = true;
                    node.terminalEvent = "";
                    node.playSyncedClip = terminalSyncedSounds["error"];
                    Instance.Logger.LogError("An error occurred executing the command: '" + commandId + "'\n" + e.ToString());
                }

                __result = node;
                return false;
            }

            [HarmonyPatch("ParsePlayerSentence")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostParsePlayerSentence(ref Terminal __instance, ref TerminalNode __result)
            {
                if (DisableNewTerminalOS.Value)
                {
                    return;
                }

                if (!__result.clearPreviousText)
                {
                    __instance.screenText.text = "\n" + __instance.screenText.text.TrimStart('\n');
                }

                __result.maxCharactersToType = 99999;
                __result.displayText = __result.displayText.TrimStart('\n');

                if (__result.displayText == ">")
                {
                    return;
                }

                if (__result.displayText == "")
                {
                    __result.displayText = ">";
                    return;
                }

                if (!__result.displayText.EndsWith(">"))
                {
                    __result.displayText = __result.displayText.TrimEnd('\n') + "\n\n>";
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

        [HarmonyPatch(typeof(HUDManager))]
        public static class HUDManagerEvents
        {
            [HarmonyPatch("SetClock")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void SetClock(ref HUDManager __instance)
            {
                ClockText.text = __instance.clockNumber.text.Replace("\n", " ");
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
                description = "View helpful information about available commands. Why did you need help to know what help does?";
                Instance.AddShortcut("?", "help");
                Instance.AddShortcut("h", "help");
                args = "(Command)";
            }

            public void GenerateHelpPage(ref TerminalNode node)
            {
                foreach (string commandId in Instance.GetCommandIds())
                {
                    Command command = Instance.GetCommand(commandId);
                    if (command.hidden)
                    {
                        continue;
                    }

                    node.displayText += ">" + commandId.ToUpper();
                    if (command.args != "")
                    {
                        node.displayText += " " + command.args;
                    }
                    node.displayText += "\n";
                }
            }

            public void GenerateCommandHelp(string commandId, Command command, ref TerminalNode node)
            {
                List<string> shortcuts = new List<string>();
                foreach (string shortcut in Instance.GetShortcuts())
                {
                    if (Instance.GetCommandIdByShortcut(shortcut) == commandId)
                    {
                        shortcuts.Add(">" + shortcut.ToUpper());
                    }
                }
                node.displayText = ">" + commandId.ToUpper();

                if (command.args != "")
                {
                    node.displayText += " " + command.args;
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
                if (Instance.IsShortcut(input))
                {
                    input = Instance.GetCommandIdByShortcut(input);
                }
                Command command = Instance.GetCommand(input);
                if (command == null)
                {
                    ErrorResponse("No command command found with id: '" + input + "'", ref node);
                    return;
                }

                GenerateCommandHelp(input, command, ref node);
            }
        }

        public class MoonsCommand : Command
        {
            public MoonsCommand()
            {
                description = "Lists the available moons you can travel to.";
                Instance.AddShortcut("ms", "moons");
            }

            public void GenerateMoonIndex(SelectableLevel level, ref TerminalNode node)
            {
                string moonId = level.PlanetName.Substring(level.PlanetName.IndexOf(" ") + 1).ToLower();
                node.displayText += "\n*";
                if (ShowMinimumChars.Value)
                {
                    node.displayText += " (" + moons[moonId].shortestChars + ")";
                }
                node.displayText += level.PlanetName.Substring(level.PlanetName.IndexOf(" "));

                if (level.currentWeather != LevelWeatherType.None)
                {
                    node.displayText += " (" + level.currentWeather.ToString() + ")";
                }

                int cost = moons[moonId].GetNode().itemCost;
                if (cost != 0)
                {
                    node.displayText += "   $" + cost.ToString();
                }
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                node.displayText = "Moons:\n* ";
                if (ShowMinimumChars.Value)
                {
                    node.displayText += "(" + moons["company"].shortestChars + ") ";
                }
                node.displayText += "The Company Building (" + Mathf.RoundToInt(StartOfRound.Instance.companyBuyingRate * 100f).ToString() + "%)";

                foreach (SelectableLevel level in terminal.moonsCatalogueList)
                {
                    GenerateMoonIndex(level, ref node);
                }
            }
        }

        public class MoonCommand : Command
        {
            public MoonCommand()
            {
                description = "View information about a moon.";
                Instance.AddShortcut("m", "moon");
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

            public void DetailedWeather(SelectableLevel level, ref TerminalNode node)
            {
                node.displayText += "\n\nTypes of Weather:";
                foreach (RandomWeatherWithVariables weather in level.randomWeathers)
                {
                    if (weather.weatherType == LevelWeatherType.None)
                    {
                        continue;
                    }
                    node.displayText += "\n* " + weather.weatherType.ToString();
                }
            }

            public void DetailedIndoors(SelectableLevel level, ref TerminalNode node)
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

            public void DetailedScrap(SelectableLevel level, ref TerminalNode node)
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

            public void DetailedEntities(SelectableLevel level, ref TerminalNode node)
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

            public void DetailedSafety(SelectableLevel level, ref TerminalNode node)
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
                        int hour = Mathf.FloorToInt(timeNormalized);
                        string minute = Mathf.FloorToInt((timeNormalized - hour) * 60).ToString();
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
                        int hour = Mathf.FloorToInt(timeNormalized);
                        string minute = Mathf.FloorToInt((timeNormalized - hour) * 60).ToString();
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

            public void DetailedResult(SelectableLevel level, ref TerminalNode node)
            {
                DetailedWeather(level, ref node);
                DetailedIndoors(level, ref node);
                DetailedScrap(level, ref node);
                DetailedEntities(level, ref node);
                DetailedSafety(level, ref node);
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
                node.displayText += "\n\n" + level.LevelDescription;

                if (level.PlanetName != "71 Gordion")
                {
                    DetailedResult(level, ref node);
                }
            }
        }

        public class RouteCommand : Command
        {
            public RouteCommand()
            {
                description = "Travel to the specified moon.";
                Instance.AddShortcut("r", "route");
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
                if (terminal.groupCredits < terminalNode.itemCost)
                {
                    ErrorResponse("You do not have enough credits to go to that moon.", ref node);
                    return;
                }

                terminal.groupCredits = Mathf.Clamp(terminal.groupCredits - terminalNode.itemCost, 0, 10000000);
                terminal.useCreditsCooldown = true;
                startOfRound.ChangeLevelServerRpc(terminalNode.buyRerouteToMoon, terminal.groupCredits);
                node.displayText = "Routing autopilot to " + moon.GetMoon().PlanetName + ".";
            }
        }

        public class StoreCommand : Command
        {
            public StoreCommand()
            {
                description = "View the items available to buy.";
                Instance.AddShortcut("x", "store");
            }

            public void GeneratePurchaseableItems(ref Terminal terminal, ref TerminalNode node)
            {
                node.displayText = "Welcome to the Company store!\nUse the BUY command to buy any items listed here:\n------------------------------";
                foreach (PurchaseableItem purchaseableItem in purchaseableItems.Values)
                {
                    Item item = purchaseableItem.GetItem(ref terminal);
                    int salePercentage = purchaseableItem.GetSalePercentage(ref terminal);
                    node.displayText += "\n* ";
                    if (ShowMinimumChars.Value)
                    {
                        node.displayText += "(" + purchaseableItem.shortestChars + ") ";
                    }

                    node.displayText += item.itemName + "   $" + (item.creditsWorth * salePercentage / 100f).ToString();
                    if (salePercentage != 100)
                    {
                        node.displayText += "     " + (100 - salePercentage).ToString() + "% OFF";
                    }
                }
            }

            public void GenerateUpgrades(ref TerminalNode node)
            {
                List<PurchaseableUnlockable> upgrades = new List<PurchaseableUnlockable>();
                foreach (PurchaseableUnlockable unlockable in purchaseableUnlockables.Values)
                {
                    if (unlockable.GetUnlockable().alwaysInStock && !unlockable.GetUnlockable().hasBeenUnlockedByPlayer && !unlockable.GetUnlockable().alreadyUnlocked)
                    {
                        upgrades.Add(unlockable);
                    }
                }
                upgrades.Sort((x, y) => x.GetNode().itemCost - y.GetNode().itemCost);

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
                    node.displayText += unlockable.GetUnlockable().unlockableName + "   $" + unlockable.GetNode().itemCost.ToString();
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
                        node.displayText += decorNode.creatureName + "   $" + decorNode.itemCost;

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

        public class BuyCommand : Command
        {
            public BuyCommand()
            {
                description = "Purchase an available item in the store.";
                Instance.AddShortcut("b", "buy");
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

                int cost = (int)(purchaseableItem.GetItem(ref terminal).creditsWorth * ((float)purchaseableItem.GetSalePercentage(ref terminal) / 100) * amount);
                if (terminal.groupCredits < cost)
                {
                    ErrorResponse("You do not have enough credits to purchase that item.", ref node);
                    return;
                }
                if (amount + terminal.numberOfItemsInDropship > maxDropshipItems)
                {
                    ErrorResponse("There is not enough space on the dropship for these items, there are currently " + terminal.numberOfItemsInDropship.ToString() + "/" + maxDropshipItems.ToString() + " items en route.", ref node);
                    return;
                }

                terminal.groupCredits = Mathf.Clamp(terminal.groupCredits - cost, 0, 10000000);
                terminal.numberOfItemsInDropship += amount;
                for (int i = 0; i < amount; i++)
                {
                    terminal.orderedItemsFromTerminal.Add(purchaseableItem.index);
                }

                if (terminal.IsServer)
                {
                    terminal.SyncGroupCreditsClientRpc(terminal.groupCredits, terminal.numberOfItemsInDropship);
                }
                else
                {
                    terminal.useCreditsCooldown = true;
                    terminal.BuyItemsServerRpc(terminal.orderedItemsFromTerminal.ToArray(), terminal.groupCredits, terminal.numberOfItemsInDropship);
                    terminal.orderedItemsFromTerminal.Clear();
                }

                node.displayText = "Purchased " + purchaseableItem.GetItem(ref terminal).itemName + " x" + amount.ToString() + " for $" + cost.ToString() + ".";
                node.playSyncedClip = terminalSyncedSounds["buy"];
            }

            public void PurchaseUnlockable(string input, ref Terminal terminal, ref TerminalNode node)
            {
                PurchaseableUnlockable purchaseableUnlockable = null;
                foreach (PurchaseableUnlockable unlockable in purchaseableUnlockables.Values)
                {
                    if (unlockable.GetUnlockable().unlockableName.ToLower().StartsWith(input))
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
                if (CheckNotEnoughChars(input.Length, purchaseableUnlockable.GetUnlockable().unlockableName.Length, ref node))
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

                int cost = purchaseableUnlockable.GetNode().itemCost;
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

                terminal.groupCredits = Mathf.Clamp(terminal.groupCredits - cost, 0, 10000000);
                HUDManager.Instance.DisplayTip("Tip", "Press B to move and place objects in the ship, E to cancel.", false, true, "LC_MoveObjectsTip");
                FindObjectOfType<StartOfRound>().BuyShipUnlockableServerRpc(purchaseableUnlockable.GetNode().shipUnlockableID, terminal.groupCredits);

                node.displayText = "Purchased " + purchaseableUnlockable.GetUnlockable().unlockableName + " for $" + cost.ToString() + ".";
                node.playSyncedClip = terminalSyncedSounds["buy"];
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
        }

        public class BestiaryCommand : Command
        {
            public BestiaryCommand()
            {
                description = "List or view information about scanned entities.";
                Instance.AddShortcut("e", "bestiary");
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

        public class StorageCommand : Command
        {
            public StorageCommand()
            {
                description = "View unlockables stored away in storage.";
                Instance.AddShortcut("st", "storage");
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
                        node.displayText += unlockable.GetUnlockable().unlockableName;
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
                description = "Retrieve an unlockable from storage.";
                Instance.AddShortcut("re", "retrieve");
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
                    if (unlockable.GetUnlockable().unlockableName.ToLower().StartsWith(input))
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
                if (CheckNotEnoughChars(input.Length, purchaseableUnlockable.GetUnlockable().unlockableName.Length, ref node))
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

        public class MonitorCommand : Command
        {
            public MonitorCommand()
            {
                description = "View the main monitor on the terminal.";
                Instance.AddShortcut("v", "monitor");
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

        public class ScanCommand : Command
        {
            public ScanCommand()
            {
                description = "View the amount of items and total value remaining outside the ship.";
                Instance.AddShortcut("sc", "scan");
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

        public class SigurdCommand : Command
        {
            public SigurdCommand()
            {
                description = "View all of the logs from Sigurd you have collected.";
                Instance.AddShortcut("logs", "sigurd");
                Instance.AddShortcut("log", "sigurd");
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

        public class TargetsCommand : Command
        {
            public TargetsCommand()
            {
                description = "View all of the radar targets you can monitor.";
                Instance.AddShortcut("rt", "targets");
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
                description = "Switch to another player or radar booster on the monitor.";
                Instance.AddShortcut("s", "switch");
                args = "(Player or Radar)";
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
                for(int i = 0; i < StartOfRound.Instance.mapScreen.radarTargets.Count; i++)
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
                description = "Ping a radar booster playing a sound.";
                Instance.AddShortcut("p", "ping");
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
                description = "Flash a radar booster blinding and stunning nearby crew and entities temporarily.";
                Instance.AddShortcut("f", "flash");
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

        public class TransmitCommand : Command
        {
            public TransmitCommand()
            {
                description = "Transmit at most a 9 character message using the signal transmitter to your crew.";
                Instance.AddShortcut("t", "transmit");
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

        public class EjectCommand : Command
        {
            public EjectCommand()
            {
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

                StartOfRound.Instance.ManuallyEjectPlayersServerRpc();
            }
        }

        public class ClearCommand : Command
        {
            public ClearCommand()
            {
                description = "Clear all text from the terminal.";
                Instance.AddShortcut("c", "clr");
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
                description = "View all of the alphanumeric codes and what objects they correspond to within the building.";
                Instance.AddShortcut("co", "codes");
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

        public class DoorCommand : Command
        {
            public DoorCommand()
            {
                description = "Toggle the ship's door.";
                Instance.AddShortcut("d", "door");
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
                description = "Toggle the ship's lights.";
                Instance.AddShortcut("l", "lights");
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

        public class ShortcutsCommand : Command
        {
            public ShortcutsCommand()
            {
                description = "View all of the shortcuts there are for commands.";
                Instance.AddShortcut("sh", "shortcuts");
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                node.displayText = "Shortcuts:";
                foreach (string shortcut in Instance.GetShortcuts())
                {
                    node.displayText += "\n* >" + Instance.GetCommandIdByShortcut(shortcut).ToUpper() + "  ->  >" + shortcut.ToUpper();
                }
                if (node.displayText == "Shortcuts:")
                {
                    node.displayText += "\nThere are no shortcuts.";
                }
            }
        }

        public class ReloadCommand : Command
        {
            public ReloadCommand()
            {
                description = "Reloads the mod's config and the terminal, updating any potentially outdated information.";
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                Instance.Logger.LogInfo("Reload command called.");

                Instance.LoadVariables(ref terminal);

                if (DisableNewTerminalOS.Value)
                {
                    Instance.Logger.LogInfo("lammOS has been disabled.");

                    terminal.terminalNodes.specialNodes[setupNodeIndex].displayText = oldSetupText;
                    terminal.terminalNodes.specialNodes[setupNodeIndex].terminalOptions[0].result.terminalOptions[0].result.displayText = oldWelcomeText;
                    oldSetupText = "";
                    oldWelcomeText = "";
                    setupNodeIndex = -1;

                    terminal.terminalNodes.specialNodes[startupNodeIndex].displayText = oldStartupText;
                    terminal.terminalNodes.specialNodes[startupNodeIndex].maxCharactersToType = 35;
                    oldStartupText = "";
                    startupNodeIndex = -1;

                    terminal.terminalNodes.specialNodes[helpNodeIndex].displayText = oldHelpText;
                    terminal.terminalNodes.specialNodes[helpNodeIndex].maxCharactersToType = 35;
                    oldHelpText = "";
                    helpNodeIndex = -1;

                    return;
                }

                node.displayText = "Reloaded.";
            }
        }

        public class DebugCommand : Command
        {
            public DebugCommand()
            {
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
                            text += "\n* " + unlockable.GetUnlockable().unlockableName + "   (" + unlockable.shortestChars + ")";
                        }
                        text += "\n\nSigurd Logs:";
                        foreach (Log log in logs.Values)
                        {
                            text += "\n* " + log.GetNode(ref terminal).creatureName + "   (" + log.shortestChars + ")";
                        }
                        Instance.Logger.LogInfo(text);
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
                            Instance.Logger.LogInfo("Synced Audios: " + terminal.syncedAudios.Length.ToString());
                            break;
                        }

                        int.TryParse(args, out index);
                        if (index < 0)
                        {
                            Instance.Logger.LogInfo("Index must be greater than 0.");
                            break;
                        }
                        if (index >= terminal.syncedAudios.Length)
                        {
                            Instance.Logger.LogInfo("Index is greater than " + terminal.syncedAudios.Length.ToString() + ".");
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
                                Instance.Logger.LogInfo("Money must be greater than 0.");
                                break;
                            }
                        }
                        terminal.useCreditsCooldown = true;
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
                        Instance.Logger.LogInfo(keywords);
                        break;
                    }
                    case "keyword":
                    {
                        int index = -1;
                        int.TryParse(args, out index);
                        if (index < 0 || index >= terminal.terminalNodes.allKeywords.Length)
                        {
                            Instance.Logger.LogInfo("Keyword index is not valid.");
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
                        Instance.Logger.LogInfo(text);
                        break;
                    }
                    case "nodes":
                    {
                        Instance.Logger.LogInfo("Nodes: " + terminal.terminalNodes.terminalNodes.Count.ToString());
                        break;
                    }
                    case "specialnodes":
                    {
                        Instance.Logger.LogInfo("Special Nodes: " + terminal.terminalNodes.specialNodes.Count.ToString());
                        break;
                    }
                    case "node":
                    {
                        int index = -1;
                        int.TryParse(args, out index);
                        if (index < 0 || index >= terminal.terminalNodes.terminalNodes.Count)
                        {
                            Instance.Logger.LogInfo("Node index is not valid.");
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
                        Instance.Logger.LogInfo(text);
                        break;
                    }
                    case "specialnode":
                    {
                        int index = -1;
                        int.TryParse(args, out index);
                        if (index < 0 || index >= terminal.terminalNodes.specialNodes.Count)
                        {
                            Instance.Logger.LogInfo("Node index is not valid.");
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
                        Instance.Logger.LogInfo(text);
                        break;
                    }
                }
            }
        }

        public class CodeCommand : Command
        {
            public string code;

            public CodeCommand(string alphanumericCode)
            {
                code = alphanumericCode;
                description = "An alphanumeric code command.";
                hidden = true;
            }

            public override void Execute(ref Terminal terminal, string input, ref TerminalNode node)
            {
                TerminalAccessibleObject[] array = FindObjectsOfType<TerminalAccessibleObject>();
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].objectCode == code)
                    {
                        array[i].CallFunctionFromTerminal();
                    }
                }

                terminal.codeBroadcastAnimator.SetTrigger("display");
                terminal.terminalAudio.PlayOneShot(terminal.codeBroadcastSFX, 1f);
            }
        }

        abstract public class Command
        {
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
        }
    }
}