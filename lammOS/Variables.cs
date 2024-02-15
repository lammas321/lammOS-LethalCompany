using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static lammOS.lammOS;

namespace lammOS.Variables
{
    public static class Variables
    {
        public static Dictionary<string, TerminalKeyword> terminalKeywords = new();
        internal static TerminalKeyword[] terminalKeywordsBackup;
        public static Dictionary<string, int> terminalSyncedSounds = new()
        {
            { "buy", 0 },
            { "error", 1 },
            { "loading", 2 },
            { "warning", 3 }
        };
        public static Dictionary<string, Moon> moons;
        public static Dictionary<string, string> moonNameToMoonNode;
        public static Dictionary<string, PurchasableItem> purchasableItems;
        public static Dictionary<string, PurchasableUnlockable> purchasableUnlockables;
        public static Dictionary<string, Entity> entities;
        public static List<EnemyType> entitiesWithoutEntry;
        public static List<TerminalNode> entriesWithoutEntity;
        public static Dictionary<string, Log> logs;

        public class Moon
        {
            public string id;
            public SelectableLevel level;
            public TerminalNode node;
            public string name;
            public string shortName;
            public string styledName;
            public string shortestChars;

            public Moon(string id, SelectableLevel level, TerminalNode node, string shortName)
            {
                this.id = id;
                this.level = level;
                this.node = node;
                name = level.PlanetName;
                this.shortName = shortName;

                if (name == shortName)
                {
                    styledName = name;
                    return;
                }

                int split = name.IndexOf(' ');
                if (split == -1)
                {
                    styledName = name;
                    return;
                }
                styledName = name.Substring(0, split) + "-" + name.Substring(split + 1);
            }
        }
        public class PurchasableItem
        {
            public int index;
            public Item item;
            public string name;
            public int salePercentage => NewTerminal.NewTerminal.Terminal.itemSalesPercentages[index];
            public string shortestChars;

            public PurchasableItem(int index)
            {
                this.index = index;
                item = NewTerminal.NewTerminal.Terminal.buyableItemsList[index];
            }
        }
        public class PurchasableUnlockable
        {
            public UnlockableItem unlockable;
            public TerminalNode node;
            public string shortestChars;

            public PurchasableUnlockable(UnlockableItem unlockable, TerminalNode node)
            {
                this.unlockable = unlockable;
                this.node = node;
            }
        }
        public class Entity
        {
            public EnemyType type;
            public TerminalNode entry;
            public string name;
            public string shortestChars;

            public Entity(EnemyType type, TerminalNode entry, string name = null)
            {
                this.type = type;
                this.entry = entry;
                if (name == null)
                {
                    name = entry == null ? type.enemyName : entry.creatureName;
                }
                this.name = name;
            }
        }
        public class Log
        {
            public TerminalNode log;
            public string shortestChars;

            public Log(TerminalNode log)
            {
                this.log = log;
            }
        }

