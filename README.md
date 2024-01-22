# What is lammOS?
lammOS is a mod that reworks how the terminal commands work, changing how many of them function, and adding several new ones.

## What's Reworked
The way commands are parsed and processed by the terminal have been reworked, making using and adding new content to the terminal a smoother experience.
- This has the negative side effect of command results not being 100% similar or synced with the vanilla game or other mods, especially if the game updates or mods change or add content making any drastic changes.
- This will also cause commands added or changed by other mods that use nodes and keywords, what the game normally uses, to *potentially* be unstable with the terminal, though some will still work with minimal issues. It is possible for other mods to add their own lammOS compatible commands with an optional or required dependency if they so choose though, minimizing all potential issues.

More specifically, for modders that are curious, what I did is pre-patch the Terminal.ParsePlayerSentence method to return my own TerminalNode that does basically nothing unless I need it to, such as to use it's displayText property. This does not run any of the code in the actual Terminal.ParsePlayerSentence method, instead I parse the text myself and run the corresponding command's Execute method to give it it's functionality, then it goes on to call the Terminal.LoadNewNode method as normal to update the terminal's on screen text.
I also pre-patched the Terminal.TextPostProcess method to do nothing by default, as it's functionality is not needed with this mod. This is toggleable within the config for compatibility with other mods however.

## What's Changed
- The screens that are shown upon opening the terminal have been changed to show that the terminal is powered by lammOS.
- All responses from commands will be formatted to end with a `>` with an empty line between the previous text and the `>`, to ensure command responses have some level of consistency. 
- You now don't need to enter at least three characters for the terminal to understand what you were trying to buy or view, you only need to type the first character, or however many you set for the CharsToAutocomplete config option, but keep in mind it will pick the first thing it finds that matches the first couple character(s) you enter and assume that's what you wanted.
  - Setting the CharsToAutocomplete config option to a value above the character length of certain options will require you to type the full name of the option.
- The `>HELP` command now lists all of the available commands and the arguments they take in, and giving it the name of another command as a parameter will give you more information about it.
- Most commands now have shortcuts, for example:
  - `>MOON` -> `>M`
  - `>PING` -> `>P`
  - `>TRANSMIT` -> `>T`
- Commands like `>MOON` and `>BESTIARY` will show even more details about the moon or entity you are curious about.
- The `>STORE` command will hide upgrades and other decorative items that have already been purchased.
- Using commands like `>ROUTE` and `>BUY` will not ask for confirmation if you'd like to go to another moon or if you'd like to purchase something.
  - To make this less problematic, you can either type most of an items name to be sure what you're typing in, or use the ShowMinimumChars config option to see the minimum characters you need to enter for the terminal to autocomplete any of the options in commands like `>MOONS` and `>STORE`.
- The `>SCAN` command now shows three categories for scanned items: in the ship, inside the building, and outside, as well as what items are at these locations and their values.
  - As of v49, joining lobbies will show any scrap in the ship as being inside the building until they are picked up and dropped, this does not occur if you are the host.
- The `>VIEW MONITOR` command has been simplified to `>MONITOR`, or simply `>V` with the command's shortcut, and the monitor now shows up instantly rather than slowly loading after entering the command.
- The `>SWITCH` command will switch to the next radar target if entered with no parameters, as if you pushed the white button on the monitor, and the `>PING` and `>FLASH` commands will now act on the radar booster currently being viewed if entered with no parameters.
- Along with many other small changes to vanilla commands, such as how they work to make them easier to use, and how their responses will look to clean some of the unnecessary text.

## What's Added
- The `>TARGETS` command, which lists all of the radar targets you can view on the monitor or terminal with `>MONITOR`.
- A clear command (`>CLR` or `>C`) that will clear all text from the terminal, useful if you can't see the monitor after using the `>MONITOR` command.
- The `>CODES` command, this lists all of the alphanumeric codes you can enter and what they are associated with (doors, turrets, or landmines) on the moon you are currently on.
- The `>DOOR` and `>LIGHTS` commands to toggle the ship's door open/closed and lights on/off.
- The `>TP` and `>ITP` commands to remotely activate the teleporter and inverse teleporter.
- The `>SHORTCUTS` command, which will list all of the shortcuts associated with every command.
- The `>RELOAD` command, to reload the config and fix any inconsistencies experienced with the terminal. There shouldn't be any inconsistencies unless you have mods that change certain properties while you're in game, if these changes are made beforehand, then there should be no problems.
  - A list of properties that may need reloading if altered by mods, though this list is not limited to these properties, potentially includes:
	- Purchaseable items and unlockables
    - Entity invulnerability
	- Entity power and the max that can exist
	- Ect.
- An clock in the top right corner of the terminal, optional with a config option.
- A synced config to allow you to change the maximum amount of items on the delivery dropship, as well as change the prices for every moon, item, and unlockable.

