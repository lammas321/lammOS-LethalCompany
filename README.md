# What is lammOS?
lammOS is a mod that reworks how the terminal commands work, changing how many of them function, and adding several new ones.

## What's Reworked
The way commands are parsed and processed by the terminal have been reworked, making using and adding new content to the terminal a smoother experience.
- This has the negative side effect of command results not being 100% similar or synced with the vanilla game or other mods, especially if the game updates or mods change or add content making any drastic changes.
- This will also cause commands added or changed by other mods that use nodes and keywords, what the game normally uses, to be unusable with the terminal. It is possible for other mods to add their own lammOS compatible commands with an optional or required dependency if they so choose though.

More specifically, for modders that are curious, what I did is pre-patch the Terminal.ParsePlayerSentence method to return my own TerminalNode that does basically nothing unless I need it to, such as to use it's displayText property. This does not run any of the code in the actual Terminal.ParsePlayerSentence method, instead I parse the text myself and run the corresponding command's Execute method to give it it's functionality, then it goes on to call the Terminal.LoadNewNode method as normal to update the terminal's on screen text.
I also pre-patched the Terminal.TextPostProcess method to do nothing by default, as it's functionality is not needed with this mod. This is toggleable within the config for compatibility with other mods however.

## What's Changed
- The screens that are shown upon opening the terminal have been changed to show that the terminal is powered by lammOS.
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
- The `>RELOAD` command, to reload the config and fix any inconsistencies experienced with the terminal. There shouldn't be any inconsistencies unless you have mods that change certain properties while you're in game, if these changes are made beforehand/ then there should be no problem.
  - A list of properties that may need reloading if altered by mods, though this list is not limited to these properties, potentially includes:
	- Purchaseable items and unlockables
    - Entity invulnerability
	- Entity power and the max that can exist
	- Ect.

## What's Planned
- Showing the entity's base health when viewing one using the `>BESTIARY` command.
- Fixing the possibility of any desyncing or inconsistencies occuring caused by other mods, so that the `>RELOAD` command doesn't need to be used.
- General improvements to the underlying code to make it more readable, would be done by adding some helper methods.
- Showing the current time in top right of the terminal. [Like Terminal_Clock by NotAtomicBomb](https://thunderstore.io/c/lethal-company/p/NotAtomicBomb/Terminal_Clock/)
- Implementing command history using the up and down arrow keys. [Like Terminal_History by NotAtomicBomb](https://thunderstore.io/c/lethal-company/p/NotAtomicBomb/Terminal_History/)
- Switching between players and radar boosters using the left and right arrow keys. [Like FastSwitchPlayerViewInRadar by kRYstall9](https://thunderstore.io/c/lethal-company/p/kRYstall9/FastSwitchPlayerViewInRadar/)
- Being able to use a walkie talkie while on the terminal. [Like TermSpeak by KodiCraft](https://thunderstore.io/c/lethal-company/p/KodiCraft/TermSpeak/)

# Changelog
## 1.0.0
- Initial release