        internal static void LoadConfigValues()
        {
            if (File.Exists(Config.ConfigFilePath))
            {
                Config.Reload();
            }
            else
            {
                Config.Clear();
            }

            // Colors
            Dictionary<string, string> weatherColors = new();
            Dictionary<string, string> defaultWeatherColors = new()
            {
                { "Foggy", "#666666" },
                { "Rainy", "#FFFF00" },
                { "Stormy", "#FF7700" },
                { "Flooded", "#FF0000" },
                { "Eclipsed", "#AA0000" }
            };
            foreach (string weatherName in Enum.GetNames(typeof(LevelWeatherType)))
            {
                if (weatherName == "None" || weatherName == "DustClouds")
                {
                    continue;
                }
                var configWeatherColor = Config.Bind("Colors", "Weather_" + weatherName + "_Color", "null");
                configWeatherColor.Value = Commands.Commands.Command.RemoveRichText(configWeatherColor.Value);
                if (configWeatherColor.Value == "null")
                {
                    if (defaultWeatherColors.ContainsKey(weatherName))
                    {
                        configWeatherColor.Value = defaultWeatherColors[weatherName];
                    }
                    else
                    {
                        configWeatherColor.Value = "#03e715";
                    }
                }
                weatherColors.Add(weatherName, configWeatherColor.Value);
            }
            WeatherColors = weatherColors;
            
            // General
            ShowCommandConfirmations = Config.Bind("General", "ShowCommandConfirmations", true, "If commands like >BUY should ask you for confirmation before doing something.").Value;

            var configCharsToAutocomplete = Config.Bind("General", "CharsToAutocomplete", 3, "The amount of characters required to autocomplete an argument, such as when buying an item, the minimum value is 1. Values over the length of an argument will be treated as the length of the argument to avoid errors, meaning you'll have to type the full name of the argument and no autocompleting can occur.");
            if (configCharsToAutocomplete.Value < 1)
            {
                configCharsToAutocomplete.Value = 1;
            }
            CharsToAutocomplete = configCharsToAutocomplete.Value;

            ShowMinimumChars = Config.Bind("General", "ShowMinimumChars", false, "If the minimum characters required for the terminal to autocomplete an argument should be shown. For example: 'p' when buying a pro flashlight, or 'te' for the teleporter, while 'telev' is the minimum for the television. Having this on all the time doesn't look the greatest, but it helps when learning typing shortcuts.").Value;
            
            var configListPaddingChar = Config.Bind("General", "ListPaddingChar", ".", "The character that should be used when adding padding to lists. If you want to use a space, the config parser will trim it out, but I will check if the config is empty and replace it with a space.");
            if (configListPaddingChar.Value == "")
            {
                configListPaddingChar.Value = " ";
            }
            ListPaddingChar = configListPaddingChar.Value[0];

            var configShowPercentagesOrRarity = Config.Bind("General", "ShowPercentagesOrRarity", "Percentage", "Whether a percentage (%) or rarity (fraction) should be shown next to things that have a chance of happening. Percentage or Rarity");
            if (configShowPercentagesOrRarity.Value != "Percentage" && configShowPercentagesOrRarity.Value != "Rarity")
            {
                configShowPercentagesOrRarity.Value = "Percentage";
            }
            ShowPercentagesOrRarity = configShowPercentagesOrRarity.Value;

            var configMaxCommandHistory = Config.Bind("General", "MaxCommandHistory", 25, "How far back the terminal should remember commands you've typed in, the minimum value is 1 and the maximum is 100.");
            if (configMaxCommandHistory.Value < 1)
            {
                configMaxCommandHistory.Value = 1;
            }
            else if (configMaxCommandHistory.Value > 100)
            {
                configMaxCommandHistory.Value = 100;
            }
            MaxCommandHistory = configMaxCommandHistory.Value;

            ShowTerminalClock = Config.Bind("General", "ShowTerminalClock", true, "If the terminal clock should be shown in the top right corner or not.").Value;

            DisableIntroSpeech = Config.Bind("General", "DisableIntroSpeech", false, "If the intro speech should be disabled.").Value;
            if (StartOfRound.Instance != null)
            {
                if (DisableIntroSpeech && savedShipIntroSpeechSFX == null)
                {
                    savedShipIntroSpeechSFX = StartOfRound.Instance.shipIntroSpeechSFX;
                    StartOfRound.Instance.shipIntroSpeechSFX = StartOfRound.Instance.disableSpeakerSFX;
                }
                else if (!DisableIntroSpeech && savedShipIntroSpeechSFX != null)
                {
                    StartOfRound.Instance.shipIntroSpeechSFX = savedShipIntroSpeechSFX;
                    savedShipIntroSpeechSFX = null;
                }
            }

            Config.Save();
        }

