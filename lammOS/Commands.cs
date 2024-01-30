using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static lammOS.lammOS;
using static lammOS.Macros.Macros;
using static lammOS.Variables.Variables;

namespace lammOS.Commands
{
    public static class Commands
    {
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

        public static Command currentCommand { get; internal set; } = null;
        public static Coroutine runningMacroCoroutine { get; internal set; } = null;
        public static bool macroAppendingText = false;

        internal static void HandleCommand(Terminal terminal, string input, ref TerminalNode node)
        {
            if (currentCommand == null)
            {
                int offset = input.IndexOf(' ');
                if (offset == -1)
                {
                    offset = input.Length;
                }

                Command.Handle(input.Substring(0, offset), terminal, input.Substring(offset == input.Length ? offset : offset + 1), ref node);
            }
            else if (currentCommand is ConfirmationCommand)
            {
                ConfirmationCommand.Handle(terminal, input, ref node);
                currentCommand = null;
            }
        }
        internal static void HandleCommandResult(Terminal terminal, TerminalNode node)
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

            AddCommand(new MonitorCommand());
            AddCommand(new TargetsCommand());
            AddCommand(new SwitchCommand());
            AddCommand(new PingCommand());
            AddCommand(new FlashCommand());

            AddCommand(new CodesCommand());
            AddCommand(new DummyCommand("Alphanumeric Codes", "code-default", "Default functionality, toggles the state of doors and disables landmines and turrets on the radar map with the given code.", "", true));
            AddCommand(new DummyCommand("Alphanumeric Codes", "code-toggle", "Specifically only toggles the state of doors on the radar map with the given code.", "", true, false));
            AddCommand(new DummyCommand("Alphanumeric Codes", "code-deactivate", "Specifically only deactivates mines and turrets on the radar map with the given code.", "", true, false));
            AddCommand(new DummyCommand("Alphanumeric Codes", "code-activate", "Specifically only activates mines and turrets on the radar map with the given code, causing them to explode or go haywire. The Company does not recommend using this option.", "", true, false));
            AddCommand(new DummyCommand("Alphanumeric Codes", "code", "Interact with objects on the radar map by typing in their alphanumeric code, such as 'B3', 'H5', ect. Enter 'HELP CODE-<Argument>' to see if an argument is enabled or disabled by the host.\n\nDefault:\n * " + GetCommand("code-default").description + "\n\nToggle:\n * " + GetCommand("code-toggle").description + "\n\nDeactivate:\n * " + GetCommand("code-deactivate").description + "\n\nActivate:\n * " + GetCommand("code-activate").description, "[Default|Toggle|Deactivate|Activate]"));

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

            AddCommand(new MacrosCommand());
            AddCommand(new MacroCommand());
            AddCommand(new RunMacroCommand());
            AddCommand(new CreateMacroCommand());
            AddCommand(new EditMacroCommand());
            AddCommand(new DeleteMacroCommand());
            AddCommand(new DummyCommand("Macros", "wait", "This command doesn't do anything normally, and is only supposed to be used within macros. When used within a macro, ", "[Seconds]"));

            AddCommand(new DebugCommand());
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    node.displayText = GenerateHelpPage();
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
                    ErrorResponse("No command command found with id: '" + input + "'", node);
                    return;
                }

                node.displayText = GenerateCommandHelp(command);
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
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

                    categories[command.category].Add(">" + (command.id.ToUpper() + " ").PadRight(length, ListPaddingCharValue) + " >" + shortcut.ToUpper() + "\n");
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

            public static string GenerateMoonIndexWeather(Moon moon)
            {
                if (moon.level.currentWeather != LevelWeatherType.None)
                {
                    return " (" + moon.level.currentWeather.ToString() + ") ";
                }
                return "";
            }
            public static string GenerateMoonIndexCost(Moon moon)
            {
                int cost = GetMoonCost(moon.id);
                if (cost != 0)
                {
                    return cost.ToString();
                }
                return "";
            }
            public static string GenerateMoonIndex(Moon moon, int nameLength, int weatherLength, int costLength)
            {
                string name = "";
                if (ShowMinimumCharsValue)
                {
                    name = "(" + moon.shortestChars + ") ";
                }
                name = moon.shortName + " ";
                string weather = GenerateMoonIndexWeather(moon);
                string cost = GenerateMoonIndexCost(moon);
                if (weather == "")
                {
                    if (cost == "")
                    {
                        return name;
                    }
                    else
                    {
                        return name.PadRight(nameLength, ListPaddingCharValue) + new string(ListPaddingCharValue, weatherLength) + " $" + cost.PadLeft(costLength);
                    }
                }
                else
                {
                    if (cost == "")
                    {
                        return name.PadRight(nameLength, ListPaddingCharValue) + weather;
                    }
                    else
                    {
                        return name.PadRight(nameLength, ListPaddingCharValue) + weather.PadRight(weatherLength, ListPaddingCharValue) + " $" + cost.PadLeft(costLength);
                    }
                }
            }
            public static string GenerateMoonsList(List<Moon> moonsList, int nameLength, int weatherLength, int costLength)
            {
                string result = " * ";
                if (ShowMinimumCharsValue)
                {
                    result += "(" + moons["company"].shortestChars + ") ";
                }
                result += "The Company Building ".PadRight(nameLength, ListPaddingCharValue);

                int cost = GetMoonCost("company");
                if (cost == 0)
                {
                    result += " (" + ((int)(StartOfRound.Instance.companyBuyingRate * 100f)).ToString() + "%)";
                }
                else
                {
                    result += (" (" + ((int)(StartOfRound.Instance.companyBuyingRate * 100f)).ToString() + "%) ").PadRight(weatherLength, ListPaddingCharValue) + " $" + cost.ToString().PadLeft(costLength);
                }

                foreach (Moon moon in moonsList)
                {
                    result += "\n * " + GenerateMoonIndex(moon, nameLength, weatherLength, costLength);
                }
                return result;
            }

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                node.displayText = "Welcome to the exomoons catalogue.\nUse the MOON [Moon] command for more details regarding a moon, and use the ROUTE <Moon> command to route the autopilot to a moon of your choice.\n";

                int nameLength = 21;
                if (ShowMinimumCharsValue)
                {
                    nameLength += ("(" + moons["company"].shortestChars + ") ").Length;
                }
                int weatherLength = 3 + ((int)(StartOfRound.Instance.companyBuyingRate * 100f)).ToString().Length;
                int costLength = 0;

                List<Moon> moonsList = new();
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

                    string name = moon.shortName;
                    if (ShowMinimumCharsValue)
                    {
                        name = "(" + moon.shortestChars + ") " + name;
                    }
                    if (name.Length + 1 > nameLength)
                    {
                        nameLength = name.Length + 1;
                    }

                    string weather = GenerateMoonIndexWeather(moon);
                    if (weather.Length > weatherLength)
                    {
                        weatherLength = weather.Length;
                    }

                    string cost = GenerateMoonIndexCost(moon);
                    if (cost.Length > costLength)
                    {
                        costLength = cost.Length;
                    }

