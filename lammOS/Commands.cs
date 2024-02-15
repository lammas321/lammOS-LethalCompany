using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static lammOS.lammOS;
using static lammOS.Macros.Macros;
using static lammOS.NewTerminal.NewTerminal;
using static lammOS.Variables.Variables;

namespace lammOS.Commands
{
    public static class Commands
    {
        public enum BlockingLevel
        {
            None,
            UntilSubmission,
            UntilTyping,
            All
        }

        internal static Dictionary<string, Command> commands;
        internal static Dictionary<string, string> shortcuts;

        public static bool AddCommand(Command command)
        {
            if (HasCommand(command.id))
            {
                lammOS.Logger.LogError("A command with the id '" + command.id + "' has already been added.");
                return false;
            }
            if (IsShortcut(command.id))
            {
                lammOS.Logger.LogWarning("There is a shortcut that will clash with the added command with the id '" + command.id + "'.");
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
                lammOS.Logger.LogError("A shortcut using the string '" + shortcut + "' already exists.");
                return false;
            }
            if (HasCommand(shortcut))
            {
                lammOS.Logger.LogWarning("There is a command with the id '" + shortcut + "' that will be overruled by the added shortcut.");
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

        internal static void AddCommands()
        {
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
            
            AddCommand(new CodesCommand());
            AddCommand(new MonitorCommand());
            AddCommand(new TargetsCommand());
            AddCommand(new SwitchCommand());
            AddCommand(new PingCommand());
            AddCommand(new FlashCommand());
            
            AddCommand(new LandCommand());
            AddCommand(new LaunchCommand());
            AddCommand(new DoorCommand());
            AddCommand(new LightsCommand());
            AddCommand(new TeleporterCommand());
            AddCommand(new InverseTeleporterCommand());
            
            AddCommand(new ScanCommand());
            AddCommand(new TransmitCommand());
            AddCommand(new ClearCommand());
            AddCommand(new ReloadCommand());
            AddCommand(new EjectCommand());
            AddCommand(new WaitCommand());
            
            AddCommand(new MacrosCommand());
            AddCommand(new MacroCommand());
            AddCommand(new RunMacroCommand());
            AddCommand(new CreateMacroCommand());
            AddCommand(new EditMacroCommand());
            AddCommand(new DeleteMacroCommand());
            
            AddCommand(new DebugCommand());

            AddCommand(new CommandArgument("alphanumericcode", "default"));
            AddCommand(new CommandArgument("alphanumericcode", "toggle", false));
            AddCommand(new CommandArgument("alphanumericcode", "deactivate", false));
            AddCommand(new CommandArgument("alphanumericcode", "activate", false));
        }

        public static bool hasSetup { get; internal set; } = false;
        internal static void AddCodeCommands()
        {
            foreach (TerminalKeyword keyword in terminalKeywords.Values)
            {
                if (keyword.accessTerminalObjects)
                {
                    AddCommand(new AlphanumericCodeCommand(keyword.word));
                }
            }
        }
        internal static void AddCompatibilityCommands()
        {
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.malco.lethalcompany.moreshipupgrades"))
            {
                lammOS.Logger.LogInfo("Adding Lategame Upgrades compatibility commands");
                AddCommand(new LategameCompatibiltyCommand());
                AddCommand(new LguCompatibiltyCommand());
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
                args = "[Command]";
            }

            public static string GenerateHelpPage()
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

                string result = "";
                foreach (string category in categories.Keys)
                {
                    result += "\n [" + category + "]\n";

                    foreach (Command command in categories[category])
                    {
                        if (!command.enabled)
                        {
                            result += "DISABLED ";
                        }
                        result += ">" + command.id.ToUpper();
                        if (command.args != "")
                        {
                            result += " " + command.args;
                        }
                        result += "\n";
                    }
                }
                return result;
            }
            public static string GenerateCommandHelp(Command command)
            {
                string result = " [" + command.category + "]\n";
                if (!command.enabled)
                {
                    result += "DISABLED ";
                }
                result += ">" + command.id.ToUpper();

                if (command.args != "")
                {
                    result += " " + command.args;
                }

                List<string> shortcuts = new();
                foreach (string shortcut in GetShortcuts())
                {
                    if (GetCommandIdByShortcut(shortcut) == command.id)
                    {
                        shortcuts.Add(">" + shortcut.ToUpper());
                    }
                }
                if (shortcuts.Count != 0)
                {
                    result += "  -  " + string.Join(", ", shortcuts);
                }

                return result + "\n * " + command.description;
            }

            public override void Execute(Terminal terminal, string input)
            {
                if (input == "")
                {
                    SetTerminalText(GenerateHelpPage());
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
                    ErrorResponse("No command command found with id: '" + input + "'");
                    return;
                }

                SetTerminalText(GenerateCommandHelp(command));
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

            public override void Execute(Terminal terminal, string input)
            {
                int length = 0;
                foreach (string shortcut in GetShortcuts())
                {
                    Command command = GetCommand(GetCommandIdByShortcut(shortcut));

                    if (!command.hidden && command.id.Length + 1 > length)
                    {
                        length = command.id.Length + 1;
                    }
                }

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

                    categories[command.category].Add(">" + (command.id.ToUpper() + " ").PadRight(length, ListPaddingChar) + " >" + shortcut.ToUpper() + "\n");
                }

                string result = "";
                foreach (string category in categories.Keys)
                {
                    result += "\n [" + category + "]\n";

                    foreach (string shortcut in categories[category])
                    {
                        result += shortcut;
                    }
                }
                if (result == "")
                {
                    result = "There are no shortcuts for any commands.";
                }
                SetTerminalText(result);
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

            public static string GenerateMoonIndexWeather(Moon moon)
            {
                if (moon.level.currentWeather != LevelWeatherType.None)
                {
                    string weather = moon.level.currentWeather.ToString();
                    if (WeatherColors.ContainsKey(weather))
                    {
                        weather = "<color=" + WeatherColors[weather] + ">" + weather + "</color>";
                    }
                    return " (" + weather + ") " + (moon.level.spawnEnemiesAndScrap ? "" : "(" + ((int)(StartOfRound.Instance.companyBuyingRate * 100f)).ToString() + "%) ");
                }
                if (!moon.level.spawnEnemiesAndScrap)
                {
                    return " (" + ((int)(StartOfRound.Instance.companyBuyingRate * 100f)).ToString() + "%) ";
                }
                return null;
            }
            public static string GenerateMoonIndexCost(Moon moon)
            {
                int cost = GetMoonCost(moon.id);
                if (cost == 0)
                {
                    return null;
                }
                return cost.ToString();
            }
            public static void GenerateMoonIndex(Moon moon, List<List<string>> itemLists)
            {
                itemLists[0].Add((ShowMinimumChars ? "(" + moon.shortestChars + ") " : "") + (moon.name == "71 Gordion" ? "The Company building" : moon.shortName) + " ");
                itemLists[1].Add(GenerateMoonIndexWeather(moon));
                itemLists[2].Add(GenerateMoonIndexCost(moon));
            }
            public static string GenerateMoonsList(List<Moon> moonsList)
            {
                List<List<string>> itemLists = new()
                {
                    new(),
                    new(),
                    new()
                };

                foreach (Moon moon in moonsList)
                {
                    GenerateMoonIndex(moon, itemLists);
                }

                return HandleListPadding(itemLists, new List<string>() { " * {ITEM}", "{ITEM}", " ${ITEM} " }, new List<string>() { "", "", "right" } );
            }

            public override void Execute(Terminal terminal, string input)
            {
                List<Moon> moonsList = new();
                moonsList.Add(moons["company"]);
                foreach (SelectableLevel level in terminal.moonsCatalogueList)
                {
                    Moon moon = null;
                    foreach (Moon m in moons.Values)
                    {
                        if (m.level == level)
                        {
                            moon = m;
                            break;
                        }
                    }
                    if (moon == null)
                    {
                        lammOS.Logger.LogWarning(">MOONS - Couldn't generate moon index for the moon '" + level.PlanetName + "'.");
                        continue;
                    }

                    moonsList.Add(moon);
                }

                SetTerminalText("Welcome to the exomoons catalogue.\nUse the MOON [Moon] command for more details regarding a moon, and use the ROUTE <Moon> command to route the autopilot to a moon of your choice.\n" + GenerateMoonsList(moonsList));
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
                args = "[Moon]";
            }

            public static string GeneratePercentageOrRarity(int rarity, int totalRarity)
            {
                if (ShowPercentagesOrRarity == "Percentage")
                {
                    return (rarity * 100 / (float)totalRarity).ToString() + "%";
                }
                return rarity.ToString() + "/" + totalRarity.ToString();
            }

            public static string GenerateDetailedWeather(Moon moon)
            {
                string result = "\n\nTypes of Weather:";
                bool hasWeather = false;
                foreach (RandomWeatherWithVariables weather in moon.level.randomWeathers)
                {
                    if (weather.weatherType == LevelWeatherType.None)
                    {
                        continue;
                    }
                    result += "\n * " + weather.weatherType.ToString();
                    hasWeather = true;
                }
                if (!hasWeather)
                {
                    result += "\n * None";
                }
                return result;
            }
            public static string GenerateDetailedIndoors(Moon moon)
            {
                string result = "\n\nIndoor Size Multiplier: x" + moon.level.factorySizeMultiplier.ToString() + "\n\nIndoor Type Rarities:";
                if (moon.level.dungeonFlowTypes.Length == 0)
                {
                    return result + "\n * 0: " + GeneratePercentageOrRarity(1, 1);
                }

                int dungeonRarity = 0;
                foreach (IntWithRarity rarity in moon.level.dungeonFlowTypes)
                {
                    dungeonRarity += rarity.rarity;
                }
                foreach (IntWithRarity rarity in moon.level.dungeonFlowTypes)
                {
                    if (rarity.rarity == 0)
                    {
                        continue;
                    }
                    result += "\n * " + rarity.id.ToString() + ": " + GeneratePercentageOrRarity(rarity.rarity, dungeonRarity);
                }
                return result;
            }
            public static string GenerateDetailedScrap(Moon moon)
            {
                List<List<string>> itemLists = new()
                {
                    new(),
                    new()
                };

                int itemRarity = 0;
                foreach (SpawnableItemWithRarity item in moon.level.spawnableScrap)
                {
                    if (item.rarity == 0)
                    {
                        continue;
                    }
                    itemRarity += item.rarity;
                }
                foreach (SpawnableItemWithRarity item in moon.level.spawnableScrap)
                {
                    if (item.rarity == 0)
                    {
                        continue;
                    }
                    itemLists[0].Add(item.spawnableItem.itemName + " ");
                    itemLists[1].Add(GeneratePercentageOrRarity(item.rarity, itemRarity));
                }

                return "\n\nMin/Max Scrap: " + moon.level.minScrap.ToString() + "/" + moon.level.maxScrap.ToString() +
                    "\nMin/Max Scrap Value: " + moon.level.minTotalScrapValue.ToString() + "/" + moon.level.maxTotalScrapValue.ToString() +
                    "\n\nSpawnable Scrap:\n" + HandleListPadding(itemLists, new List<string>() { " * {ITEM}", " {ITEM} " }, new List<string>() { "", "right" }, false);
            }
            public static string GenerateDetailedEntityGroup(List<SpawnableEnemyWithRarity> entityList, string area)
            {
                if (entityList.Count == 0)
                {
                    return "No " + area + " entities spawn on this moon.";
                }

                List<List<string>> itemLists = new()
                {
                    new(),
                    new(),
                    new(),
                    new()
                };

                int entityRarity = 0;
                foreach (SpawnableEnemyWithRarity entity in entityList)
                {
                    if (entity.rarity == 0)
                    {
                        continue;
                    }
                    entityRarity += entity.rarity;
                }
                foreach (SpawnableEnemyWithRarity entity in entityList)
                {
                    if (entity.rarity == 0)
                    {
                        continue;
                    }
                    itemLists[0].Add(entities[entity.enemyType.enemyName].name + " ");
                    itemLists[1].Add(" (" + entity.enemyType.PowerLevel.ToString());
                    itemLists[2].Add(entity.enemyType.MaxCount.ToString() + ") ");
                    itemLists[3].Add(GeneratePercentageOrRarity(entity.rarity, entityRarity));
                }

                return HandleListPadding(itemLists, new List<string>() { " * {ITEM}", "{ITEM} :", " {ITEM}", "{ITEM} " }, new List<string>() { "", "left", "right", "right" }, false);
            }
            public static string GenerateDetailedEntities(Moon moon)
            {
                return "\n\nMax Entity Power:\n * Daytime: " + moon.level.maxDaytimeEnemyPowerCount.ToString() +
                    "\n * Indoor:  " + moon.level.maxEnemyPowerCount.ToString() +
                    "\n * Outdoor: " + moon.level.maxOutsideEnemyPowerCount.ToString() +
                    "\n\nDaytime Entities: (Power : Max Amount)\n" + GenerateDetailedEntityGroup(moon.level.DaytimeEnemies, "daytime") +
                    "\n\nIndoor Entities: (Power : Max Amount)\n" + GenerateDetailedEntityGroup(moon.level.Enemies, "indoor") +
                    "\n\nOutdoor Entities: (Power : Max Amount)\n" + GenerateDetailedEntityGroup(moon.level.OutsideEnemies, "outdoor");
            }
            public static string GenerateDetailedSafety(Moon moon)
            {
                string result = "";
                bool indoorsEvaluated = false;
                bool outdoorsEvaluated = false;
                for (float i = 0; i < 1; i += 0.05f)
                {
                    if (indoorsEvaluated && outdoorsEvaluated)
                    {
                        break;
                    }

                    if (!indoorsEvaluated && moon.level.enemySpawnChanceThroughoutDay.Evaluate(i) > 0)
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
                        result += "\n\nIndoors Safe Until Around: " + hour.ToString() + ":" + (minute.Length == 1 ? "0" : "") + minute + " " + end;
                        indoorsEvaluated = true;
                    }

                    if (!outdoorsEvaluated && moon.level.outsideEnemySpawnChanceThroughDay.Evaluate(i) > 0)
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
                        result += "\nOutdoors Safe Until Around: " + hour.ToString() + ":" + (minute.Length == 1 ? "0" : "") + minute + " " + end;
                        outdoorsEvaluated = true;
                    }
                }
                return result;
            }

            public static string GenerateDetailedResult(Moon moon)
            {
                return GenerateDetailedWeather(moon)
                    + GenerateDetailedIndoors(moon)
                    + GenerateDetailedScrap(moon)
                    + GenerateDetailedEntities(moon)
                    + GenerateDetailedSafety(moon);
            }

            public override void Execute(Terminal terminal, string input)
            {
                Moon moon = null;
                if (input == "")
                {
                    foreach (Moon m in moons.Values)
                    {
                        if (m.level == StartOfRound.Instance.currentLevel)
                        {
                            moon = m;
                            break;
                        }
                    }
                }
                else
                {
                    input = input.ToLower();
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
                        ErrorResponse("No moon goes by the name: '" + input + "'");
                        return;
                    }
                    if (CheckNotEnoughChars(input.Length, resultId.Length))
                    {
                        return;
                    }
                }

                string cost = MoonsCommand.GenerateMoonIndexCost(moon);
                string weather = MoonsCommand.GenerateMoonIndexWeather(moon);
                string result = moon.styledName + (weather ?? " ") + (cost == null ? "" : "$" + cost) + "\n\n" + moon.level.LevelDescription;

                if (moon.level.spawnEnemiesAndScrap)
                {
                    result += GenerateDetailedResult(moon);
                }

                SetTerminalText(result);
            }
        }
        public class RouteCommand : ConfirmationCommand
        {
            public Moon moon = null;