        internal static void LoadKeywords()
        {
            if (terminalKeywords.Count != 0)
            {
                foreach (TerminalKeyword terminalKeyword in NewTerminal.NewTerminal.Terminal.terminalNodes.allKeywords)
                {
                    if (terminalKeywords.ContainsKey(terminalKeyword.word))
                    {
                        terminalKeywords.Remove(terminalKeyword.word);
                    }
                }
            }

            foreach (TerminalKeyword terminalKeyword in NewTerminal.NewTerminal.Terminal.terminalNodes.allKeywords)
            {
                if (terminalKeywords.ContainsKey(terminalKeyword.word))
                {
                    lammOS.Logger.LogWarning("A terminal keyword has already been added with the name: '" + terminalKeyword.word + "'");
                    continue;
                }
                terminalKeywords.Add(terminalKeyword.word, terminalKeyword);
            }
        }
        internal static void LoadMoons()
        {
            moons = new();
            moonNameToMoonNode = new();
            entitiesWithoutEntry = new();

            Dictionary<int, bool> foundLevels = new();
            for (int i = 0; i < StartOfRound.Instance.levels.Length; i++)
            {
                foundLevels.Add(i, false);
            }

            foreach (CompatibleNoun cn in terminalKeywords["route"].compatibleNouns)
            {
                if (moons.ContainsKey(cn.noun.word))
                {
                    lammOS.Logger.LogWarning("A moon has already been added with the id '" + cn.noun.word + "'.");
                    continue;
                }
                if (cn.result.terminalOptions[1].result.buyRerouteToMoon < 0 || cn.result.terminalOptions[1].result.buyRerouteToMoon >= StartOfRound.Instance.levels.Length)
                {
                    lammOS.Logger.LogWarning("Moon terminal node '" + cn.noun.word + "' with a selectable level index of " + cn.result.terminalOptions[1].result.buyRerouteToMoon.ToString() + " is out of the selectable level range: 0-" + (StartOfRound.Instance.levels.Length - 1).ToString());
                    continue;
                }

                SelectableLevel level = StartOfRound.Instance.levels[cn.result.terminalOptions[1].result.buyRerouteToMoon];
                string shortName = level.PlanetName.TrimStart(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9']);
                if (shortName.IndexOf(' ') == 0)
                {
                    shortName = shortName.Substring(1);
                }

                foundLevels[cn.result.terminalOptions[1].result.buyRerouteToMoon] = true;
                Moon moon = new(cn.noun.word, level, cn.result.terminalOptions[1].result, shortName);
                moons.Add(moon.id, moon);
                moonNameToMoonNode.Add(level.PlanetName.ToLower(), moon.id);

                foreach (SpawnableEnemyWithRarity entity in moon.level.Enemies)
                {
                    if (!entitiesWithoutEntry.Contains(entity.enemyType))
                    {
                        entitiesWithoutEntry.Add(entity.enemyType);
                    }
                }
                foreach (SpawnableEnemyWithRarity entity in moon.level.OutsideEnemies)
                {
                    if (!entitiesWithoutEntry.Contains(entity.enemyType))
                    {
                        entitiesWithoutEntry.Add(entity.enemyType);
                    }
                }
                foreach (SpawnableEnemyWithRarity entity in moon.level.DaytimeEnemies)
                {
                    if (!entitiesWithoutEntry.Contains(entity.enemyType))
                    {
                        entitiesWithoutEntry.Add(entity.enemyType);
                    }
                }
            }

            foreach (int levelId in foundLevels.Keys)
            {
                if (!foundLevels[levelId])
                {
                    lammOS.Logger.LogWarning("Could not find a matching terminal node for the moon: " + StartOfRound.Instance.levels[levelId].PlanetName);
                }
            }

            foreach (string moonId in moons.Keys)
            {
                for (int i = Mathf.Min(moonId.Length, CharsToAutocomplete); i <= moonId.Length; i++)
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
        internal static void LoadPurchasables()
        {
            purchasableItems = new();
            for (int i = 0; i < NewTerminal.NewTerminal.Terminal.buyableItemsList.Length; i++)
            {
                purchasableItems.Add(NewTerminal.NewTerminal.Terminal.buyableItemsList[i].itemName.ToLower(), new PurchasableItem(i));
            }

            foreach (string itemId in purchasableItems.Keys)
            {
                for (int i = Mathf.Min(itemId.Length, CharsToAutocomplete); i < itemId.Length; i++)
                {
                    string shortestChars = itemId.Substring(0, i);
                    bool shortest = true;
                    foreach (string itemId2 in purchasableItems.Keys)
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
                        purchasableItems[itemId].shortestChars = shortestChars;
                        break;
                    }
                }
                if (purchasableItems[itemId].shortestChars == null)
                {
                    // TODO fix, an earlier name conflicts by being too similar (or the same) and of the same or longer length than this name
                    purchasableItems[itemId].shortestChars = "ERR";
                }
            }

            purchasableUnlockables = new();
            foreach (UnlockableItem unlockable in StartOfRound.Instance.unlockablesList.unlockables)
            {
                if (unlockable.shopSelectionNode == null)
                {
                    bool foundMatch = false;
                    for (int j = 0; j < terminalKeywords["buy"].compatibleNouns.Length; j++)
                    {
                        if (unlockable.unlockableName.ToLower().Replace("-", " ").StartsWith(terminalKeywords["buy"].compatibleNouns[j].noun.word.Replace("-", " ")))
                        {
                            if (purchasableUnlockables.ContainsKey(terminalKeywords["buy"].compatibleNouns[j].result.creatureName.ToLower()))
                            {
                                lammOS.Logger.LogWarning("A purchasable unlockable has already been added with the name: '" + terminalKeywords["buy"].compatibleNouns[j].result.creatureName.ToLower() + "'");
                                continue;
                            }
                            purchasableUnlockables.Add(terminalKeywords["buy"].compatibleNouns[j].result.creatureName.ToLower(), new PurchasableUnlockable(unlockable, terminalKeywords["buy"].compatibleNouns[j].result));
                            foundMatch = true;
                            break;
                        }
                    }
                    if (!foundMatch && !unlockable.alreadyUnlocked && !unlockable.unlockedInChallengeFile && unlockable.unlockableType != 753)
                    {
                        lammOS.Logger.LogWarning("Could not find a matching buy node for the unlockable: '" + unlockable.unlockableName + "'" + unlockable.IsPlaceable.ToString());
                    }
                    continue;
                }
                if (purchasableUnlockables.ContainsKey(unlockable.shopSelectionNode.creatureName.ToLower()))
                {
                    lammOS.Logger.LogWarning("A purchasable unlockable has already been added with the name: '" + unlockable.shopSelectionNode.creatureName.ToLower() + "'");
                    continue;
                }
                purchasableUnlockables.Add(unlockable.shopSelectionNode.creatureName.ToLower(), new PurchasableUnlockable(unlockable, unlockable.shopSelectionNode));
            }

            foreach (string unlockableId in purchasableUnlockables.Keys)
            {
                for (int i = Mathf.Min(unlockableId.Length, CharsToAutocomplete); i < unlockableId.Length; i++)
                {
                    string shortestChars = unlockableId.Substring(0, i);
                    bool shortest = true;
                    foreach (string itemId2 in purchasableItems.Keys)
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
                        foreach (string unlockableId2 in purchasableUnlockables.Keys)
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
                            purchasableUnlockables[unlockableId].shortestChars = shortestChars;
                            break;
                        }
                    }
                }
                if (purchasableUnlockables[unlockableId].shortestChars == null)
                {
                    // TODO fix, an earlier name conflicts by being too similar (or the same) and of the same or longer length than this name
                    purchasableUnlockables[unlockableId].shortestChars = "ERR";
                }
            }
        }
        public static void AddEntity(EnemyType entity, TerminalNode entry, int entityIndex, string name = null, int entryWithoutEntityIndex = -1)
        {
            entities.Add(entity.enemyName, new Entity(entity, entry, name));
            if (entityIndex == -1)
            {
                return;
            }
            entitiesWithoutEntry.RemoveAt(entityIndex);
            if (entryWithoutEntityIndex != -1)
            {
                entriesWithoutEntity.RemoveAt(entryWithoutEntityIndex);
                return;
            }
            if (entry == null)
            {
                return;
            }
            for (int i = 0; i < entriesWithoutEntity.Count; i++)
            {
                if (entriesWithoutEntity[i].creatureFileID == entry.creatureFileID)
                {
                    entriesWithoutEntity.RemoveAt(i);
                    break;
                }
            }
        }
        internal static void LoadEntities()
        {
            int entityIndex = 0;
            entities = new();
            entriesWithoutEntity = new(NewTerminal.NewTerminal.Terminal.enemyFiles);
            while (entityIndex < entitiesWithoutEntry.Count)
            {
                EnemyType entity = entitiesWithoutEntry[entityIndex];
                switch (entity.enemyName)
                {
                    case "Centipede":
                        {
                            AddEntity(entity, NewTerminal.NewTerminal.Terminal.enemyFiles[0], entityIndex);
                            break;
                        }
                    case "Flowerman":
                        {
                            AddEntity(entity, NewTerminal.NewTerminal.Terminal.enemyFiles[1], entityIndex);
                            break;
                        }
                    case "Crawler":
                        {
                            AddEntity(entity, NewTerminal.NewTerminal.Terminal.enemyFiles[2], entityIndex);
                            break;
                        }
                    case "MouthDog":
                        {
                            AddEntity(entity, NewTerminal.NewTerminal.Terminal.enemyFiles[3], entityIndex);
                            break;
                        }
                    case "Hoarding bug":
                        {
                            AddEntity(entity, NewTerminal.NewTerminal.Terminal.enemyFiles[4], entityIndex);
                            break;
                        }
                    case "Blob":
                        {
                            AddEntity(entity, NewTerminal.NewTerminal.Terminal.enemyFiles[5], entityIndex);
                            break;
                        }
                    case "ForestGiant":
                        {
                            AddEntity(entity, NewTerminal.NewTerminal.Terminal.enemyFiles[6], entityIndex);
                            break;
                        }
                    case "Spring":
                        {
                            AddEntity(entity, NewTerminal.NewTerminal.Terminal.enemyFiles[7], entityIndex);
                            break;
                        }
                    case "Lasso":
                        {
                            AddEntity(entity, NewTerminal.NewTerminal.Terminal.enemyFiles[8], entityIndex);
                            break;
                        }
                    case "Earth Leviathan":
                        {
                            AddEntity(entity, NewTerminal.NewTerminal.Terminal.enemyFiles[9], entityIndex);
                            break;
                        }
                    case "Jester":
                        {
                            AddEntity(entity, NewTerminal.NewTerminal.Terminal.enemyFiles[10], entityIndex);
                            break;
                        }
                    case "Puffer":
                        {
                            AddEntity(entity, NewTerminal.NewTerminal.Terminal.enemyFiles[11], entityIndex);
                            break;
                        }
                    case "Bunker Spider":
                        {
                            AddEntity(entity, NewTerminal.NewTerminal.Terminal.enemyFiles[12], entityIndex);
                            break;
                        }
                    case "Manticoil":
                        {
                            AddEntity(entity, NewTerminal.NewTerminal.Terminal.enemyFiles[13], entityIndex);
                            break;
                        }
                    case "Red Locust Bees":
                        {
                            AddEntity(entity, NewTerminal.NewTerminal.Terminal.enemyFiles[14], entityIndex);
                            break;
                        }
                    case "Docile Locust Bees":
                        {
                            AddEntity(entity, NewTerminal.NewTerminal.Terminal.enemyFiles[15], entityIndex);
                            break;
                        }
                    case "Baboon hawk":
                        {
                            AddEntity(entity, NewTerminal.NewTerminal.Terminal.enemyFiles[16], entityIndex);
                            break;
                        }
                    case "Nutcracker":
                        {
                            AddEntity(entity, NewTerminal.NewTerminal.Terminal.enemyFiles[17], entityIndex);
                            break;
                        }
                    case "Girl":
                        {
                            AddEntity(entity, null, entityIndex, "Ghost girls");
                            break;
                        }
                    case "Masked":
                        {
                            AddEntity(entity, null, entityIndex);
                            break;
                        }
                    default:
                        {
                            bool foundMatch = false;
                            for (int i = 0; i < entriesWithoutEntity.Count; i++)
                            {
                                if (entriesWithoutEntity[i].creatureName.ToLower().Replace(" ", "").Replace("-", "").StartsWith(entity.enemyName.ToLower().Replace(" ", "").Replace("-", "")))
                                {
                                    AddEntity(entity, entriesWithoutEntity[i], entityIndex, entriesWithoutEntity[i].creatureName, i);
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
        }
        internal static void PostLoadingEntities()
        {
            foreach (EnemyType entity in entitiesWithoutEntry)
            {
                lammOS.Logger.LogWarning("The entity '" + entity.enemyName + "' could not successfully be matched to a bestiary entry.");
            }
            entitiesWithoutEntry.Clear();

            foreach (TerminalNode entry in entriesWithoutEntity)
            {
                if (entities.ContainsKey(entry.creatureName))
                {
                    lammOS.Logger.LogWarning("A bestiary entry for the entity '" + entry.creatureName + "' has already been added.");
                    continue;
                }
                lammOS.Logger.LogWarning("The bestiary entry for the entity '" + entry.creatureName + "' could not successfully be matched to an entity. Adding as a standalone bestiary entry.");
                entities.Add(entry.creatureName, new Entity(null, entry, entry.creatureName));
            }
            entriesWithoutEntity.Clear();

            foreach (Entity entity in entities.Values)
            {
                for (int i = Mathf.Min(entity.name.Length, CharsToAutocomplete); i < entity.name.Length; i++)
                {
                    string shortestChars = entity.name.Substring(0, i).ToLower();
                    bool shortest = true;
                    foreach (Entity entity2 in entities.Values)
                    {
                        if (entity.name == entity2.name)
                        {
                            break;
                        }

                        if (entity2.name.ToLower().StartsWith(shortestChars))
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
        internal static void LoadLogs()
        {
            logs = new();
            for (int i = 0; i < NewTerminal.NewTerminal.Terminal.logEntryFiles.Count; i++)
            {
                logs.Add(NewTerminal.NewTerminal.Terminal.logEntryFiles[i].creatureName.ToLower(), new Log(NewTerminal.NewTerminal.Terminal.logEntryFiles[i]));
            }

            foreach (string logId in logs.Keys)
            {
                for (int i = Mathf.Min(logId.Length, CharsToAutocomplete); i <= logId.Length; i++)
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
        
        internal static void RemoveVanillaKeywords()
        {
            terminalKeywordsBackup = (TerminalKeyword[])NewTerminal.NewTerminal.Terminal.terminalNodes.allKeywords.Clone();
            List<TerminalKeyword> keywords = new();
            foreach (TerminalKeyword keyword in NewTerminal.NewTerminal.Terminal.terminalNodes.allKeywords)
            {
                if (keyword.accessTerminalObjects)
                {
                    continue;
                }

                if (keyword.word == "buy" || keyword.word == "money" || keyword.word == "help" || keyword.word == "store" || keyword.word == "bestiary" || keyword.word == "reset credits" || keyword.word == "inside cam" || keyword.word == "moons" || keyword.word == "route" || keyword.word == "decor" || keyword.word == "upgrades" || keyword.word == "sigurd" || keyword.word == "storage" || keyword.word == "other" || keyword.word == "scan" || keyword.word == "monitor" || keyword.word == "switch" || keyword.word == "eject")
                {
                    continue;
                }

                if (keyword.defaultVerb != null)
                {
                    if (keyword.defaultVerb.word == "buy" || keyword.defaultVerb.word == "route")
                    {
                        continue;
                    }

                    if (keyword.defaultVerb.word == "info")
                    {
                        bool remove = false;
                        foreach (CompatibleNoun cn in terminalKeywords["info"].compatibleNouns)
                        {
                            if (cn == null || cn.noun == null || cn.result == null)
                            {
                                continue;
                            }

                            if (cn.noun.word == keyword.word && cn.result.creatureFileID != -1)
                            {
                                remove = true;
                                break;
                            }
                        }

                        if (remove)
                        {
                            continue;
                        }
                    }

                    if (keyword.defaultVerb.word == "view")
                    {
                        bool remove = false;
                        foreach (CompatibleNoun cn in terminalKeywords["view"].compatibleNouns)
                        {
                            if (cn == null || cn.noun == null || cn.result == null)
                            {
                                continue;
                            }

                            if (cn.noun.word == keyword.word && cn.result.storyLogFileID != -1)
                            {
                                remove = true;
                                break;
                            }
                        }

                        if (remove)
                        {
                            continue;
                        }
                    }
                }

                keywords.Add(keyword);
            }

            NewTerminal.NewTerminal.Terminal.terminalNodes.allKeywords = keywords.ToArray();
        }

        public static readonly int DefaultMaxDropshipItems = 12;
        public static readonly float DefaultMacroInstructionsPerSecond = 5f;

        public static int GetMoonCost(string moonId)
        {
            float multiplier = SyncedConfig.SyncedConfig.Instance.MoonPriceMultipliers.TryGetValue(moonId, out float value) ? value : -1f;
            int moon = moons[moonId].node.buyRerouteToMoon;
            moons[moonId].node.buyRerouteToMoon = -1;
            int cost = moons[moonId].node.itemCost;
            moons[moonId].node.buyRerouteToMoon = moon;
            if (multiplier == -1)
            {
                return cost;
            }
            if (cost == 0)
            {
                return (int)multiplier;
            }
            return (int)(cost * multiplier);
        }
        public static int GetItemCost(string itemId, bool includeSale = false)
        {
            float multiplier = SyncedConfig.SyncedConfig.Instance.ItemPriceMultipliers.TryGetValue(itemId, out float value) ? value : -1f;
            int cost = purchasableItems[itemId].item.creditsWorth;
            if (multiplier == -1)
            {
                if (includeSale)
                {
                    return (int)(cost * purchasableItems[itemId].salePercentage / 100f);
                }
                return cost;
            }
            if (cost == 0)
            {
                if (includeSale)
                {
                    return (int)(multiplier * purchasableItems[itemId].salePercentage / 100f);
                }
                return (int)multiplier;
            }
            if (includeSale)
            {
                return (int)(cost * multiplier * purchasableItems[itemId].salePercentage / 100f);
            }
            return (int)(cost * multiplier);
        }
        public static int GetUnlockableCost(string unlockableId)
        {
            float multiplier = SyncedConfig.SyncedConfig.Instance.UnlockablePriceMultipliers.TryGetValue(unlockableId, out float value) ? value : -1f;
            int unlockable = purchasableUnlockables[unlockableId].node.shipUnlockableID;
            purchasableUnlockables[unlockableId].node.shipUnlockableID = -1;
            int cost = purchasableUnlockables[unlockableId].node.itemCost;
            purchasableUnlockables[unlockableId].node.shipUnlockableID = unlockable;
            if (multiplier == -1)
            {
                return cost;
            }
            if (cost == 0)
            {
                return (int)multiplier;
            }
            return (int)(cost * multiplier);
        }
    }
}