## What's Planned
- Sorting commands into categories, mainly for how they show up when using the `>HELP` command, either based on function or by mod that adds them.
- The ability to create saveable macros and run them, which would execute a series of commands you tell it to.
- Adding back purchase and routing confirmation screens as a config option.
- Fixing the possibility of any desyncing or inconsistencies occuring caused by other mods, so that the `>RELOAD` command doesn't need to be used.
- Showing the entity's base health when viewing one using the `>BESTIARY` command.
- Implementing command history using the up and down arrow keys. Like [Terminal_History by NotAtomicBomb](https://thunderstore.io/c/lethal-company/p/NotAtomicBomb/Terminal_History/)
- Switching between radar targets using the left and right arrow keys. Like [FastSwitchPlayerViewInRadar by kRYstall9](https://thunderstore.io/c/lethal-company/p/kRYstall9/FastSwitchPlayerViewInRadar/)
- Being able to use a walkie talkie while on the terminal. Like [TermSpeak by KodiCraft](https://thunderstore.io/c/lethal-company/p/KodiCraft/TermSpeak/)

## Known Incompatibilities
- [Advanced Company by PotatoePet](https://thunderstore.io/c/lethal-company/p/PotatoePet/AdvancedCompany/)
  - Price changes don't sync, this has to do with how Advanced Company saves these changes, not making it possible for me to access them.
    - The creator of the mod has plans to make an api that will allow me and other mod creators to get and modify these prices.

## Contact
- @lammas123 on Discord - [Lethal Company Modding Discord](https://discord.com/invite/lcmod) - [lammOS Thread](https://discord.com/channels/1168655651455639582/1196941743673847938)

# Changelog
## 1.1.2
- Fixed an incompatibility with [Lategame Upgrades by malco](https://thunderstore.io/c/lethal-company/p/malco/Lategame_Upgrades/) and other mods that modified the help node's results, making lammOS' replacement of it more mod friendly, as it was causing Lategame Upgrades to throw errors.
  - This incompatibility in particular caused quite a few problems for lammOS, as Lategame Upgrades throwing this error would cause a slight desync from the Terminal for lammOS. This lead to the case where routing to a paid moon would attempt to use the Terminal from the first lobby the host created, but never any lobby after that until they restarted their game.
- Made adding entities to the list of entities viewable with the `>BESTIARY` command more broad, as it would add the Rolling Giant from the [RollingGiant mod by NomnomAB](https://thunderstore.io/c/lethal-company/p/NomnomAB/RollingGiant/). 

## 1.1.1
- Fixed a bug that would occur when buying more than 12 items at a time as a client where your purchase wouldn't register but the group credits would be deducted only for you. You would then no longer be able to purchase anything for being on a permanent group credit usage cooldown until someone else did, which would then also fix your desynced group credits.
- Fixed routing to other moons or buying unlockables taking your group credits if purchased as the host. Doing so would either take double the group credits taken if you had the funds, or take only the normal amount but not route you to the moon or unlock the unlockable.

## 1.1.0
- Fixed a bug with loading all entities into the entity dictionary the bestiary would use when any mod added additional entities.
- Fixed an incompatibility with [Lethal Things by Evasia](https://thunderstore.io/c/lethal-company/p/Evaisa/LethalThings/) where some of their decor items would cause an error to be thrown when entering the `>STORE` command.
- Made lots of changes to the publicity of methods and properties in the mod.
- Removed the DisableNewTerminal config option, not sure why I added it initially as it just adds more complexity.
- Made lots of little changes to several commands to make them more consistent with each other.
- Added a new Synced config, along with the new config option MaxDropshipItems.
- Added a synced price multiplier config option for every moon, item, and unlockable, including ones added by mods to the Synced config. This works as a workaround for price changes from [Advanced Company by PotatoePet](https://thunderstore.io/c/lethal-company/p/PotatoePet/AdvancedCompany/) not being retrievable.
  - When joining another server with this mod, your synced config options will sync with the host if they have the mod, otherwise all prices will be set to their default values (so you can't cheat).
  - When joining a host who has the mod while you do not, you'll be able to use and buy things on the terminal as normal, but the host will check to make sure any purchases you make were actually possible according to their config and sync any changes they need to make regarding your purchase accordingly (also so you can't cheat or accidentally break something).

## 1.0.1
- Switched from using the Terminal.Awake method to the Terminal.Start method, for better compatibility with other mods. Namely [Lategame Upgrades by malco](https://thunderstore.io/c/lethal-company/p/malco/Lategame_Upgrades/), as it would add information to the help node after I would change it and make the resulting node's result look strange.
- Moved appending `\n\n>` to the end of nodes to a post Terminal.ParsePlayerSentence method, so that the text is added to the end of other mod's nodes, making responses slightly more consistent everywhere.
- Made general improvements to the underlying code to make it more readable by adding some helper methods.
- Made command shortcuts actually change to the name of the command you type in visually upon pressing enter, fixing an incompatibility with [Lategame Upgrades by malco](https://thunderstore.io/c/lethal-company/p/malco/Lategame_Upgrades/) that made the `>TRANSMIT` command shortcut not transmit a message.
  - This also makes the command appear rather than the shortcut if the command doesn't clear all of the text previously there, as well as for [Terminal_History by NotAtomicBomb](https://thunderstore.io/c/lethal-company/p/NotAtomicBomb/Terminal_History/) when going back through your history.
- Fixed a (potential) small bug (with the terminal itself) that occured when running multiple commands that don't clear the previous text back to back. Doing so would continuously add 2 new lines before all of the previous text creating a big block of nothing at the top of the screen.
- The current time is now shown in the top right of the terminal. Like [Terminal_Clock by NotAtomicBomb](https://thunderstore.io/c/lethal-company/p/NotAtomicBomb/Terminal_Clock/)

## 1.0.0
- Initial release