                    moonsList.Add(moon);
                }

                node.displayText += "+" + new string('-', nameLength + weatherLength + costLength + 4) + "+\n" + GenerateMoonsList(moonsList, nameLength, weatherLength, costLength);
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
                if (ShowPercentagesOrRarityValue == "Percentage")
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
                string result = "\n\nMin/Max Scrap: " + moon.level.minScrap.ToString() + "/" + moon.level.maxScrap.ToString() +
                    "\nMin/Max Scrap Value: " + moon.level.minTotalScrapValue.ToString() + "/" + moon.level.maxTotalScrapValue.ToString() +
                    "\n\nSpawnable Scrap:";

                int itemRarity = 0;
                int nameLength = 0;
                foreach (SpawnableItemWithRarity item in moon.level.spawnableScrap)
                {
                    if (item.rarity == 0)
                    {
                        continue;
                    }
                    itemRarity += item.rarity;
                    if (item.spawnableItem.itemName.Length + 1 > nameLength)
                    {
                        nameLength = item.spawnableItem.itemName.Length + 1;
                    }
                }
                int chanceLength = 0;
                foreach (SpawnableItemWithRarity item in moon.level.spawnableScrap)
                {
                    if (item.rarity == 0)
                    {
                        continue;
                    }
                    string chance = GeneratePercentageOrRarity(item.rarity, itemRarity);
                    if (chance.Length > chanceLength)
                    {
                        chanceLength = chance.Length;
                    }
                }
                foreach (SpawnableItemWithRarity item in moon.level.spawnableScrap)
                {
                    if (item.rarity == 0)
                    {
                        continue;
                    }
                    result += "\n * " + (item.spawnableItem.itemName + " ").PadRight(nameLength, ListPaddingCharValue) + " " + GeneratePercentageOrRarity(item.rarity, itemRarity).PadLeft(chanceLength);
                }
                return result;
            }
            public static string GenerateDetailedEntities(Moon moon)
            {
                string result = "\n\nMax Entity Power:\n * Daytime: " + moon.level.maxDaytimeEnemyPowerCount.ToString() +
                    "\n * Indoor: " + moon.level.maxEnemyPowerCount.ToString() +
                    "\n * Outdoor: " + moon.level.maxOutsideEnemyPowerCount.ToString() +
                    "\n\nDaytime Entities: (Power : Max)";

                if (moon.level.DaytimeEnemies.Count == 0)
                {
                    result += "\nNo daytime entities spawn on this moon.";
                }
                else
                {
                    int nameLength = 0;
                    int daytimeEntityRarity = 0;
                    foreach (SpawnableEnemyWithRarity entity in moon.level.DaytimeEnemies)
                    {
                        if (entity.rarity == 0)
                        {
                            continue;
                        }
                        daytimeEntityRarity += entity.rarity;

                        string name = entities[entity.enemyType.enemyName].name + " (" + entity.enemyType.PowerLevel.ToString() + " : " + entity.enemyType.MaxCount.ToString() + ") ";
                        if (name.Length > nameLength)
                        {
                            nameLength = name.Length;
                        }
                    }
                    int chanceLength = 0;
                    foreach (SpawnableEnemyWithRarity entity in moon.level.DaytimeEnemies)
                    {
                        if (entity.rarity == 0)
                        {
                            continue;
                        }
                        string chance = GeneratePercentageOrRarity(entity.rarity, daytimeEntityRarity);
                        if (chance.Length > chanceLength)
                        {
                            chanceLength = chance.Length;
                        }
                    }
                    foreach (SpawnableEnemyWithRarity entity in moon.level.DaytimeEnemies)
                    {
                        if (entity.rarity == 0)
                        {
                            continue;
                        }
                        result += "\n * " + (entities[entity.enemyType.enemyName].name + " (" + entity.enemyType.PowerLevel.ToString() + " : " + entity.enemyType.MaxCount.ToString() + ") ").PadRight(nameLength, ListPaddingCharValue) + " "
                            + GeneratePercentageOrRarity(entity.rarity, daytimeEntityRarity).PadLeft(chanceLength);
                    }
                }

                result += "\n\nInside Entities: (Power : Max)";
                if (moon.level.Enemies.Count == 0)
                {
                    result += "\nNo inside entities spawn on this moon.";
                }
                else
                {
                    int nameLength = 0;
                    int insideEntityRarity = 0;
                    foreach (SpawnableEnemyWithRarity entity in moon.level.Enemies)
                    {
                        if (entity.rarity == 0)
                        {
                            continue;
                        }
                        insideEntityRarity += entity.rarity;

                        string name = entities[entity.enemyType.enemyName].name + " (" + entity.enemyType.PowerLevel.ToString() + " : " + entity.enemyType.MaxCount.ToString() + ") ";
                        if (name.Length > nameLength)
                        {
                            nameLength = name.Length;
                        }
                    }
                    int chanceLength = 0;
                    foreach (SpawnableEnemyWithRarity entity in moon.level.Enemies)
                    {
                        if (entity.rarity == 0)
                        {
                            continue;
                        }
                        string chance = GeneratePercentageOrRarity(entity.rarity, insideEntityRarity);
                        if (chance.Length > chanceLength)
                        {
                            chanceLength = chance.Length;
                        }
                    }
                    foreach (SpawnableEnemyWithRarity entity in moon.level.Enemies)
                    {
                        if (entity.rarity == 0)
                        {
                            continue;
                        }
                        result += "\n * " + (entities[entity.enemyType.enemyName].name + " (" + entity.enemyType.PowerLevel.ToString() + " : " + entity.enemyType.MaxCount.ToString() + ") ").PadRight(nameLength, ListPaddingCharValue) + " "
                            + GeneratePercentageOrRarity(entity.rarity, insideEntityRarity).PadLeft(chanceLength);
                    }
                }

                result += "\n\nOutside Entities: (Power : Max)";
                if (moon.level.Enemies.Count == 0)
                {
                    result += "\nNo outside entities spawn on this moon.";
                }
                else
                {
                    int nameLength = 0;
                    int outsideEntityRarity = 0;
                    foreach (SpawnableEnemyWithRarity entity in moon.level.OutsideEnemies)
                    {
                        if (entity.rarity == 0)
                        {
                            continue;
                        }
                        outsideEntityRarity += entity.rarity;

                        string name = entities[entity.enemyType.enemyName].name + " (" + entity.enemyType.PowerLevel.ToString() + " : " + entity.enemyType.MaxCount.ToString() + ") ";
                        if (name.Length > nameLength)
                        {
                            nameLength = name.Length;
                        }
                    }
                    int chanceLength = 0;
                    foreach (SpawnableEnemyWithRarity entity in moon.level.OutsideEnemies)
                    {
                        if (entity.rarity == 0)
                        {
                            continue;
                        }
                        string chance = GeneratePercentageOrRarity(entity.rarity, outsideEntityRarity);
                        if (chance.Length > chanceLength)
                        {
                            chanceLength = chance.Length;
                        }
                    }
                    foreach (SpawnableEnemyWithRarity entity in moon.level.OutsideEnemies)
                    {
                        if (entity.rarity == 0)
                        {
                            continue;
                        }
                        result += "\n * " + (entities[entity.enemyType.enemyName].name + " (" + entity.enemyType.PowerLevel.ToString() + " : " + entity.enemyType.MaxCount.ToString() + ") ").PadRight(nameLength, ListPaddingCharValue) + " "
                            + GeneratePercentageOrRarity(entity.rarity, outsideEntityRarity).PadLeft(chanceLength);
                    }
                }
                return result;
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
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
                        ErrorResponse("No moon goes by the name: '" + input + "'", node);
                        return;
                    }
                    if (CheckNotEnoughChars(input.Length, resultId.Length, node))
                    {
                        return;
                    }
                }

                node.displayText = moon.styledName + MoonsCommand.GenerateMoonIndexWeather(moon) + MoonsCommand.GenerateMoonIndexCost(moon) + "\n\n" + moon.level.LevelDescription;

                if (moon.level.spawnEnemiesAndScrap)
                {
                    node.displayText += GenerateDetailedResult(moon);
                }
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                if (StartOfRound.Instance.isChallengeFile)
                {
                    ErrorResponse("You cannot route to another moon while on a challenge moon save file.", node);
                    return;
                }

                if (input == "")
                {
                    ErrorResponse("Please enter a moon to route the autopilot to.", node);
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
                    ErrorResponse("No moon goes by the name: '" + input + "'", node);
                    return;
                }
                if (CheckNotEnoughChars(input.Length, resultId.Length, node))
                {
                    return;
                }

                this.moon = moon;

                if (ShowCommandConfirmationsValue)
                {
                    currentCommand = this;
                    node.displayText = "Would you like to route to " + moon.level.PlanetName + " for $" + GetMoonCost(resultId).ToString() + "?\nType CONFIRM to confirm routing.";
                    return;
                }

                Route(terminal, ref node);
                this.moon = null;
            }

            public void Route(Terminal terminal, ref TerminalNode node)
            {
                if (moon.level == StartOfRound.Instance.currentLevel)
                {
                    ErrorResponse("You are already at that moon.", node);
                    return;
                }

                if (!StartOfRound.Instance.inShipPhase)
                {
                    ErrorResponse("You are only able to route to a moon while in orbit.", node);
                    return;
                }
                if (StartOfRound.Instance.travellingToNewLevel)
                {
                    ErrorResponse("You are already travelling elsewhere, please wait.", node);
                    return;
                }

                if (terminal.useCreditsCooldown)
                {
                    ErrorResponse("You're on a credit usage cooldown.", node);
                    return;
                }

                int cost = GetMoonCost(moon.id);
                if (terminal.groupCredits < cost)
                {
                    ErrorResponse("You do not have enough credits to go to that moon.", node);
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
                node.displayText = "Routing autopilot to " + moon.styledName + ".";
            }

            public override void Confirmed(Terminal terminal, ref TerminalNode node)
            {
                node.clearPreviousText = false;

                Route(terminal, ref node);

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

            public static string GeneratePurchasableItems(Terminal terminal)
            {
                string result = "Welcome to the Company store!\n" + terminal.numberOfItemsInDropship.ToString() + "/" + SyncedConfig.SyncedConfig.Instance.MaxDropshipItemsValue + " items are in the dropship.\nUse the BUY command to buy any items listed here:\n";
                int nameLength = 0;
                int costLength = 0;
                int discountLength = 0;
                foreach (PurchasableItem purchasableItem in purchasableItems.Values)
                {
                    string name = "";
                    if (ShowMinimumCharsValue)
                    {
                        name = "(" + purchasableItem.shortestChars + ") ";
                    }
                    name += purchasableItem.item.itemName + " ";

                    if (name.Length > nameLength)
                    {
                        nameLength = name.Length;
                    }

                    string costString = GetItemCost(purchasableItem.item.itemName.ToLower()).ToString();
                    if (costString.Length > costLength)
                    {
                        costLength = costString.Length;
                    }

                    if (purchasableItem.salePercentage != 100)
                    {
                        string text = "   " + (100 - purchasableItem.salePercentage).ToString() + "% OFF";
                        if (text.Length > discountLength)
                        {
                            discountLength = text.Length;
                        }
                    }
                }

                string finalText = "";
                foreach (PurchasableItem purchasableItem in purchasableItems.Values)
                {
                    string text = "";
                    if (ShowMinimumCharsValue)
                    {
                        text = "(" + purchasableItem.shortestChars + ") ";
                    }

                    text += purchasableItem.item.itemName + " ";
                    text = text.PadRight(nameLength, ListPaddingCharValue) + " $" + GetItemCost(purchasableItem.item.itemName.ToLower(), true).ToString().PadLeft(costLength);

                    if (purchasableItem.salePercentage != 100)
                    {
                        text += "   " + (100 - purchasableItem.salePercentage).ToString() + "% OFF";
                    }
                    finalText += "\n * " + text;
                }
                return result + "+" + new string('-', nameLength + costLength + discountLength + 4) + "+" + finalText;
            }
            public static string GenerateUpgrades()
            {
                int nameLength = 0;
                int costLength = 0;
                List<PurchasableUnlockable> upgrades = new();
                foreach (PurchasableUnlockable purchasableUnlockable in purchasableUnlockables.Values)
                {
                    if (purchasableUnlockable.unlockable.alwaysInStock && !purchasableUnlockable.unlockable.hasBeenUnlockedByPlayer && !purchasableUnlockable.unlockable.alreadyUnlocked)
                    {
                        upgrades.Add(purchasableUnlockable);
                        string name = "";
                        if (ShowMinimumCharsValue)
                        {
                            name = "(" + purchasableUnlockable.shortestChars + ") ";
                        }
                        name += purchasableUnlockable.node.creatureName + " ";

                        if (name.Length > nameLength)
                        {
                            nameLength = name.Length;
                        }

                        string costString = GetUnlockableCost(purchasableUnlockable.node.creatureName.ToLower()).ToString();
                        if (costString.Length > costLength)
                        {
                            costLength = costString.Length;
                        }
                    }
                }
                upgrades.Sort((x, y) => GetUnlockableCost(x.node.creatureName.ToLower()) - GetUnlockableCost(y.node.creatureName.ToLower()));

                string result = "\n\nShip Upgrades:\n";
                if (upgrades.Count == 0)
                {
                    return result + "+------------------------------------+\nAll ship upgrades have been purchased.";
                }

                string finalText = "";
                foreach (PurchasableUnlockable purchasableUnlockable in upgrades)
                {
                    string text = "";
                    if (ShowMinimumCharsValue)
                    {
                        text = "(" + purchasableUnlockable.shortestChars + ") ";
                    }
                    text += purchasableUnlockable.node.creatureName + " ";
                    finalText += "\n * " + text.PadRight(nameLength, ListPaddingCharValue) + " $" + GetUnlockableCost(purchasableUnlockable.node.creatureName.ToLower()).ToString().PadLeft(costLength);
                }
                return result + "+" + new string('-', nameLength + costLength + 4) + "+" + finalText;
            }
            public static string GenerateDecorSelection(Terminal terminal)
            {
                string result = "\n\nShip Decor:\n";
                int nameLength = 0;
                int costLength = 0;
                bool decorAvailable = false;
                foreach (TerminalNode decorNode in terminal.ShipDecorSelection)
                {
                    PurchasableUnlockable purchasableUnlockable = purchasableUnlockables[decorNode.creatureName.ToLower()];
                    if (purchasableUnlockable != null && !purchasableUnlockable.unlockable.hasBeenUnlockedByPlayer && !purchasableUnlockable.unlockable.alreadyUnlocked)
                    {
                        decorAvailable = true;
                        string name = "";
                        if (ShowMinimumCharsValue)
                        {
                            name = "(" + purchasableUnlockable.shortestChars + ") ";
                        }
                        name += purchasableUnlockable.node.creatureName + " ";

                        if (name.Length > nameLength)
                        {
                            nameLength = name.Length;
                        }

                        string costString = GetUnlockableCost(purchasableUnlockable.node.creatureName.ToLower()).ToString();
                        if (costString.Length > costLength)
                        {
                            costLength = costString.Length;
                        }
                    }
                }
                if (!decorAvailable)
                {
                    return result + "+-----------------------+\nNo decor items available.";
                }

                string finalText = "";
                foreach (TerminalNode decorNode in terminal.ShipDecorSelection)
                {
                    PurchasableUnlockable purchasableUnlockable = purchasableUnlockables[decorNode.creatureName.ToLower()];
                    if (!purchasableUnlockable.unlockable.hasBeenUnlockedByPlayer && !purchasableUnlockable.unlockable.alreadyUnlocked)
                    {
                        string text = "";
                        if (ShowMinimumCharsValue)
                        {
                            text = "(" + purchasableUnlockable.shortestChars + ") ";
                        }

                        text += purchasableUnlockable.node.creatureName + " ";
                        finalText += "\n * " + text.PadRight(nameLength, ListPaddingCharValue) + " $" + GetUnlockableCost(purchasableUnlockable.node.creatureName.ToLower()).ToString().PadLeft(costLength);
                    }
                }
                return result + "+" + new string('-', nameLength + costLength + 4) + "+" + finalText;
            }

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                node.displayText = GeneratePurchasableItems(terminal)
                    + GenerateUpgrades()
                    + GenerateDecorSelection(terminal);
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

            public void PurchaseItem(string input, string itemInput, int amount, Terminal terminal, ref TerminalNode node)
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
                    PurchaseUnlockable(input, terminal, ref node);
                    return;
                }
                if (CheckNotEnoughChars(itemInput.Length, purchasableItem.item.itemName.Length, node))
                {
                    return;
                }

                if (amount < 1)
                {
                    ErrorResponse("You must buy at least 1 item when purchasing items.", node);
                    return;
                }

                this.purchasableItem = purchasableItem;
                this.amount = amount;

                if (ShowCommandConfirmationsValue)
                {
                    currentCommand = this;
                    node.displayText = "Would you like to purchase " + purchasableItem.item.itemName + " x" + amount.ToString() + " for $" + (GetItemCost(purchasableItem.item.itemName.ToLower(), true) * amount).ToString() + "?\nType CONFIRM to confirm your purchase.";
                    return;
                }

                TryPurchaseItem(terminal, ref node);
                purchasableItem = null;
                amount = 0;
            }
            public void PurchaseUnlockable(string input, Terminal terminal, ref TerminalNode node)
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
                    ErrorResponse("No purchasable item or unlockable goes by the name: '" + input + "'", node);
                    return;
                }
                if (CheckNotEnoughChars(input.Length, purchasableUnlockable.node.creatureName.Length, node))
                {
                    return;
                }

                this.purchasableUnlockable = purchasableUnlockable;

                if (ShowCommandConfirmationsValue)
                {
                    currentCommand = this;
                    node.displayText = "Would you like to purchase a " + purchasableUnlockable.node.creatureName + " for $" + GetUnlockableCost(purchasableUnlockable.node.creatureName.ToLower()).ToString() + "?\nType CONFIRM to confirm your purchase.";
                    return;
                }

                TryPurchaseUnlockable(terminal, ref node);
                purchasableUnlockable = null;
            }

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    ErrorResponse("Please enter an item to purchase.", node);
                    return;
                }
                if (terminal.useCreditsCooldown)
                {
                    ErrorResponse("You're on a credit usage cooldown.", node);
                    return;
                }

                input = input.ToLower();
                ParseInput(input, out string itemInput, out int amount);

                PurchaseItem(input, itemInput, amount, terminal, ref node);
            }

            public void TryPurchaseItem(Terminal terminal, ref TerminalNode node)
            {
                int cost = GetItemCost(purchasableItem.item.itemName.ToLower(), true);
                if (terminal.groupCredits < cost * amount)
                {
                    ErrorResponse("You do not have enough credits to purchase that item.", node);
                    return;
                }
                if (amount + terminal.numberOfItemsInDropship > SyncedConfig.SyncedConfig.Instance.MaxDropshipItemsValue)
                {
                    ErrorResponse("There is not enough space on the dropship for these items, there are currently " + terminal.numberOfItemsInDropship.ToString() + "/" + SyncedConfig.SyncedConfig.Instance.MaxDropshipItemsValue.ToString() + " items en route.", node);
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

                node.displayText = "Purchased " + purchasableItem.item.itemName + " x" + amount.ToString() + " for $" + (cost * amount).ToString() + ".";
                node.playSyncedClip = terminalSyncedSounds["buy"];
            }
            public void TryPurchaseUnlockable(Terminal terminal, ref TerminalNode node)
            {
                if (!purchasableUnlockable.unlockable.alwaysInStock && !terminal.ShipDecorSelection.Contains(purchasableUnlockable.unlockable.shopSelectionNode))
                {
                    ErrorResponse("This unlockable is not for sale.", node);
                    return;
                }
                if (purchasableUnlockable.unlockable.hasBeenUnlockedByPlayer || purchasableUnlockable.unlockable.alreadyUnlocked)
                {
                    ErrorResponse("You already have this unlockable.", node);
                    return;
                }

                int cost = GetUnlockableCost(purchasableUnlockable.node.creatureName.ToLower());
                if (terminal.groupCredits < cost)
                {
                    ErrorResponse("You do not have enough credits to purchase that unlockable.", node);
                    return;
                }
                if ((!StartOfRound.Instance.inShipPhase && !StartOfRound.Instance.shipHasLanded) || StartOfRound.Instance.shipAnimator.GetCurrentAnimatorStateInfo(0).tagHash != Animator.StringToHash("ShipIdle"))
                {
                    ErrorResponse("You cannot purchase that unlockable currently.", node);
                    return;
                }

                HUDManager.Instance.DisplayTip("Tip", "Press B to move and place objects in the ship, E to cancel.", false, true, "LC_MoveObjectsTip");
                if (terminal.IsHost)
                {
                    StartOfRound.Instance.BuyShipUnlockableServerRpc(purchasableUnlockable.node.shipUnlockableID, terminal.groupCredits - cost);
                }
                else
                {
                    terminal.groupCredits = Mathf.Clamp(terminal.groupCredits - cost, 0, 10000000);
                    StartOfRound.Instance.BuyShipUnlockableServerRpc(purchasableUnlockable.node.shipUnlockableID, terminal.groupCredits);
                }

                node.displayText = "Purchased " + purchasableUnlockable.node.creatureName + " for $" + cost.ToString() + ".";
                node.playSyncedClip = terminalSyncedSounds["buy"];
            }

            public override void Confirmed(Terminal terminal, ref TerminalNode node)
            {
                node.clearPreviousText = false;
                if (purchasableItem != null)
                {
                    TryPurchaseItem(terminal, ref node);
                    purchasableItem = null;
                    amount = 0;
                }
                else if (purchasableUnlockable != null)
                {
                    TryPurchaseUnlockable(terminal, ref node);
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                node.displayText = "Use the RETRIEVE command to take unlockables out of storage. Stored Unlockables:\n";
                int length = 0;
                string finalText = "";
                bool hasStored = false;
                foreach (PurchasableUnlockable unlockable in purchasableUnlockables.Values)
                {
                    if (unlockable.unlockable.inStorage)
                    {
                        hasStored = true;

                        string text = "";
                        if (ShowMinimumCharsValue)
                        {
                            text = "(" + unlockable.shortestChars + ") ";
                        }
                        text += unlockable.node.creatureName;

                        if (text.Length > length)
                        {
                            length = text.Length;
                        }
                        finalText += "\n * " + text;
                    }
                }
                if (!hasStored)
                {
                    node.displayText += "+----------------------------------+\nThere are no unlockables in storage.";
                    return;
                }
                node.displayText += "+" + new string('-', length + 2) + "+" + finalText;
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    ErrorResponse("Please enter an unlockable to take out of storage.", node);
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
                    ErrorResponse("No unlockable goes by the name: '" + input + "'", node);
                    return;
                }
                if (CheckNotEnoughChars(input.Length, purchasableUnlockable.node.creatureName.Length, node))
                {
                    return;
                }

                if (!purchasableUnlockable.unlockable.inStorage)
                {
                    ErrorResponse("That unlockable is not in storage.", node);
                    return;
                }
                if (!purchasableUnlockable.unlockable.hasBeenUnlockedByPlayer && !purchasableUnlockable.unlockable.alreadyUnlocked)
                {
                    ErrorResponse("You do not own that unlockable.", node);
                    return;
                }

                StartOfRound.Instance.ReturnUnlockableFromStorageServerRpc(purchasableUnlockable.node.shipUnlockableID);
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
                args = "[Entity]";
            }

            public string GenerateBestiaryPage(Terminal terminal)
            {
                string result = "Welcome to the Bestiary, use the BESTIARY command followed by one of the following names to view information about that entity.\n";
                if (terminal.scannedEnemyIDs == null || terminal.scannedEnemyIDs.Count <= 0)
                {
                    return result + "+----------------------------+\nNo data collected on wildlife.";
                }
                int length = 0;
                string finalText = "";
                foreach (Entity entity in entities.Values)
                {
                    TerminalNode entityNode = entity.entry;
                    if (entityNode == null || !terminal.scannedEnemyIDs.Contains(entityNode.creatureFileID))
                    {
                        continue;
                    }

                    string text = "";
                    if (terminal.newlyScannedEnemyIDs.Contains(entityNode.creatureFileID))
                    {
                        text  = "(!) ";
                    }
                    if (ShowMinimumCharsValue)
                    {
                        text += "(" + entity.shortestChars + ") ";
                    }
                    text += entity.name;
                    if (text.Length > length)
                    {
                        length = text.Length;
                    }
                    finalText += "\n * " + text;
                }
                return result + "+" + new string('-', length + 2) + "+" + finalText;
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    node.displayText = GenerateBestiaryPage(terminal);
                    return;
                }

                input = input.ToLower();
                EnemyType entityType = null;
                TerminalNode entityNode = null;
                string resultName = null;
                foreach (Entity entity in entities.Values)
                {
                    if (entity.entry != null && entity.name.ToLower().StartsWith(input) && terminal.scannedEnemyIDs.Contains(entity.entry.creatureFileID))
                    {
                        entityType = entity.type;
                        entityNode = entity.entry;
                        resultName = entity.name;
                        break;
                    }
                }
                if (entityType == null)
                {
                    ErrorResponse("No entity exists with the name: '" + input + "'", node);
                    return;
                }
                if (CheckNotEnoughChars(input.Length, resultName.Length, node))
                {
                    return;
                }

                terminal.newlyScannedEnemyIDs.Remove(entityNode.creatureFileID);
                node = entityNode;
                node.displayText = node.displayText.TrimEnd('\n') + GenerateEntryPage(entityType);
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

                int length = 0;
                string finalText = "";
                foreach (Log log in logs.Values)
                {
                    if (!terminal.unlockedStoryLogs.Contains(log.log.storyLogFileID))
                    {
                        continue;
                    }
                    string text = "";
                    if (terminal.newlyUnlockedStoryLogs.Contains(log.log.storyLogFileID))
                    {
                        text = "(!) ";
                    }
                    if (ShowMinimumCharsValue)
                    {
                        text += "(" + log.shortestChars + ") ";
                    }
                    text += log.log.creatureName;

                    if (text.Length > length)
                    {
                        length = text.Length;
                    }
                    finalText += "\n * " + text;
                }
                return "Sigurd's Log Entries.\nRead an entry by entering SIGURD followed by the name of the entry you wish to read.\n+" + new string('-', length + 2) + "+" + finalText;
            }

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    node.displayText = GenerateSigurdPage(terminal);
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
                    ErrorResponse("No log exists with the name: '" + input + "'", node);
                    return;
                }
                if (CheckNotEnoughChars(input.Length, log.log.creatureName.Length, node))
                {
                    return;
                }

                terminal.newlyUnlockedStoryLogs.Remove(log.log.storyLogFileID);
                node = log.log;
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                node.displayTexture = StartOfRound.Instance.mapScreen.cam.targetTexture;
                node.persistentImage = true;
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                node.displayText = "These are radar targets you can switch to using the SWITCH command, or by using the left and right arrow keys with the monitor on screen after using the MONITOR command. Radar boosters can be flashed and pinged by using the FLASH and PING commands.\n";
                int length = 0;
                string finalText = "";
                foreach (TransformAndName target in StartOfRound.Instance.mapScreen.radarTargets)
                {
                    string text = "";
                    PlayerControllerB component = target.transform.gameObject.GetComponent<PlayerControllerB>();
                    if (component != null && !component.isPlayerControlled && !component.isPlayerDead && component.redirectToEnemy == null)
                    {
                        continue;
                    }
                    text = target.name;

                    if (text.Length > length)
                    {
                        length = text.Length;
                    }

                    finalText += "\n * " + text;
                }

                node.displayText += "+" + new string('-', length + 2) + "+" + finalText;
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
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
                    ErrorResponse("No radar target goes by the name: '" + input + "'", node);
                    return;
                }
                if (CheckNotEnoughChars(input.Length, StartOfRound.Instance.mapScreen.radarTargets[target].name.Length, node))
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
                args = "[Radar booster]";
            }

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    if (!StartOfRound.Instance.mapScreen.radarTargets[StartOfRound.Instance.mapScreen.targetTransformIndex].isNonPlayer)
                    {
                        ErrorResponse("You can only ping radar boosters.", node);
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
                    ErrorResponse("No radar booster goes by the name: '" + input + "'", node);
                    return;
                }
                if (CheckNotEnoughChars(input.Length, StartOfRound.Instance.mapScreen.radarTargets[target].name.Length, node))
                {
                    return;
                }

                if (!StartOfRound.Instance.mapScreen.radarTargets[target].isNonPlayer)
                {
                    ErrorResponse("You can only ping radar boosters.", node);
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
                args = "[Radar booster]";
            }

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    if (!StartOfRound.Instance.mapScreen.radarTargets[StartOfRound.Instance.mapScreen.targetTransformIndex].isNonPlayer)
                    {
                        ErrorResponse("You can only flash radar boosters.", node);
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
                    ErrorResponse("No radar booster goes by the name: '" + input + "'", node);
                    return;
                }
                if (CheckNotEnoughChars(input.Length, StartOfRound.Instance.mapScreen.radarTargets[target].name.Length, node))
                {
                    return;
                }

                if (!StartOfRound.Instance.mapScreen.radarTargets[target].isNonPlayer)
                {
                    ErrorResponse("You can only flash radar boosters.", node);
                    return;
                }
                StartOfRound.Instance.mapScreen.FlashRadarBooster(target);
            }
        }

        public class CodesCommand : Command
        {
            public CodesCommand()
            {
                category = "Alphanumeric Codes";
                id = "codes";
                description = "View all of the alphanumeric codes and what objects they correspond to within the building.";
                AddShortcut("co", id);
            }

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                node.displayText = "This is the full list of alphanumeric codes on the current moon and what objects they're bound to.\nYou can enter one of the following alphanumeric codes to interact with the object they're bound to:\n";
                int length = 0;
                string result = "";
                foreach (TerminalAccessibleObject tao in UnityEngine.Object.FindObjectsByType<TerminalAccessibleObject>(FindObjectsSortMode.None))
                {
                    result += "\n * " + tao.objectCode + ": ";
                    if (tao.isBigDoor)
                    {
                        result += "Door";
                        if (4 > length)
                        {
                            length = 4;
                        }
                        continue;
                    }
                    if (tao.codeAccessCooldownTimer == 3.2f)
                    {
                        result += "Landmine";
                        if (8 > length)
                        {
                            length = 8;
                        }
                        continue;
                    }
                    result += "Turret";
                    if (6 > length)
                    {
                        length = 6;
                    }
                }

                if (result == "")
                {
                    node.displayText += "+------------------------------+\nThere are no alphanumeric codes.";
                    return;
                }
                node.displayText += "+" + new string('-', length + 6) + "+" + result;
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                if (ShowCommandConfirmationsValue)
                {
                    currentCommand = this;
                    node.displayText = "Would you like to pull the lever to land on " + StartOfRound.Instance.currentLevel.PlanetName + "?\nType CONFIRM to confirm landing.";
                    return;
                }

                Land(terminal, ref node);
            }

            public void Land(Terminal terminal, ref TerminalNode node)
            {
                if (StartOfRound.Instance.travellingToNewLevel)
                {
                    ErrorResponse("The ship is currently routing to another moon.", node);
                    return;
                }
                if (StartOfRound.Instance.shipHasLanded)
                {
                    ErrorResponse("The ship is already on the moon.", node);
                    return;
                }
                if (!GameNetworkManager.Instance.gameHasStarted && !GameNetworkManager.Instance.isHostingGame)
                {
                    ErrorResponse("The host must be the one to land the ship at the start of the game.", node);
                    return;
                }

                StartMatchLever lever = UnityEngine.Object.FindAnyObjectByType<StartMatchLever>();
                if (lever.leverHasBeenPulled)
                {
                    ErrorResponse("The lever has already been pulled.", node);
                    return;
                }
                if (!lever.triggerScript.interactable)
                {
                    ErrorResponse("The lever cannot currently be pulled.", node);
                    return;
                }

                lever.LeverAnimation();
                lever.PullLever();
                node.displayText = "Pulling the lever to land on " + StartOfRound.Instance.currentLevel.PlanetName + "...";
            }

            public override void Confirmed(Terminal terminal, ref TerminalNode node)
            {
                node.clearPreviousText = false;

                Land(terminal, ref node);
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                if (ShowCommandConfirmationsValue)
                {
                    currentCommand = this;
                    node.displayText = "Would you like to pull the lever to launch into orbit?\nType CONFIRM to confirm landing.";
                    return;
                }

                Launch(terminal, ref node);
            }

            public void Launch(Terminal terminal, ref TerminalNode node)
            {
                if (StartOfRound.Instance.travellingToNewLevel)
                {
                    ErrorResponse("The ship is currently routing to another moon.", node);
                    return;
                }
                if (!StartOfRound.Instance.shipHasLanded)
                {
                    ErrorResponse("The ship is either still landing, already in orbit, or currently leaving the moon.", node);
                    return;
                }
                if (!GameNetworkManager.Instance.gameHasStarted && !GameNetworkManager.Instance.isHostingGame)
                {
                    ErrorResponse("The host must be the one to land the ship at the start of the game.", node);
                    return;
                }

                StartMatchLever lever = UnityEngine.Object.FindAnyObjectByType<StartMatchLever>();
                if (!lever.leverHasBeenPulled)
                {
                    ErrorResponse("The lever has already been pulled.", node);
                    return;
                }
                if (!lever.triggerScript.interactable)
                {
                    ErrorResponse("The lever cannot currently be pulled.", node);
                    return;
                }

                lever.LeverAnimation();
                lever.PullLever();
                node.displayText = "Pulling the lever to launch into orbit...";
            }

            public override void Confirmed(Terminal terminal, ref TerminalNode node)
            {
                node.clearPreviousText = false;

                Launch(terminal, ref node);
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
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
                                return;
                            }
                            ErrorResponse("The teleporter is on cooldown.", node);
                            return;
                        }
                        ErrorResponse("The teleporter is in storage.", node);
                        return;
                    }
                }
                ErrorResponse("You do not own a teleporter.", node);
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
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
                                return;
                            }
                            ErrorResponse("The inverse teleporter is on cooldown.", node);
                            return;
                        }
                        ErrorResponse("The inverse teleporter is in storage.", node);
                        return;
                    }
                }
                ErrorResponse("You do not own an inverse teleporter.", node);
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                int shipCount = 0, shipValue = 0, indoorCount = 0, indoorValue = 0, outdoorCount = 0, outdoorValue = 0;
                string outdoorItems = "", indoorItems = "";

                int nameLength = 0;
                int valueLength = 0;
                GrabbableObject[] objects = UnityEngine.Object.FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None);
                foreach (GrabbableObject item in objects)
                {
                    if (item.itemProperties.isScrap && !item.isInShipRoom)
                    {
                        if (item.itemProperties.itemName.Length + 1 > nameLength)
                        {
                            nameLength = item.itemProperties.itemName.Length + 1;
                        }
                        if (item.scrapValue.ToString().Length > valueLength)
                        {
                            valueLength = item.scrapValue.ToString().Length;
                        }
                    }
                }

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
                        indoorItems += "\n * " + (item.itemProperties.itemName + " ").PadRight(nameLength, ListPaddingCharValue) + " $" + item.scrapValue.ToString().PadLeft(valueLength);
                        continue;
                    }

                    outdoorCount++;
                    outdoorValue += item.scrapValue;
                    outdoorItems += "\n * " + (item.itemProperties.itemName + " ").PadRight(nameLength, ListPaddingCharValue) + " $" + item.scrapValue.ToString().PadLeft(valueLength);
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
                args = "(9 characters)";
            }

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                SignalTranslator signalTranslator = UnityEngine.Object.FindAnyObjectByType<SignalTranslator>();
                if (signalTranslator == null)
                {
                    ErrorResponse("You do not own a signal translator.", node);
                    return;
                }
                if (UnityEngine.Time.realtimeSinceStartup - signalTranslator.timeLastUsingSignalTranslator <= 8f)
                {
                    ErrorResponse("The signal translator is still in use.", node);
                    return;
                }

                string text = input.Substring(0, Mathf.Min(input.Length, 9));
                if (string.IsNullOrEmpty(text))
                {
                    ErrorResponse("Please enter a 9 character or less message to send.", node);
                    return;
                }

                node.displayText = "Transmitting message...";
                if (!terminal.IsServer)
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                node.displayText = ">";
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                lammOS.Load();

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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                if (StartOfRound.Instance.isChallengeFile)
                {
                    ErrorResponse("You are unable to be ejected on challenge moons.", node);
                    return;
                }
                if (!StartOfRound.Instance.inShipPhase)
                {
                    ErrorResponse("You must be in orbit to be ejected.", node);
                    return;
                }
                if (StartOfRound.Instance.firingPlayersCutsceneRunning)
                {
                    ErrorResponse("Your crew is already being ejected.", node);
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

            public override void Confirmed(Terminal terminal, ref TerminalNode node)
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                node.displayText = "Here are all of your personal macros.\nThey can be inspected using the MACRO command, and ran using the RUN-MACRO command.\n";
                int length = 0;
                string result = "";
                foreach (string macroId in GetMacroIds())
                {
                    result += "\n * " + macroId;
                    if (macroId.Length > length)
                    {
                        length = macroId.Length;
                    }
                }
                if (result == "")
                {
                    node.displayText += "+-----------------------------------------------------------+\nYou have no macros, create one with the CREATE-MACRO command.";
                    return;
                }
                node.displayText += "+" + new string('-', length + 2) + "+" + result;
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    ErrorResponse("You must enter a macro id.", node);
                    return;
                }

                input = input.ToLower();
                if (!HasMacro(input))
                {
                    ErrorResponse("No macro exists with that id.", node);
                    return;
                }

                node.displayText = "Macro: " + input + "\nInstructions:";
                foreach (string instruction in GetMacro(input))
                {
                    node.displayText += "\n * " + instruction;
                }
            }
        }
        public class RunMacroCommand : Command
        {
            public RunMacroCommand()
            {
                category = "Macros";
                id = "run-macro";
                description = "Run one of your macros.";
                AddShortcut("-", id);
                args = "<Macro>";
            }

            public IEnumerator RunMacro(string macroId)
            {
                yield return new WaitForEndOfFrame();
                macroAppendingText = false;

                string output = "Executing the macro with the id '" + macroId + "':\n\n>";

                foreach (string instruction in GetMacro(macroId))
                {
                    TerminalNode dummyNode = ScriptableObject.CreateInstance<TerminalNode>();
                    dummyNode.displayText = "";
                    dummyNode.clearPreviousText = true;
                    dummyNode.terminalEvent = "";

                    output += instruction;

                    if (instruction.StartsWith("wait"))
                    {
                        float time = 1f;
                        int split = instruction.IndexOf(' ');
                        if (split != -1)
                        {
                            float.TryParse(instruction.Substring(split + 1), out time);
                        }

                        output += "\n\n>";
                        dummyNode.displayText = output;
                        macroAppendingText = true;
                        Variables.Variables.Terminal.LoadNewNode(dummyNode);
                        macroAppendingText = false;
                        yield return new WaitForSeconds(time);
                    }
                    else
                    {
                        HandleCommand(Variables.Variables.Terminal, instruction, ref dummyNode);
                        dummyNode.clearPreviousText = true;
                        HandleCommandResult(Variables.Variables.Terminal, dummyNode);

                        output += "\n\n" + dummyNode.displayText;
                        dummyNode.displayText = output;
                        macroAppendingText = true;
                        Variables.Variables.Terminal.LoadNewNode(dummyNode);
                        macroAppendingText = false;
                        yield return new WaitForSeconds(1f / GetMacroInstructionsPerSecond());
                    }
                }

                runningMacroCoroutine = null;
            }

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                if (runningMacroCoroutine != null)
                {
                    ErrorResponse("You cannot run macros within macros.", node);
                    return;
                }

                if (input == "")
                {
                    ErrorResponse("You must enter a macro id.", node);
                    return;
                }

                input = input.ToLower();
                if (!HasMacro(input))
                {
                    ErrorResponse("No macro exists with that id.", node);
                    return;
                }

                runningMacroCoroutine = StartOfRound.Instance.StartCoroutine(RunMacro(input));
                macroAppendingText = true;
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    ErrorResponse("You must enter a macro id.", node);
                    return;
                }

                int offset = input.IndexOf(' ');
                if (offset == -1)
                {
                    ErrorResponse("You must enter intructions for the macro to run, each one separated by ';'.", node);
                    return;
                }

                string macroId = input.Substring(0, offset).ToLower();
                if (HasMacro(macroId))
                {
                    ErrorResponse("A macro already exists with that id.", node);
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
                    ErrorResponse("You must enter intructions for the macro to run, each one separated by ';'.", node);
                    return;
                }

                AddMacro(macroId, finalInstructions);
                Save();

                node.displayText = "Created a new macro with the id '" + macroId + "' with the following instructions:";
                foreach (string instruction in finalInstructions)
                {
                    node.displayText += "\n * " + instruction;
                }
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    ErrorResponse("You must enter a macro id.", node);
                    return;
                }

                int offset = input.IndexOf(' ');
                if (offset == -1)
                {
                    ErrorResponse("You must enter intructions for the macro to run, each one separated by ';'.", node);
                    return;
                }

                string macroId = input.Substring(0, offset).ToLower();
                if (!HasMacro(macroId))
                {
                    ErrorResponse("No macro exists with that id.", node);
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
                    ErrorResponse("You must enter intructions for the macro to run, each one separated by ';'.", node);
                    return;
                }

                ModifyMacro(macroId, finalInstructions);
                Save();

                node.displayText = "Edited the macro with the id '" + macroId + "' giving it the new instructions:";
                foreach (string instruction in finalInstructions)
                {
                    node.displayText += "\n * " + instruction;
                }
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

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                if (input == "")
                {
                    ErrorResponse("You must enter a macro id.", node);
                    return;
                }

                input = input.ToLower();
                if (!HasMacro(input))
                {
                    ErrorResponse("No macro exists with that id.", node);
                    return;
                }

                RemoveMacro(input);
                Save();

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

            public void LogShortestChars()
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

            public void LogKeyword(string args, Terminal terminal)
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
                lammOS.Logger.LogInfo(text);
            }
            public void LogNode(string args, Terminal terminal)
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
                lammOS.Logger.LogInfo(text);
            }
            public void LogSpecialNode(string args, Terminal terminal)
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
                lammOS.Logger.LogInfo(text);
            }

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                input = input.ToLower();
                int split = input.IndexOf(' ');
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
                            if (!terminal.IsServer)
                            {
                                lammOS.Logger.LogInfo("You must be the host of the lobby to use that debug command.");
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
                            LogKeyword(args, terminal);
                            break;
                        }
                    case "nodes":
                        {
                            lammOS.Logger.LogInfo("Nodes: " + terminal.terminalNodes.terminalNodes.Count.ToString());
                            break;
                        }
                    case "specialnodes":
                        {
                            lammOS.Logger.LogInfo("Special Nodes: " + terminal.terminalNodes.specialNodes.Count.ToString());
                            break;
                        }
                    case "node":
                        {
                            LogNode(args, terminal);
                            break;
                        }
                    case "specialnode":
                        {
                            LogSpecialNode(args, terminal);
                            break;
                        }
                }
            }
        }

        public class CodeCommand : Command
        {
            public CodeCommand(string alphanumericCode)
            {
                category = "Alphanumeric Codes";
                id = alphanumericCode;
                description = "An alphanumeric code command.";
                args = GetCommand("code").args;
                hidden = true;
            }

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
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
                    if (action != "toggle" && CheckNotEnoughChars(input.Length, action.Length, node))
                    {
                        return;
                    }
                }
                if (!GetCommand("code-" + action).enabled)
                {
                    ErrorResponse("That type of action is not enabled by the host.", node);
                    return;
                }

                TerminalAccessibleObject[] objects = UnityEngine.Object.FindObjectsByType<TerminalAccessibleObject>(FindObjectsSortMode.None);
                switch (action)
                {
                    case "default":
                        {
                            foreach (TerminalAccessibleObject obj in objects)
                            {
                                if (obj.objectCode == id)
                                {
                                    obj.CallFunctionFromTerminal();
                                }
                            }
                            break;
                        }
                    case "toggle":
                        {
                            foreach (TerminalAccessibleObject obj in objects)
                            {
                                if (obj.objectCode == id && obj.isBigDoor)
                                {
                                    obj.CallFunctionFromTerminal();
                                }
                            }
                            break;
                        }
                    case "deactivate":
                        {
                            foreach (TerminalAccessibleObject obj in objects)
                            {
                                if (obj.objectCode == id && !obj.isBigDoor)
                                {
                                    obj.CallFunctionFromTerminal();
                                }
                            }
                            break;
                        }
                    case "activate":
                        {
                            Landmine[] landmines = UnityEngine.Object.FindObjectsByType<Landmine>(FindObjectsSortMode.None);
                            Turret[] turrets = UnityEngine.Object.FindObjectsByType<Turret>(FindObjectsSortMode.None);
                            foreach (TerminalAccessibleObject obj in objects)
                            {
                                if (obj.objectCode == id && !obj.isBigDoor)
                                {
                                    foreach (Landmine landmine in landmines)
                                    {
                                        if (landmine.NetworkObjectId == obj.NetworkObjectId)
                                        {
                                            ((IHittable)landmine).Hit(0, Vector3.zero);
                                        }
                                    }

                                    foreach (Turret turret in turrets)
                                    {
                                        if (turret.NetworkObjectId == obj.NetworkObjectId && turret.turretMode != TurretMode.Berserk)
                                        {
                                            ((IHittable)turret).Hit(0, Vector3.zero);
                                        }
                                    }
                                }
                            }
                            break;
                        }
                }

                terminal.codeBroadcastAnimator.SetTrigger("display");
                terminal.terminalAudio.PlayOneShot(terminal.codeBroadcastSFX, 1f);
            }
        }
        public class DummyCommand : Command
        {
            public DummyCommand(string category, string id, string description, string args = "", bool hidden = false, bool enabled = true)
            {
                this.category = category;
                this.id = id;
                this.description = description;
                this.args = args;
                this.hidden = hidden;
                defaultEnabled = enabled;
            }

            public override void Execute(Terminal terminal, string input, ref TerminalNode node)
            {
                node.displayText = "This command doen't do anything, and is only here to give helpful information. Enter 'HELP " + id.ToUpper() + "' to learn why this command is here.\n\n>";
            }
        }

        public abstract class ConfirmationCommand : Command
        {
            public abstract void Confirmed(Terminal terminal, ref TerminalNode node);

            internal static void Handle(Terminal terminal, string input, ref TerminalNode node)
            {
                if ("confirm".StartsWith(input))
                {
                    if (CheckNotEnoughChars(input.Length, 7, node))
                    {
                        return;
                    }
                    (currentCommand as ConfirmationCommand).Confirmed(terminal, ref node);
                    return;
                }
                node.displayText = ">";
            }
        }
        public abstract class Command
        {
            public string category = "Uncategorized";
            public string id { get; internal set; } = "";
            public string description = "";
            public string args = "";
            public bool hidden = false;
            internal bool defaultEnabled = true;
            public bool enabled
            {
                get
                {
                    return SyncedConfig.SyncedConfig.Instance.EnabledCommands.ContainsKey(id) ? SyncedConfig.SyncedConfig.Instance.EnabledCommands[id] : defaultEnabled;
                }
            }

            public static void ErrorResponse(string response, TerminalNode node)
            {
                node.displayText = response;
                node.playSyncedClip = terminalSyncedSounds["error"];
            }
            public static bool CheckNotEnoughChars(int inputLength, int resultLength, TerminalNode node)
            {
                if (inputLength < Mathf.Min(CharsToAutocompleteValue, resultLength))
                {
                    ErrorResponse("Not enough characters were input to autocomplete a result. The current requirement is " + CharsToAutocompleteValue.ToString() + " characters.", node);
                    return true;
                }
                return false;
            }

            public abstract void Execute(Terminal terminal, string input, ref TerminalNode node);

            internal static void Handle(string commandId, Terminal terminal, string input, ref TerminalNode node)
            {
                if (IsShortcut(commandId))
                {
                    commandId = GetCommandIdByShortcut(commandId);
                    if (runningMacroCoroutine == null)
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
                if (!command.enabled)
                {
                    ErrorResponse("That command is disabled by the host.\n\n>", node);
                    return;
                }

                try
                {
                    command.Execute(terminal, input, ref node);
                }
                catch (Exception e)
                {
                    node = ScriptableObject.CreateInstance<TerminalNode>();
                    node.displayText = "An error occurred executing the command: '" + commandId + "'\n\n>";
                    node.clearPreviousText = true;
                    node.terminalEvent = "";
                    node.playSyncedClip = terminalSyncedSounds["error"];
                    lammOS.Logger.LogError("An error occurred executing the command: '" + commandId + "'\n" + e.ToString());
                }
            }
        }
    }
}