            public RouteCommand()
            {
                category = "Moons";
                id = "route";
                description = "Travel to the specified moon.";
                AddShortcut("r", id);
                args = "<Moon>";
            }

            public override void Execute(Terminal terminal, string input)
            {
                if (StartOfRound.Instance.isChallengeFile)
                {
                    ErrorResponse("You cannot route to another moon while on a challenge moon save file.");
                    return;
                }

                if (input == "")
                {
                    ErrorResponse("Please enter a moon to route the autopilot to.");
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
                    ErrorResponse("No moon goes by the name: '" + input + "'");
                    return;
                }
                if (CheckNotEnoughChars(input.Length, resultId.Length))
                {
                    return;
                }

                this.moon = moon;

                if (ShowCommandConfirmations)
                {
                    blockingLevel = BlockingLevel.UntilSubmission;
                    SetTerminalText("Would you like to route to " + moon.level.PlanetName + " for $" + GetMoonCost(resultId).ToString() + "?\nType CONFIRM to confirm routing.");
                    return;
                }

                Route(terminal);
                this.moon = null;
            }

            public void Route(Terminal terminal, bool confirmed = false)
            {
                if (moon.level == StartOfRound.Instance.currentLevel)
                {
                    ErrorResponse("You are already at that moon.");
                    return;
                }

                if (!StartOfRound.Instance.inShipPhase)
                {
                    ErrorResponse("You are only able to route to a moon while in orbit.");
                    return;
                }
                if (StartOfRound.Instance.travellingToNewLevel)
                {
                    ErrorResponse("You are already travelling elsewhere, please wait.");
                    return;
                }

                if (terminal.useCreditsCooldown)
                {
                    ErrorResponse("You're on a credit usage cooldown.");
                    return;
                }

                int cost = GetMoonCost(moon.id);
                if (terminal.groupCredits < cost)
                {
                    ErrorResponse("You do not have enough credits to go to that moon.");
                    return;
                }

                if (terminal.IsHost)
                {
                    StartOfRound.Instance.ChangeLevelServerRpc(moon.node.buyRerouteToMoon, terminal.groupCredits - cost);
                }
                else
                {
                    terminal.groupCredits -= cost;
                    StartOfRound.Instance.ChangeLevelServerRpc(moon.node.buyRerouteToMoon, terminal.groupCredits);
                }

                if (confirmed)
                {
                    AppendTerminalText("Routing autopilot to " + moon.styledName + ".");
                    return;
                }
                SetTerminalText("Routing autopilot to " + moon.styledName + ".");
            }

            public override void Confirmed(Terminal terminal)
            {
                Route(terminal, true);

                moon = null;
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

            public static string GeneratePurchasableItems()
            {
                List<List<string>> itemLists = new()
                {
                    new(),
                    new(),
                    new()
                };

                foreach (PurchasableItem purchasableItem in purchasableItems.Values)
                {
                    itemLists[0].Add((ShowMinimumChars ? "(" + purchasableItem.shortestChars + ") " : "") + purchasableItem.item.itemName + " ");
                    itemLists[1].Add(GetItemCost(purchasableItem.item.itemName.ToLower(), true).ToString());
                    itemLists[2].Add(purchasableItem.salePercentage == 100 ? null : (100 - purchasableItem.salePercentage).ToString());
                }

                return "Welcome to the Company store!\n" + NewTerminal.NewTerminal.Terminal.numberOfItemsInDropship.ToString() + "/" + SyncedConfig.SyncedConfig.Instance.MaxDropshipItems + " items are in the dropship.\nUse the BUY command to buy any items listed here:\n" + HandleListPadding(itemLists, new List<string>() { " * {ITEM}", " ${ITEM} ", "{ITEM}% OFF " }, new List<string>() { "", "right", "right" });
            }
            public static string GenerateUpgrades()
            {
                List<PurchasableUnlockable> upgrades = new();
                foreach (PurchasableUnlockable purchasableUnlockable in purchasableUnlockables.Values)
                {
                    if (purchasableUnlockable.unlockable.alwaysInStock && !purchasableUnlockable.unlockable.hasBeenUnlockedByPlayer && !purchasableUnlockable.unlockable.alreadyUnlocked)
                    {
                        upgrades.Add(purchasableUnlockable);
                    }
                }
                if (upgrades.Count == 0)
                {
                    return "\n\nShip Upgrades:\n+" + new string('-', "All ship upgrades have been purchased.".Length - 2) + "+\nAll ship upgrades have been purchased.";
                }

                List<List<string>> itemLists = new()
                {
                    new(),
                    new()
                };

                upgrades.Sort((x, y) => GetUnlockableCost(x.node.creatureName.ToLower()) - GetUnlockableCost(y.node.creatureName.ToLower()));
                foreach (PurchasableUnlockable purchasableUnlockable in upgrades)
                {
                    itemLists[0].Add((ShowMinimumChars ? "(" + purchasableUnlockable.shortestChars + ") " : "") + purchasableUnlockable.node.creatureName + " ");
                    itemLists[1].Add(GetUnlockableCost(purchasableUnlockable.node.creatureName.ToLower()).ToString());
                }

                return "\n\nShip Upgrades:\n" + HandleListPadding(itemLists, new List<string>() { " * {ITEM}", " ${ITEM} " }, new List<string>() { "", "right" });
            }
            public static string GenerateDecorSelection(Terminal terminal)
            {
                List<List<string>> itemLists = new()
                {
                    new(),
                    new()
                };

                foreach (TerminalNode decorNode in terminal.ShipDecorSelection)
                {
                    PurchasableUnlockable purchasableUnlockable = purchasableUnlockables[decorNode.creatureName.ToLower()];
                    if (purchasableUnlockable != null && !purchasableUnlockable.unlockable.hasBeenUnlockedByPlayer && !purchasableUnlockable.unlockable.alreadyUnlocked)
                    {
                        itemLists[0].Add((ShowMinimumChars ? "(" + purchasableUnlockable.shortestChars + ") " : "") + purchasableUnlockable.node.creatureName + " ");
                        itemLists[1].Add(GetUnlockableCost(purchasableUnlockable.node.creatureName.ToLower()).ToString());
                    }
                }
                if (itemLists[0].Count == 0)
                {
                    return "\n\nShip Decor:\n+" + new string('-', "No decor items available.".Length - 2) + "+\nNo decor items available.";
                }

                return "\n\nShip Decor:\n" + HandleListPadding(itemLists, new List<string>() { " * {ITEM}", " ${ITEM} " }, new List<string>() { "", "right" });
            }

            public override void Execute(Terminal terminal, string input)
            {
                SetTerminalText(GeneratePurchasableItems() + GenerateUpgrades() + GenerateDecorSelection(terminal));
            }
        }
        public class BuyCommand : ConfirmationCommand
        {
            public PurchasableItem purchasableItem = null;
            public int amount = 0;
            public PurchasableUnlockable purchasableUnlockable = null;

            public BuyCommand()
            {
                category = "Items";
                id = "buy";
                description = "Purchase any available item in the store.";
                AddShortcut("b", id);
                args = "<Item> [Amount]";
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

            public void PurchaseItem(string input, string itemInput, int amount, Terminal terminal)
            {
                PurchasableItem purchasableItem = null;
                foreach (PurchasableItem item in purchasableItems.Values)
                {
                    if (item.item.itemName.ToLower().StartsWith(itemInput))
                    {
                        purchasableItem = item;
                        break;
                    }
                }
                if (purchasableItem == null)
                {
                    PurchaseUnlockable(input, terminal);
                    return;
                }
                if (CheckNotEnoughChars(itemInput.Length, purchasableItem.item.itemName.Length))
                {
                    return;
                }

                if (amount < 1)
                {
                    ErrorResponse("You must buy at least 1 item when purchasing items.");
                    return;
                }

                this.purchasableItem = purchasableItem;
                this.amount = amount;

                if (ShowCommandConfirmations)
                {
                    blockingLevel = BlockingLevel.UntilSubmission;
                    SetTerminalText("Would you like to purchase " + purchasableItem.item.itemName + " x" + amount.ToString() + " for $" + (GetItemCost(purchasableItem.item.itemName.ToLower(), true) * amount).ToString() + "?\nType CONFIRM to confirm your purchase.");
                    return;
                }

                TryPurchaseItem(terminal);
                purchasableItem = null;
                amount = 0;
            }
            public void PurchaseUnlockable(string input, Terminal terminal)
            {
                PurchasableUnlockable purchasableUnlockable = null;
                foreach (PurchasableUnlockable unlockable in purchasableUnlockables.Values)
                {
                    if (unlockable.node.creatureName.ToLower().StartsWith(input))
                    {
                        purchasableUnlockable = unlockable;
                        break;
                    }
                }
                if (purchasableUnlockable == null)
                {
                    ErrorResponse("No purchasable item or unlockable goes by the name: '" + input + "'");
                    return;
                }
                if (CheckNotEnoughChars(input.Length, purchasableUnlockable.node.creatureName.Length))
                {
                    return;
                }

                this.purchasableUnlockable = purchasableUnlockable;

                if (ShowCommandConfirmations)
                {
                    blockingLevel = BlockingLevel.UntilSubmission;
                    SetTerminalText("Would you like to purchase a " + purchasableUnlockable.node.creatureName + " for $" + GetUnlockableCost(purchasableUnlockable.node.creatureName.ToLower()).ToString() + "?\nType CONFIRM to confirm your purchase.");
                    return;
                }

                TryPurchaseUnlockable(terminal);
                purchasableUnlockable = null;
            }

            public override void Execute(Terminal terminal, string input)
            {
                if (input == "")
                {
                    ErrorResponse("Please enter an item to purchase.");
                    return;
                }
                if (terminal.useCreditsCooldown)
                {
                    ErrorResponse("You're on a credit usage cooldown.");
                    return;
                }

                input = input.ToLower();
                ParseInput(input, out string itemInput, out int amount);

                PurchaseItem(input, itemInput, amount, terminal);
            }

            public void TryPurchaseItem(Terminal terminal, bool confirmed = false)
            {
                int cost = GetItemCost(purchasableItem.item.itemName.ToLower(), true);
                if (terminal.groupCredits < cost * amount)
                {
                    ErrorResponse("You do not have enough credits to purchase that item.");
                    return;
                }
                if (amount + terminal.numberOfItemsInDropship > SyncedConfig.SyncedConfig.Instance.MaxDropshipItems)
                {
                    ErrorResponse("There is not enough space on the dropship for these items, there are currently " + terminal.numberOfItemsInDropship.ToString() + "/" + SyncedConfig.SyncedConfig.Instance.MaxDropshipItems.ToString() + " items en route.");
                    return;
                }

                for (int i = 0; i < amount; i++)
                {
                    terminal.orderedItemsFromTerminal.Add(purchasableItem.index);
                    terminal.numberOfItemsInDropship++;
                    terminal.groupCredits = Mathf.Clamp(terminal.groupCredits - cost, 0, 10000000);

                    if (!terminal.IsHost && terminal.orderedItemsFromTerminal.Count == 12)
                    {
                        terminal.BuyItemsServerRpc(terminal.orderedItemsFromTerminal.ToArray(), terminal.groupCredits, terminal.numberOfItemsInDropship);
                        terminal.orderedItemsFromTerminal.Clear();
                    }
                }

                if (terminal.IsHost)
                {
                    terminal.SyncGroupCreditsClientRpc(terminal.groupCredits, terminal.numberOfItemsInDropship);
                }
                else
                {
                    if (terminal.orderedItemsFromTerminal.Count != 0)
                    {
                        terminal.BuyItemsServerRpc(terminal.orderedItemsFromTerminal.ToArray(), terminal.groupCredits, terminal.numberOfItemsInDropship);
                        terminal.orderedItemsFromTerminal.Clear();
                    }
                }

                PlaySyncedClip(terminalSyncedSounds["buy"]);
                if (confirmed)
                {
                    AppendTerminalText("Purchased " + purchasableItem.item.itemName + " x" + amount.ToString() + " for $" + (cost * amount).ToString() + ".");
                    return;
                }
                SetTerminalText("Purchased " + purchasableItem.item.itemName + " x" + amount.ToString() + " for $" + (cost * amount).ToString() + ".");
            }
            public void TryPurchaseUnlockable(Terminal terminal, bool confirmed = false)
            {
                if (!purchasableUnlockable.unlockable.alwaysInStock && !terminal.ShipDecorSelection.Contains(purchasableUnlockable.unlockable.shopSelectionNode))
                {
                    ErrorResponse("This unlockable is not for sale.");
                    return;
                }
                if (purchasableUnlockable.unlockable.hasBeenUnlockedByPlayer || purchasableUnlockable.unlockable.alreadyUnlocked)
                {
                    ErrorResponse("You already have this unlockable.");
                    return;
                }

                int cost = GetUnlockableCost(purchasableUnlockable.node.creatureName.ToLower());
                if (terminal.groupCredits < cost)
                {
                    ErrorResponse("You do not have enough credits to purchase that unlockable.");
                    return;
                }
                if ((!StartOfRound.Instance.inShipPhase && !StartOfRound.Instance.shipHasLanded) || StartOfRound.Instance.shipAnimator.GetCurrentAnimatorStateInfo(0).tagHash != Animator.StringToHash("ShipIdle"))
                {
                    ErrorResponse("You cannot purchase that unlockable currently.");
                    return;
                }

                HUDManager.Instance.DisplayTip("Tip", "Press B to move and place objects in the ship, E to cancel.", false, true, "LC_MoveObjectsTip");
                if (terminal.IsHost)
                {
                    StartOfRound.Instance.BuyShipUnlockableServerRpc(purchasableUnlockable.node.shipUnlockableID, Mathf.Clamp(terminal.groupCredits - cost, 0, 10000000));
                }
                else
                {
                    terminal.groupCredits = Mathf.Clamp(terminal.groupCredits - cost, 0, 10000000);
                    StartOfRound.Instance.BuyShipUnlockableServerRpc(purchasableUnlockable.node.shipUnlockableID, terminal.groupCredits);
                }

                PlaySyncedClip(terminalSyncedSounds["buy"]);
                if (confirmed)
                {
                    AppendTerminalText("Purchased " + purchasableUnlockable.node.creatureName + " for $" + cost.ToString() + ".");
                    return;
                }
                SetTerminalText("Purchased " + purchasableUnlockable.node.creatureName + " for $" + cost.ToString() + ".");
            }

            public override void Confirmed(Terminal terminal)
            {
                if (purchasableItem != null)
                {
                    TryPurchaseItem(terminal, true);

                    purchasableItem = null;
                    amount = 0;
                }
                else if (purchasableUnlockable != null)
                {
                    TryPurchaseUnlockable(terminal, true);

                    purchasableUnlockable = null;
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

            public override void Execute(Terminal terminal, string input)
            {
                List<string> unlockables = new();
                foreach (PurchasableUnlockable unlockable in purchasableUnlockables.Values)
                {
                    if (unlockable.unlockable.inStorage)
                    {
                        unlockables.Add((ShowMinimumChars ? "(" + unlockable.shortestChars + ") " : "") + unlockable.node.creatureName);
                    }
                }

                if (unlockables.Count == 0)
                {
                    SetTerminalText("Use the RETRIEVE command to take unlockables out of storage. Stored Unlockables:\n+" + new string('-', "There are no unlockables in storage.".Length - 2) + "+\nThere are no unlockables in storage.");
                    return;
                }
                SetTerminalText("Use the RETRIEVE command to take unlockables out of storage. Stored Unlockables:\n" + HandleListPadding(new() { unlockables }, new List<string>() { " * {ITEM} " }, new List<string>() { "" }));
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
                args = "<Item>";
            }

            public override void Execute(Terminal terminal, string input)
            {
                if (input == "")
                {
                    ErrorResponse("Please enter an unlockable to take out of storage.");
                    return;
                }

                input = input.ToLower();
                PurchasableUnlockable purchasableUnlockable = null;
                foreach (PurchasableUnlockable unlockable in purchasableUnlockables.Values)
                {
                    if (unlockable.node.creatureName.ToLower().StartsWith(input))
                    {
                        purchasableUnlockable = unlockable;
                        break;
                    }
                }
                if (purchasableUnlockable == null)
                {
                    ErrorResponse("No unlockable goes by the name: '" + input + "'");
                    return;
                }
                if (CheckNotEnoughChars(input.Length, purchasableUnlockable.node.creatureName.Length))
                {
                    return;
                }

                if (!purchasableUnlockable.unlockable.inStorage)
                {
                    ErrorResponse("That unlockable is not in storage.");
                    return;
                }
                if (!purchasableUnlockable.unlockable.hasBeenUnlockedByPlayer && !purchasableUnlockable.unlockable.alreadyUnlocked)
                {
                    ErrorResponse("You do not own that unlockable.");
                    return;
                }

                StartOfRound.Instance.ReturnUnlockableFromStorageServerRpc(purchasableUnlockable.node.shipUnlockableID);
                SetTerminalText("Returned unlockable from storage.");
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
                args = "[Entity]";
            }

            public string GenerateBestiaryPage(Terminal terminal)
            {
                if (terminal.scannedEnemyIDs == null || terminal.scannedEnemyIDs.Count == 0)
                {
                    return "Welcome to the Bestiary, use the BESTIARY command followed by one of the following names to view information about that entity.\n+" + new string('-', "No data collected on wildlife.".Length - 2) + "+\nNo data collected on wildlife.";
                }

                List<string> entitiesList = new();
                foreach (Entity entity in entities.Values)
                {
                    if (entity.entry != null && terminal.scannedEnemyIDs.Contains(entity.entry.creatureFileID))
                    {
                        entitiesList.Add((terminal.newlyScannedEnemyIDs.Contains(entity.entry.creatureFileID) ? "(!) " : "") + (ShowMinimumChars ? "(" + entity.shortestChars + ") " : "") + entity.name);
                    }
                }

                if (entitiesList.Count == 0)
                {
                    return "Welcome to the Bestiary, use the BESTIARY command followed by one of the following names to view information about that entity.\n+" + new string('-', "No data collected on wildlife.".Length - 2) + "+\nNo data collected on wildlife.";
                }
                return "Welcome to the Bestiary, use the BESTIARY command followed by one of the following names to view information about that entity.\n" + HandleListPadding(new() { entitiesList }, new List<string>() { " * {ITEM} " }, new List<string>() { "" });
            }
            public string GenerateEntryPage(EnemyType entityType)
            {
                string result = "\n\nPower: " + entityType.PowerLevel.ToString() +
                    "\nMax Count: " + entityType.MaxCount.ToString() +
                    "\nCan Die: " + entityType.canDie.ToString() +
                    "\nEntity Type: " + (entityType.isDaytimeEnemy ? "Daytime" : (entityType.isOutsideEnemy ? "Outside" : "Inside")) +
                    "\n\nStunnable: " + entityType.canBeStunned.ToString();

                if (entityType.canBeStunned)
                {
                    result += "\nStun Time Multiplier: " + entityType.stunTimeMultiplier.ToString() + "x";
                }
                if (!entityType.isDaytimeEnemy && !entityType.isOutsideEnemy)
                {
                    result += "\n\nDoor Open Speed: " + (1 / entityType.doorSpeedMultiplier).ToString() + "s";
                }
                return result;
            }

            public override void Execute(Terminal terminal, string input)
            {
                if (input == "")
                {
                    SetTerminalText(GenerateBestiaryPage(terminal));
                    return;
                }

                input = input.ToLower();
                Entity entity = null;
                foreach (Entity e in entities.Values)
                {
                    if (e.entry != null && e.name.ToLower().StartsWith(input) && terminal.scannedEnemyIDs.Contains(e.entry.creatureFileID))
                    {
                        entity = e;
                        break;
                    }
                }
                if (entity == null)
                {
                    ErrorResponse("No entity exists with the name: '" + input + "'");
                    return;
                }
                if (CheckNotEnoughChars(input.Length, entity.name.Length))
                {
                    return;
                }

                terminal.newlyScannedEnemyIDs.Remove(entity.entry.creatureFileID);
                PlaySyncedClip(terminalSyncedSounds["loading"]);
                terminal.LoadTerminalImage(entity.entry);
                SetTerminalText(entity.entry.displayText.TrimEnd('\n') + (entity.type == null ? "" : GenerateEntryPage(entity.type)));
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
                args = "[Log]";
            }

            public string GenerateSigurdPage(Terminal terminal)
            {
                if (terminal.unlockedStoryLogs == null || terminal.unlockedStoryLogs.Count == 0)
                {
                    return "ERROR, DATA HAS BEEN CORRUPTED OR OVERWRITTEN.";
                }

                List<string> logsList = new();
                foreach (Log log in logs.Values)
                {
                    if (terminal.unlockedStoryLogs.Contains(log.log.storyLogFileID))
                    {
                        logsList.Add((terminal.newlyUnlockedStoryLogs.Contains(log.log.storyLogFileID) ? "(!) " : "") + (ShowMinimumChars ? "(" + log.shortestChars + ") " : "") + log.log.creatureName);
                    }
                }
                return "Sigurd's Log Entries.\nRead an entry by entering SIGURD followed by the name of the entry you wish to read.\n" + HandleListPadding(new() { logsList }, new List<string>() { " * {ITEM} " }, new List<string>() { "" });
            }

            public override void Execute(Terminal terminal, string input)
            {
                if (input == "")
                {
                    SetTerminalText(GenerateSigurdPage(terminal));
                    return;
                }

                input = input.ToLower();
                Log log = null;
                foreach (string logId in logs.Keys)
                {
                    if (logId.StartsWith(input) && terminal.unlockedStoryLogs.Contains(logs[logId].log.storyLogFileID))
                    {
                        log = logs[logId];
                        break;
                    }
                }
                if (log == null)
                {
                    ErrorResponse("No log exists with the name: '" + input + "'");
                    return;
                }
                if (CheckNotEnoughChars(input.Length, log.log.creatureName.Length))
                {
                    return;
                }

                terminal.newlyUnlockedStoryLogs.Remove(log.log.storyLogFileID);
                SetTerminalText(log.log.displayText);
            }
        }
        
        public class CodesCommand : Command
        {
            public CodesCommand()
            {
                category = "Radar";
                id = "codes";
                description = "View all of the alphanumeric codes and what objects they correspond to within the building.";
                AddShortcut("co", id);
            }

            public override void Execute(Terminal terminal, string input)
            {
                List<List<string>> itemLists = new()
                {
                    new(),
                    new(),
                    new()
                };
                foreach (TerminalAccessibleObject tao in UnityEngine.Object.FindObjectsByType<TerminalAccessibleObject>(FindObjectsSortMode.None))
                {
                    itemLists[0].Add(tao.objectCode);

                    if (tao.isBigDoor)
                    {
                        itemLists[1].Add("Door");
                        itemLists[2].Add(tao.isDoorOpen ? "(Open)" : "(Closed)");
                        continue;
                    }
                    if (tao.name == "Landmine")
                    {
                        itemLists[1].Add("Landmine");
                    }
                    else if (tao.name == "TurretScript")
                    {
                        itemLists[1].Add("Turret");
                    }
                    else
                    {
                        itemLists[1].Add(tao.name);
                    }

                    itemLists[2].Add(tao.inCooldown ? "(Deactivated)" : "(Active)");
                }

                if (itemLists[0].Count == 0)
                {
                    SetTerminalText("This is the full list of alphanumeric codes on the current moon and what objects they're bound to.\nYou can enter one of the following alphanumeric codes to interact with the object they're bound to, or enter HELP B3 to see some other options.\n+" + new string('-', "There are no alphanumeric codes.".Length - 2) + "+\nThere are no alphanumeric codes.");
                    return;
                }
                SetTerminalText("This is the full list of alphanumeric codes on the current moon and what objects they're bound to.\nYou can enter one of the following alphanumeric codes to interact with the object they're bound to, or enter HELP B3 to see some other options.\n" + HandleListPadding(itemLists, new List<string>() { " * {ITEM} : ", " {ITEM} ", " {ITEM} " }, new List<string>() { "", "", "" }));
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

            public override void Execute(Terminal terminal, string input)
            {
                SetTerminalText("");
                if (StartOfRound.Instance.inShipPhase || terminal.displayingPersistentImage == StartOfRound.Instance.mapScreen.cam.targetTexture)
                {
                    terminal.terminalImage.enabled = false;
                    terminal.terminalImage.texture = null;
                    terminal.displayingPersistentImage = null;
                    SetBodyHelmetCameraVisibility();
                    return;
                }
                terminal.terminalImage.enabled = true;
                terminal.terminalImage.texture = StartOfRound.Instance.mapScreen.cam.targetTexture;
                terminal.displayingPersistentImage = StartOfRound.Instance.mapScreen.cam.targetTexture;
                SetBodyHelmetCameraVisibility();
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

            public override void Execute(Terminal terminal, string input)
            {
                List<List<string>> itemLists = new()
                {
                    new(),
                    new()
                };
                foreach (TransformAndName target in StartOfRound.Instance.mapScreen.radarTargets)
                {
                    PlayerControllerB component = target.transform.gameObject.GetComponent<PlayerControllerB>();
                    if (component != null && !component.isPlayerControlled && !component.isPlayerDead && component.redirectToEnemy == null)
                    {
                        continue;
                    }

                    itemLists[0].Add(target.name);
                    itemLists[1].Add(target.isNonPlayer ? "(Radar Booster)" : null);
                }

                SetTerminalText("These are radar targets you can switch to using the SWITCH command, or by using the left and right arrow keys with the monitor on screen after using the MONITOR command. Radar boosters can be flashed and pinged by using the FLASH and PING commands.\n" + HandleListPadding(itemLists, new List<string>() { " * {ITEM} ", " {ITEM} " }, new List<string>() { "", "" }));
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
                args = "[Radar target]";
            }

            public override void Execute(Terminal terminal, string input)
            {
                if (input == "")
                {
                    StartOfRound.Instance.mapScreen.SwitchRadarTargetAndSync((StartOfRound.Instance.mapScreen.targetTransformIndex + 1) % StartOfRound.Instance.mapScreen.radarTargets.Count);
                    SetTerminalText("");
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
                    ErrorResponse("No radar target goes by the name: '" + input + "'");
                    return;
                }
                if (CheckNotEnoughChars(input.Length, StartOfRound.Instance.mapScreen.radarTargets[target].name.Length))
                {
                    return;
                }

                StartOfRound.Instance.mapScreen.SwitchRadarTargetAndSync(target);
                SetTerminalText("");
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
                args = "[Radar booster]";
            }

            public override void Execute(Terminal terminal, string input)
            {
                if (input == "")
                {
                    if (!StartOfRound.Instance.mapScreen.radarTargets[StartOfRound.Instance.mapScreen.targetTransformIndex].isNonPlayer)
                    {
                        ErrorResponse("You can only ping radar boosters.");
                        return;
                    }
                    StartOfRound.Instance.mapScreen.PingRadarBooster(StartOfRound.Instance.mapScreen.targetTransformIndex);
                    SetTerminalText("");
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
                    ErrorResponse("No radar booster goes by the name: '" + input + "'");
                    return;
                }
                if (CheckNotEnoughChars(input.Length, StartOfRound.Instance.mapScreen.radarTargets[target].name.Length))
                {
                    return;
                }

                if (!StartOfRound.Instance.mapScreen.radarTargets[target].isNonPlayer)
                {
                    ErrorResponse("You can only ping radar boosters.");
                    return;
                }
                StartOfRound.Instance.mapScreen.PingRadarBooster(target);
                SetTerminalText("");
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
                args = "[Radar booster]";
            }

            public override void Execute(Terminal terminal, string input)
            {
                if (input == "")
                {
                    if (!StartOfRound.Instance.mapScreen.radarTargets[StartOfRound.Instance.mapScreen.targetTransformIndex].isNonPlayer)
                    {
                        ErrorResponse("You can only flash radar boosters.");
                        return;
                    }
                    StartOfRound.Instance.mapScreen.FlashRadarBooster(StartOfRound.Instance.mapScreen.targetTransformIndex);
                    SetTerminalText("");
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
                    ErrorResponse("No radar booster goes by the name: '" + input + "'");
                    return;
                }
                if (CheckNotEnoughChars(input.Length, StartOfRound.Instance.mapScreen.radarTargets[target].name.Length))
                {
                    return;
                }

                if (!StartOfRound.Instance.mapScreen.radarTargets[target].isNonPlayer)
                {
                    ErrorResponse("You can only flash radar boosters.");
                    return;
                }
                StartOfRound.Instance.mapScreen.FlashRadarBooster(target);
                SetTerminalText("");
            }
        }
        
        public class LandCommand : ConfirmationCommand
        {
            public LandCommand()
            {
                category = "Interactive";
                id = "land";
                description = "Lands the ship on the moon you are currently orbiting.";
            }

            public override void Execute(Terminal terminal, string input)
            {
                if (ShowCommandConfirmations)
                {
                    blockingLevel = BlockingLevel.UntilSubmission;
                    SetTerminalText("Would you like to pull the lever to land on " + StartOfRound.Instance.currentLevel.PlanetName + "?\nType CONFIRM to confirm landing.");
                    return;
                }

                Land(terminal);
            }

            public void Land(Terminal terminal, bool confirmed = false)
            {
                if (StartOfRound.Instance.travellingToNewLevel)
                {
                    ErrorResponse("The ship is currently routing to another moon.");
                    return;
                }
                if (StartOfRound.Instance.shipHasLanded)
                {
                    ErrorResponse("The ship is already on the moon.");
                    return;
                }
                if (!GameNetworkManager.Instance.gameHasStarted && !GameNetworkManager.Instance.isHostingGame)
                {
                    ErrorResponse("The host must be the one to land the ship at the start of the game.");
                    return;
                }

                StartMatchLever lever = UnityEngine.Object.FindAnyObjectByType<StartMatchLever>();
                if (lever.leverHasBeenPulled)
                {
                    ErrorResponse("The lever has already been pulled.");
                    return;
                }
                if (!lever.triggerScript.interactable)
                {
                    ErrorResponse("The lever cannot currently be pulled.");
                    return;
                }

                lever.LeverAnimation();
                lever.PullLever();
                if (confirmed)
                {
                    AppendTerminalText("Pulling the lever to land on " + StartOfRound.Instance.currentLevel.PlanetName + "...");
                    return;
                }
                SetTerminalText("Pulling the lever to land on " + StartOfRound.Instance.currentLevel.PlanetName + "...");
            }

            public override void Confirmed(Terminal terminal)
            {
                Land(terminal, true);
            }
        }
        public class LaunchCommand : ConfirmationCommand
        {
            public LaunchCommand()
            {
                category = "Interactive";
                id = "launch";
                description = "Launches the ship into orbit around the moon it is currently on.";
            }

            public override void Execute(Terminal terminal, string input)
            {
                if (ShowCommandConfirmations)
                {
                    blockingLevel = BlockingLevel.UntilSubmission;
                    SetTerminalText("Would you like to pull the lever to launch into orbit?\nType CONFIRM to confirm landing.");
                    return;
                }

                Launch(terminal);
            }

            public void Launch(Terminal terminal, bool confirmed = false)
            {
                if (StartOfRound.Instance.travellingToNewLevel)
                {
                    ErrorResponse("The ship is currently routing to another moon.");
                    return;
                }
                if (!StartOfRound.Instance.shipHasLanded)
                {
                    ErrorResponse("The ship is either still landing, already in orbit, or currently leaving the moon.");
                    return;
                }
                if (!GameNetworkManager.Instance.gameHasStarted && !GameNetworkManager.Instance.isHostingGame)
                {
                    ErrorResponse("The host must be the one to land the ship at the start of the game.");
                    return;
                }

                StartMatchLever lever = UnityEngine.Object.FindAnyObjectByType<StartMatchLever>();
                if (!lever.leverHasBeenPulled)
                {
                    ErrorResponse("The lever has already been pulled.");
                    return;
                }
                if (!lever.triggerScript.interactable)
                {
                    ErrorResponse("The lever cannot currently be pulled.");
                    return;
                }

                lever.LeverAnimation();
                lever.PullLever();
                if (confirmed)
                {
                    AppendTerminalText("Pulling the lever to launch into orbit...");
                    return;
                }
                SetTerminalText("Pulling the lever to launch into orbit...");
            }

            public override void Confirmed(Terminal terminal)
            {
                Launch(terminal, true);
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

            public override void Execute(Terminal terminal, string input)
            {
                GameObject.Find(StartOfRound.Instance.hangarDoorsClosed ? "StartButton" : "StopButton").GetComponentInChildren<InteractTrigger>().onInteract.Invoke(GameNetworkManager.Instance.localPlayerController);
                SetTerminalText("");
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

            public override void Execute(Terminal terminal, string input)
            {
                StartOfRound.Instance.shipRoomLights.ToggleShipLights();
                SetTerminalText("");
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

            public override void Execute(Terminal terminal, string input)
            {
                foreach (ShipTeleporter teleporter in UnityEngine.Object.FindObjectsByType<ShipTeleporter>(FindObjectsSortMode.None))
                {
                    if (!teleporter.isInverseTeleporter)
                    {
                        if (teleporter.IsSpawned && teleporter.isActiveAndEnabled)
                        {
                            if (teleporter.buttonTrigger.interactable)
                            {
                                teleporter.PressTeleportButtonOnLocalClient();
                                SetTerminalText("");
                                return;
                            }
                            ErrorResponse("The teleporter is on cooldown.");
                            return;
                        }
                        ErrorResponse("The teleporter is in storage.");
                        return;
                    }
                }
                ErrorResponse("You do not own a teleporter.");
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

            public override void Execute(Terminal terminal, string input)
            {
                foreach (ShipTeleporter teleporter in UnityEngine.Object.FindObjectsByType<ShipTeleporter>(FindObjectsSortMode.None))
                {
                    if (teleporter.isInverseTeleporter)
                    {
                        if (teleporter.IsSpawned && teleporter.isActiveAndEnabled)
                        {
                            if (teleporter.buttonTrigger.interactable)
                            {
                                teleporter.PressTeleportButtonOnLocalClient();
                                SetTerminalText("");
                                return;
                            }
                            ErrorResponse("The inverse teleporter is on cooldown.");
                            return;
                        }
                        ErrorResponse("The inverse teleporter is in storage.");
                        return;
                    }
                }
                ErrorResponse("You do not own an inverse teleporter.");
            }
        }
        
        public class ScanCommand : Command
        {
            public ScanCommand()
            {
                category = "Other";
                id = "scan";
                description = "View the amount of items and total value remaining outside the ship.";
                AddShortcut("scrap", id);
                AddShortcut("sc", id);
            }

            public override void Execute(Terminal terminal, string input)
            {
                int shipCount = 0, shipValue = 0, indoorCount = 0, indoorValue = 0, outdoorCount = 0, outdoorValue = 0;

                List<List<string>> indoorLists = new()
                {
                    new(),
                    new()
                };
                List<List<string>> outdoorLists = new()
                {
                    new(),
                    new()
                };
                
                GrabbableObject[] objects = UnityEngine.Object.FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None);
                foreach (GrabbableObject item in objects)
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
                        indoorLists[0].Add(item.itemProperties.itemName);
                        indoorLists[1].Add(item.scrapValue.ToString());
                        continue;
                    }

                    outdoorCount++;
                    outdoorValue += item.scrapValue;
                    outdoorLists[0].Add(item.itemProperties.itemName);
                    outdoorLists[1].Add(item.scrapValue.ToString());
                }

                string indoors = HandleListPadding(indoorLists, new List<string>() { " * {ITEM} ", " ${ITEM} " }, new List<string>() { "", "right" }, false) + "\n";

                SetTerminalText("Ship: " + shipCount.ToString() + " scrap with a value of $" + shipValue.ToString() +
                    "\n\nIndoors: " + indoorCount.ToString() + " scrap with a value of $" + indoorValue.ToString() + "\n" + (indoors == "\n" ? "" : indoors) +
                    "\nOutdoors: " + outdoorCount.ToString() + " scrap with a value of $" + outdoorValue.ToString() + "\n" + HandleListPadding(outdoorLists, new List<string>() { " * {ITEM} ", " ${ITEM} " }, new List<string>() { "", "right" }, false));
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
                args = "(9 characters)";
            }

            public override void Execute(Terminal terminal, string input)
            {
                SignalTranslator signalTranslator = UnityEngine.Object.FindAnyObjectByType<SignalTranslator>();
                if (signalTranslator == null)
                {
                    ErrorResponse("You do not own a signal translator.");
                    return;
                }
                if (UnityEngine.Time.realtimeSinceStartup - signalTranslator.timeLastUsingSignalTranslator <= 8f)
                {
                    ErrorResponse("The signal translator is still in use.");
                    return;
                }

                string text = input.Substring(0, Mathf.Min(input.Length, 9));
                if (string.IsNullOrEmpty(text))
                {
                    ErrorResponse("Please enter a 9 character or less message to send.");
                    return;
                }

                SetTerminalText("Transmitting message...");
                if (!terminal.IsHost)
                {
                    signalTranslator.timeLastUsingSignalTranslator = UnityEngine.Time.realtimeSinceStartup;
                }
                HUDManager.Instance.UseSignalTranslatorServerRpc(text);
            }
        }
        public class ClearCommand : Command
        {
            public ClearCommand()
            {
                category = "Other";
                id = "clear";
                description = "Clear all text from the terminal.";
                AddShortcut("clr", id);
                AddShortcut("cls", id);
                AddShortcut("c", id);
            }

            public override void Execute(Terminal terminal, string input)
            {
                SetTerminalText("");
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

            public override void Execute(Terminal terminal, string input)
            {
                lammOS.Load();

                SetTerminalText("Reloaded.");
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

            public override void Execute(Terminal terminal, string input)
            {
                if (!terminal.IsHost)
                {
                    ErrorResponse("You must be the host to eject the crew.");
                    return;
                }
                if (StartOfRound.Instance.isChallengeFile)
                {
                    ErrorResponse("You are unable to be ejected on challenge moons.");
                    return;
                }
                if (!StartOfRound.Instance.inShipPhase)
                {
                    ErrorResponse("You must be in orbit to be ejected.");
                    return;
                }
                if (StartOfRound.Instance.firingPlayersCutsceneRunning)
                {
                    ErrorResponse("Your crew is already being ejected.");
                    return;
                }

                if (ShowCommandConfirmations)
                {
                    blockingLevel = BlockingLevel.UntilSubmission;
                    PlaySyncedClip(terminalSyncedSounds["warning"]);
                    SetTerminalText("Are you sure you want to eject your crew? There is no going back.\nType CONFIRM to confirm your decision.");
                    return;
                }

                StartOfRound.Instance.ManuallyEjectPlayersServerRpc();
                SetTerminalText("");
            }

            public override void Confirmed(Terminal terminal)
            {
                StartOfRound.Instance.ManuallyEjectPlayersServerRpc();
                SetTerminalText("");
            }
        }
        public class WaitCommand : Command
        {
            public Coroutine waitCoroutine = null;

            public WaitCommand()
            {
                category = "Other";
                id = "wait";
                description = "Waits x number of seconds until normal execution may continue, will wait 1 second with no arguments or if the provided arguments are not a valid number.";
                args = "[Seconds]";
            }

            public IEnumerator Wait(float time = 1f)
            {
                blockingLevel = BlockingLevel.All;
                yield return new WaitForSeconds(time);
                blockingLevel = BlockingLevel.None;
                waitCoroutine = null;
                currentCommand = null;
                AppendTerminalText("Waited " + time.ToString() + " seconds.");
            }

            public override void Execute(Terminal terminal, string input)
            {
                float time = 1f;
                if (input != "")
                {
                    float.TryParse(input, out time);
                }

                waitCoroutine = terminal.StartCoroutine(Wait(time));
            }

            public override void Handle(Terminal terminal, string input)
            {
                SetInputText(terminal.currentText.Substring(inputIndex));
            }

            public override void QuitTerminal(Terminal terminal)
            {
                blockingLevel = BlockingLevel.None;
                if (waitCoroutine != null)
                {
                    terminal.StopCoroutine(waitCoroutine);
                    waitCoroutine = null;
                }
                currentCommand = null;
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

            public override void Execute(Terminal terminal, string input)
            {
                List<string> macroList = GetMacroIds();
                if (macroList.Count == 0)
                {
                    SetTerminalText("Here are all of your personal macros.\nThey can be inspected using the MACRO command, and ran using the RUN-MACRO command.\n+" + new string('-', "You have no macros, create one with the CREATE-MACRO command.".Length - 2) + "+\nYou have no macros, create one with the CREATE-MACRO command.");
                    return;
                }
                SetTerminalText("Here are all of your personal macros.\nThey can be inspected using the MACRO command, and ran using the RUN-MACRO command.\n" + HandleListPadding(new() { macroList }, new List<string>() { " * {ITEM} " }, new List<string>() { "" }));
            }
        }
        public class MacroCommand : Command
        {
            public MacroCommand()
            {
                category = "Macros";
                id = "macro";
                description = "View the instructions that are ran for one of your macros.";
                AddShortcut("m-", id);
                args = "<Macro>";
            }

            public override void Execute(Terminal terminal, string input)
            {
                if (input == "")
                {
                    ErrorResponse("You must enter a macro id.");
                    return;
                }

                input = input.ToLower();
                if (!HasMacro(input))
                {
                    ErrorResponse("No macro exists with that id.");
                    return;
                }

                SetTerminalText("Macro: " + input + "\nInstructions:\n" + HandleListPadding(new() { GetMacro(input) }, new List<string>() { " * {ITEM} " }, new List<string>() { "" }, false));
            }
        }
        public class RunMacroCommand : Command
        {
            public Coroutine macroCoroutine = null;

            public RunMacroCommand()
            {
                category = "Macros";
                id = "run-macro";
                description = "Run one of your macros.";
                AddShortcut("-", id);
                args = "<Macro>";
            }

            public IEnumerator RunMacro(Terminal terminal, string macroId)
            {
                yield return new WaitForEndOfFrame();
                saveSubmissionsToHistory = false;

                foreach (string instruction in GetMacro(macroId))
                {
                    float start = Time.time;
                    if (currentCommand == this)
                    {
                        currentCommand = null;
                    }
                    SetInputText(instruction);
                    terminal.OnSubmit();

                    yield return new WaitUntil(() => currentCommand == null || currentCommand.blockingLevel <= BlockingLevel.UntilSubmission);
                    if (currentCommand == null)
                    {
                        currentCommand = this;
                    }
                    ForceScrollbar(-1);

                    float time = 1f / DefaultMacroInstructionsPerSecond - (Time.time - start);
                    if (time > 0)
                    {
                        yield return new WaitForSeconds(time);
                    }
                }

                saveSubmissionsToHistory = true;
                blockingLevel = BlockingLevel.None;
                macroCoroutine = null;
                currentCommand = null;
            }

            public override void Execute(Terminal terminal, string input)
            {
                if (macroCoroutine != null)
                {
                    ErrorResponse("You cannot run a macro within a macro.");
                    return;
                }

                if (input == "")
                {
                    ErrorResponse("You must enter a macro id.");
                    return;
                }

                input = input.ToLower();
                if (!HasMacro(input))
                {
                    ErrorResponse("No macro exists with that id.");
                    return;
                }

                blockingLevel = BlockingLevel.UntilTyping;
                macroCoroutine = terminal.StartCoroutine(RunMacro(terminal, input));
                SetTerminalText("Executing the macro with the id '" + input + "'");
            }

            public override void Handle(Terminal terminal, string input)
            {
                saveSubmissionsToHistory = true;
                blockingLevel = BlockingLevel.None;
                if (macroCoroutine != null)
                {
                    terminal.StopCoroutine(macroCoroutine);
                    macroCoroutine = null;
                }
                currentCommand = null;
                ErrorResponse("Macro execution interrupted by key press.");
            }

            public override void QuitTerminal(Terminal terminal)
            {
                saveSubmissionsToHistory = true;
                blockingLevel = BlockingLevel.None;
                if (macroCoroutine != null)
                {
                    terminal.StopCoroutine(macroCoroutine);
                    macroCoroutine = null;
                }
                currentCommand = null;
                ErrorResponse("Macro execution interrupted by leaving the terminal.");
            }
        }
        public class CreateMacroCommand : Command
        {
            public CreateMacroCommand()
            {
                category = "Macros";
                id = "create-macro";
                description = "Create a new macro.";
                AddShortcut("c-", id);
                args = "<Macro> (Instructions split by ';')";
            }

            public override void Execute(Terminal terminal, string input)
            {
                if (input == "")
                {
                    ErrorResponse("You must enter a macro id.");
                    return;
                }

                int offset = input.IndexOf(' ');
                if (offset == -1)
                {
                    ErrorResponse("You must enter intructions for the macro to run, each one separated by ';'.");
                    return;
                }

                string macroId = input.Substring(0, offset).ToLower();
                if (HasMacro(macroId))
                {
                    ErrorResponse("A macro already exists with that id.");
                    return;
                }

                List<string> instructions = new(input.Substring(offset + 1).Split(';'));
                List<string> finalInstructions = new();
                foreach (string instruction in instructions)
                {
                    string trimmedInstruction = instruction.Trim();
                    if (trimmedInstruction != "")
                    {
                        finalInstructions.Add(trimmedInstruction);
                    }
                }

                if (finalInstructions.Count == 0)
                {
                    ErrorResponse("You must enter intructions for the macro to run, each one separated by ';'.");
                    return;
                }

                AddMacro(macroId, finalInstructions);
                Save();

                SetTerminalText("Created a new macro with the id '" + macroId + "' with the following instructions:\n" + HandleListPadding(new() { finalInstructions }, new List<string>() { " * {ITEM} " }, new List<string>() { "" }, false));
            }
        }
        public class EditMacroCommand : Command
        {
            public EditMacroCommand()
            {
                category = "Macros";
                id = "edit-macro";
                description = "Edit one of your macros.";
                AddShortcut("e-", id);
                args = "<Macro> (Instructions split by ';')";
            }

            public override void Execute(Terminal terminal, string input)
            {
                if (input == "")
                {
                    ErrorResponse("You must enter a macro id.");
                    return;
                }

                int offset = input.IndexOf(' ');
                if (offset == -1)
                {
                    ErrorResponse("You must enter intructions for the macro to run, each one separated by ';'.");
                    return;
                }

                string macroId = input.Substring(0, offset).ToLower();
                if (!HasMacro(macroId))
                {
                    ErrorResponse("No macro exists with that id.");
                    return;
                }

                List<string> instructions = new(input.Substring(offset + 1).Split(';'));
                List<string> finalInstructions = new();
                foreach (string instruction in instructions)
                {
                    string trimmedInstruction = instruction.Trim();
                    if (trimmedInstruction != "")
                    {
                        finalInstructions.Add(trimmedInstruction);
                    }
                }

                if (finalInstructions.Count == 0)
                {
                    ErrorResponse("You must enter intructions for the macro to run, each one separated by ';'.");
                    return;
                }

                ModifyMacro(macroId, finalInstructions);
                Save();

                SetTerminalText("Edited the macro with the id '" + macroId + "' giving it the new instructions:\n" + HandleListPadding(new() { finalInstructions }, new List<string>() { " * {ITEM} " }, new List<string>() { "" }, false));
            }
        }
        public class DeleteMacroCommand : Command
        {
            public DeleteMacroCommand()
            {
                category = "Macros";
                id = "delete-macro";
                description = "Delete one of your macros.";
                AddShortcut("d-", id);
                args = "<Macro>";
            }

            public override void Execute(Terminal terminal, string input)
            {
                if (input == "")
                {
                    ErrorResponse("You must enter a macro id.");
                    return;
                }

                input = input.ToLower();
                if (!HasMacro(input))
                {
                    ErrorResponse("No macro exists with that id.");
                    return;
                }

                RemoveMacro(input);
                Save();

                SetTerminalText("Deleted the macro with the id '" + input + "'.");
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

            public static void LogShortestChars()
            {
                string text = "- Shortest Characters -\nEntities:";
                foreach (Entity entity in entities.Values)
                {
                    text += "\n * " + entity.name + "   (" + entity.shortestChars + ")";
                }
                text += "\n\nMoons:";
                foreach (Moon moon in moons.Values)
                {
                    text += "\n * " + moon.name + "   (" + moon.shortestChars + ")";
                }
                text += "\n\nPurchasable Items:";
                foreach (PurchasableItem item in purchasableItems.Values)
                {
                    text += "\n * " + item.item.itemName + "   (" + item.shortestChars + ")";
                }
                text += "\n\nPurchasable Unlockables:";
                foreach (PurchasableUnlockable unlockable in purchasableUnlockables.Values)
                {
                    text += "\n * " + unlockable.node.creatureName + "   (" + unlockable.shortestChars + ")";
                }
                text += "\n\nSigurd Logs:";
                foreach (Log log in logs.Values)
                {
                    text += "\n * " + log.log.creatureName + "   (" + log.shortestChars + ")";
                }
                lammOS.Logger.LogInfo(text);
            }

            public static string GetKeyword(TerminalKeyword keyword, int indent = 2)
            {
                if (keyword == null)
                {
                    return "Keyword: null";
                }
                string text = "Keyword: " + keyword.word +
                "\n" + new string(' ', indent) + "accessTerminalObjects: " + keyword.accessTerminalObjects.ToString() +
                "\n" + new string(' ', indent) + "defaultVerb: " + (keyword.defaultVerb == null ? "null" : keyword.defaultVerb.word) +
                "\n" + new string(' ', indent) + "isVerb: " + keyword.isVerb.ToString();

                if (keyword.specialKeywordResult == null)
                {
                    text += "\n\n" + new string(' ', indent) + "specialKeywordResult: null";
                }
                else
                {
                    text += "\n\n" + new string(' ', indent) + "specialKeywordResult: " + GetNode(keyword.specialKeywordResult, indent + 2);
                }
                
                text += "\n" + new string(' ', indent) + "compatibleNouns:";
                if (keyword.compatibleNouns == null || keyword.compatibleNouns.Length == 0)
                {
                    text += " null";
                }
                else
                {
                    for (int i = 0; i < keyword.compatibleNouns.Length; i++)
                    {
                        text += "\n" + new string(' ', indent) + i.ToString() + ":\n" + new string(' ', indent) + GetKeyword(keyword.compatibleNouns[i].noun, indent + 2) + "\n" + new string(' ', indent) + GetNode(keyword.compatibleNouns[i].result, indent + 2);
                    }
                }

                return text;
            }
            public static string GetNode(TerminalNode node, int indent = 2)
            {
                if (node == null)
                {
                    return "Node: null";
                }

                string text = "Node:\n" + new string(' ', indent) + "displayText: " + node.displayText +
                "\n" + new string(' ', indent) + "clearPreviousText: " + node.clearPreviousText.ToString() +
                "\n" + new string(' ', indent) + "terminalEvent: " + node.terminalEvent +
                "\n" + new string(' ', indent) + "acceptAnything: " + node.acceptAnything.ToString() +
                "\n" + new string(' ', indent) + "isConfirmationNode: " + node.isConfirmationNode.ToString() +
                "\n" + new string(' ', indent) + "maxCharactersToType: " + node.maxCharactersToType.ToString() +

                "\n\n" + new string(' ', indent) + "itemCost: " + node.itemCost.ToString() +
                "\n" + new string(' ', indent) + "buyItemIndex: " + node.buyItemIndex.ToString() +
                "\n" + new string(' ', indent) + "buyRerouteToMoon: " + node.buyRerouteToMoon.ToString() +

                "\n\n" + new string(' ', indent) + "buyUnlockable: " + node.buyUnlockable.ToString() +
                "\n" + new string(' ', indent) + "returnFromStorage: " + node.returnFromStorage.ToString() +
                "\n" + new string(' ', indent) + "shipUnlockableID: " + node.shipUnlockableID.ToString() +

                "\n\n" + new string(' ', indent) + "displayPlanetInfo: " + node.displayPlanetInfo.ToString() +
                "\n" + new string(' ', indent) + "storyLogFileID: " + node.storyLogFileID.ToString() +
                "\n" + new string(' ', indent) + "creatureFileID: " + node.creatureFileID.ToString() +
                "\n" + new string(' ', indent) + "creatureName: " + node.creatureName +

                "\n\n" + new string(' ', indent) + "playClip(null?): " + (node.playClip == null ? "null" : "not null") +
                "\n" + new string(' ', indent) + "playSyncedClip: " + node.playSyncedClip.ToString() +
                "\n" + new string(' ', indent) + "displayTexture(null?): " + (node.displayTexture == null ? "null" : "not null") +
                "\n" + new string(' ', indent) + "loadImageSlowly: " + node.loadImageSlowly.ToString() +
                "\n" + new string(' ', indent) + "persistentImage: " + node.persistentImage.ToString() +
                "\n" + new string(' ', indent) + "displayVideo(null?): " + (node.displayVideo == null ? "null" : "not null") +

                "\n\n" + new string(' ', indent) + "overrideOptions: " + node.overrideOptions.ToString() +
                "\n" + new string(' ', indent) + "terminalOptions:";
                if (node.terminalOptions == null || node.terminalOptions.Length == 0)
                {
                    text += " null";
                }
                else
                {
                    for (int i = 0; i < node.terminalOptions.Length; i++)
                    {
                        text += "\n" + new string(' ', indent) + i.ToString() + ":\n" + new string(' ', indent) + GetKeyword(node.terminalOptions[i].noun, indent + 2) + "\n" + new string(' ', indent) + GetNode(node.terminalOptions[i].result, indent + 2);
                    }
                }
                
                return text;
            }

            public override void Execute(Terminal terminal, string input)
            {
                input = input.ToLower();
                int split = input.IndexOf(' ');
                if (split == -1)
                {
                    split = input.Length;
                }
                string args = input.Substring(split).TrimStart(' ');
                input = input.Substring(0, split);

                switch (input)
                {
                    case "shortest_chars":
                        {
                            LogShortestChars();
                            break;
                        }
                    case "reboot":
                        {
                            ES3.Save("HasUsedTerminal", value: false, "LCGeneralSaveData");
                            break;
                        }
                    case "play_synced":
                        {
                            if (args == "")
                            {
                                break;
                            }

                            int index = -1;
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
                    case "set_percent":
                        {
                            if (!terminal.IsHost)
                            {
                                break;
                            }

                            if (args == "")
                            {
                                break;
                            }

                            int split2 = args.IndexOf(' ');
                            if (split2 == -1)
                            {
                                break;
                            }

                            int index = -1;
                            int.TryParse(args.Substring(0, split2), out index);
                            if (index < 0)
                            {
                                break;
                            }
                            if (index >= terminal.itemSalesPercentages.Length)
                            {
                                break;
                            }

                            int percent = -1;
                            int.TryParse(args.Substring(split2 + 1), out percent);
                            if (percent < 10)
                            {
                                break;
                            }
                            if (percent > 100)
                            {
                                break;
                            }

                            terminal.itemSalesPercentages[index] = percent;
                            break;
                        }
                    case "money":
                        {
                            if (!terminal.IsHost)
                            {
                                break;
                            }

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
                            lammOS.Logger.LogInfo(keywords);
                            break;
                        }
                    case "keyword":
                        {
                            int index = -1;
                            int.TryParse(args, out index);
                            if (index < 0 || index >= terminal.terminalNodes.allKeywords.Length)
                            {
                                lammOS.Logger.LogInfo("Not within range.");
                                return;
                            }

                            lammOS.Logger.LogInfo(GetKeyword(terminal.terminalNodes.allKeywords[index]));
                            break;
                        }
                    case "nodes":
                        {
                            lammOS.Logger.LogInfo("Nodes: " + terminal.terminalNodes.terminalNodes.Count.ToString());
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

                            lammOS.Logger.LogInfo(GetNode(terminal.terminalNodes.terminalNodes[index]));
                            break;
                        }
                    case "specialnodes":
                        {
                            lammOS.Logger.LogInfo("Special Nodes: " + terminal.terminalNodes.specialNodes.Count.ToString());
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

                            lammOS.Logger.LogInfo(GetNode(terminal.terminalNodes.specialNodes[index]));
                            break;
                        }
                }
                SetTerminalText("");
            }
        }

        public abstract class ConfirmationCommand : Command
        {
            public abstract void Confirmed(Terminal terminal);

            public override void Handle(Terminal terminal, string input)
            {
                blockingLevel = BlockingLevel.None;
                if ("confirm".StartsWith(input.ToLower()))
                {
                    Confirmed(terminal);
                    return;
                }
                AppendTerminalText("");
            }
        }
        
        public class AlphanumericCodeCommand : Command
        {
            public AlphanumericCodeCommand(string alphanumericCode)
            {
                category = "Alphanumeric Code";
                id = alphanumericCode;
                description = "An alphanumeric code command.\n\nDefault:\nDefault functionality.\n\nToggle:\nOnly toggle the state of big doors.\n\nDeactivate:\nOnly deactivate turrets and landmines.\n\nActivate:\nDetonate landmines and make turrets go berserk.\nNOTE: This action is not recommended for use by\nThe Company for being too funny.";
                args = "[Default|Toggle|Deactivate|Activate]";
                hidden = true;
            }

            public bool ExecuteActivateAction(TerminalAccessibleObject tao)
            {
                Turret turret = tao.gameObject.GetComponent<Turret>();
                if (turret != null)
                {
                    ((IHittable)turret).Hit(0, Vector3.zero);
                    return true;
                }

                Landmine landmine = tao.gameObject.GetComponent<Landmine>();
                if (landmine != null)
                {
                    ((IHittable)landmine).Hit(0, Vector3.zero);
                    return true;
                }

                return false;
            }
            public void ExecuteAction(string action)
            {
                TerminalAccessibleObject[] taos = UnityEngine.Object.FindObjectsByType<TerminalAccessibleObject>(FindObjectsSortMode.None);
                switch (action)
                {
                    case "toggle":
                        {
                            foreach (TerminalAccessibleObject tao in taos)
                            {
                                if (tao.objectCode == id && tao.isBigDoor)
                                {
                                    tao.CallFunctionFromTerminal();
                                }
                            }
                            break;
                        }
                    case "deactivate":
                        {
                            foreach (TerminalAccessibleObject tao in taos)
                            {
                                if (tao.objectCode == id && !tao.isBigDoor)
                                {
                                    tao.CallFunctionFromTerminal();
                                }
                            }
                            break;
                        }
                    case "activate":
                            {
                            foreach (TerminalAccessibleObject tao in taos)
                            {
                                if (tao.objectCode == id && !tao.isBigDoor)
                                {
                                    ExecuteActivateAction(tao);
                                }
                            }
                            break;
                        }
                    default:
                        {
                            foreach (TerminalAccessibleObject tao in taos)
                            {
                                if (tao.objectCode == id)
                                {
                                    tao.CallFunctionFromTerminal();
                                }
                            }
                            break;
                        }
                }
            }

            public override void Execute(Terminal terminal, string input)
            {
                string action = "default";
                if (input != "")
                {
                    input = input.ToLower();
                    List<string> actions = new()
                    {
                        "default",
                        "toggle",
                        "deactivate",
                        "activate"
                    };

                    foreach (string a in actions)
                    {
                        if (a.ToLower().StartsWith(input))
                        {
                            action = a;
                            break;
                        }
                    }
                    if (action != "default" && CheckNotEnoughChars(input.Length, action.Length))
                    {
                        return;
                    }
                }
                if (!GetCommand("alphanumericcode+" + action).enabled)
                {
                    ErrorResponse("That type of action has not been enabled by the host.");
                    return;
                }

                ExecuteAction(action);
                terminal.codeBroadcastAnimator.SetTrigger("display");
                PlayClip(terminal.codeBroadcastSFX);
                SetTerminalText("");
            }
        }
        public class CommandArgument : Command
        {
            public CommandArgument(string commandId, string argument, bool enabled = true)
            {
                id = commandId + "+" + argument;
                description = "A command argument for the '" + commandId + "' command.";
                hidden = true;
                defaultEnabled = enabled;
            }

            public override void Execute(Terminal terminal, string args)
            {
                currentCommand = null;
            }
        }

        public class LategameCompatibiltyCommand : CompatibilityCommand
        {
            public LategameCompatibiltyCommand()
            {
                category = "Lategame Upgrades";
                id = "lategame";
                description = "Displays information related to the Lategame Upgrades mod.";
            }
        }
        public class LguCompatibiltyCommand : CompatibilityCommand
        {
            public LguCompatibiltyCommand()
            {
                category = "Lategame Upgrades";
                id = "lgu";
                description = "Displays the purchasable upgrades from the Lategame Upgrades store.";
            }
        }

        public class CompatibilityCommand : Command
        {
            public override void Execute(Terminal terminal, string args)
            {
                currentCommand = null;
            }
        }

        public abstract class Command
        {
            public string category = "Uncategorized";
            public string id { get; internal set; } = "";
            public string description = "";
            public string args = "";
            public BlockingLevel blockingLevel = BlockingLevel.None;
            public bool hidden = false;
            internal bool defaultEnabled = true;
            public bool enabled
            {
                get
                {
                    return SyncedConfig.SyncedConfig.Instance.EnabledCommands.ContainsKey(id) ? SyncedConfig.SyncedConfig.Instance.EnabledCommands[id] : defaultEnabled;
                }
            }

            public static void ErrorResponse(string response)
            {
                SetTerminalText("<color=#ff1f00>" + response + "</color>");
                PlaySyncedClip(terminalSyncedSounds["error"]);
            }
            public static bool CheckNotEnoughChars(int inputLength, int resultLength)
            {
                if (inputLength < Mathf.Min(CharsToAutocomplete, resultLength))
                {
                    ErrorResponse("Not enough characters were input to autocomplete a result. The current requirement is " + CharsToAutocomplete.ToString() + " characters.");
                    return true;
                }
                return false;
            }

            public static string RemoveRichText(string text)
            {
                int startIndex = -1;
                for (int i = 0; i < text.Length; i++)
                {
                    if (startIndex == -1)
                    {
                        if (text[i] == '<')
                        {
                            startIndex = i;
                        }
                        continue;
                    }
                    if (text[i] == '>')
                    {
                        text = text.Substring(0, startIndex) + text.Substring(i + 1);
                        i = startIndex - 1;
                        startIndex = -1;
                    }
                }
                return text;
            }
            public static string PadRichTextLeft(string text, int desiredLength, char padding = ' ')
            {
                for (int i = RemoveRichText(text).Length; i < desiredLength; i++)
                {
                    text = padding + text;
                }
                return text;
            }
            public static string PadRichTextRight(string text, int desiredLength, char padding = ' ')
            {
                for (int i = RemoveRichText(text).Length; i < desiredLength; i++)
                {
                    text += padding;
                }
                return text;
            }
            public static string HandleListPadding(List<List<string>> itemLists, List<string> itemFormats, List<string> itemAlignments, bool separator = true)
            {
                int totalLength = 0;
                List<int> lengths = new();
                for (int i = 0; i < itemLists.Count; i++)
                {
                    List<string> items = itemLists[i];
                    int length = 0;

                    for (int j = 0; j < items.Count; j++)
                    {
                        if (items[j] == null)
                        {
                            continue;
                        }
                        int itemLength = RemoveRichText(itemFormats[i].Replace("{ITEM}", items[j])).Length;
                        if (itemLength > length)
                        {
                            length = itemLength;
                        }
                    }
                    totalLength += length;
                    length -= RemoveRichText(itemFormats[i].Replace("{ITEM}", "")).Length;
                    lengths.Add(length);
                }

                List<string> result = new();
                for (int i = 0; i < itemLists[0].Count; i++)
                {
                    bool hasContent = false;
                    string text = "";
                    for (int j = itemLists.Count - 1; j >= 0; j--)
                    {
                        if (itemLists[j][i] == null)
                        {
                            if (hasContent)
                            {
                                text = new string(ListPaddingChar, lengths[j]) + text;
                            }
                            continue;
                        }

                        switch (itemAlignments[j])
                        {
                            case "right":
                                {
                                    itemLists[j][i] = PadRichTextLeft(itemLists[j][i], lengths[j]);
                                    break;
                                }
                            case "rightChar":
                                {
                                    if (hasContent)
                                    {
                                        itemLists[j][i] = PadRichTextLeft(itemLists[j][i], lengths[j], ListPaddingChar);
                                    }
                                    break;
                                }
                            case "left":
                                {
                                    itemLists[j][i] = PadRichTextRight(itemLists[j][i], lengths[j]);
                                    break;
                                }
                            default:
                                {
                                    if (hasContent)
                                    {
                                        itemLists[j][i] = PadRichTextRight(itemLists[j][i], lengths[j], ListPaddingChar);
                                    }
                                    break;
                                }
                        }
                        hasContent = true;

                        text = itemFormats[j].Replace("{ITEM}", itemLists[j][i]) + text;
                    }

                    result.Add(text);
                }

                string output = string.Join('\n', result);
                if (separator)
                {
                    output = "+" + new string('-', Mathf.Min(MaxTerminalLineWidth, totalLength) - 2) + "+\n" + output;
                }
                return output;
            }

            public abstract void Execute(Terminal terminal, string args);

            public virtual void Handle(Terminal terminal, string input)
            {
                blockingLevel = BlockingLevel.None;
            }

            public virtual void QuitTerminal(Terminal terminal)
            {
                blockingLevel = BlockingLevel.None;
            }
        }
    }
}