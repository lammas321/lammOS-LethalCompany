# What is lammOS?
lammOS is a mod that reworks how the terminal commands work, changing how many of them function, and adding several new ones.

# What's Reworked
The way commands are parsed and processed by the terminal have been reworked, making using and adding new content to the terminal a smoother experience.
- This has the negative side effect of command results not being 100% similar or synced with the vanilla game or other mods, especially if the game updates or mods change or add content making any drastic changes.
- This will also cause commands added or changed by other mods to *potentially* be unusable with the terminal, though some will still work with minimal issues. It is possible for other mods to add their own lammOS compatible commands with an optional or required dependency if they so choose though, minimizing all potential issues.

## Specifics
More specifically for modders or others that are curious, what I did is pre-patch the Terminal.OnSubmit method to conditionally do nothing if the player enters a valid lammOS command, which will then run my own mod's NewTerminal.OnSubmit method and thus executing the entered command. Otherwise, it'll run the default Terminal.OnSubmit method and check for terminal keyword matches as usual.
- lammOS also removes most of the vanilla terminal keywords, so that users aren't routing to moons or buying items with the terminal keywords.

I also pre-patched the Terminal.TextChanged method to run my own NewTerminal.OnTextChanged method, which is functionally similar but different so that it works better with lammOS.
- Several other Terminal methods have been patched, but are less signifigant and are only there to prevent bugs or weird visuals.


# What's Changed
## Commands
The `>HELP` command now lists all of the available commands and the arguments they take in, and giving it the name of another command as a argument will give you more information about that command.
Most commands now have shortcuts, all of which are viewable with the `>SHORTCUTS` command, but for some examples:
- `>MOON` -> `>M`
- `>PING` -> `>P`
- `>TRANSMIT` -> `>T`

Commands like `>MOON` and `>BESTIARY` will show even more details about the moon or entity you are curious about.
The `>STORE` command will hide upgrades and other decorations that have already been purchased.

The `>SCAN` command now shows three categories for scanned items: in the ship, inside the building, and outside, as well as what items are at these locations and their values.
- As of v49, joining lobbies will show any scrap in the ship as being inside until they are picked up and dropped, this does not occur if you are the host.
- Additionally, bee hives will appear as being inside instead of outside until they are picked up and dropped.

The `>VIEW MONITOR` command has been simplified to `>MONITOR`, or simply `>V` with the command's shortcut, and the monitor now shows up instantly rather than slowly loading after entering the command.

The `>SWITCH` command will switch to the next radar target if entered with no arguments, as if you pushed the white button on the monitor.
The `>PING` and `>FLASH` commands will now act on the radar booster currently being viewed if entered with no arguments.

## Config Options
You can disable confirmation screens for commands such as `>ROUTE` and `>BUY` by disabling the ShowCommandConfirmations config option.

You can change how many characters you must enter for the terminal to understand what you were trying to buy or view, this can be changed using the CharsToAutocomplete config option, but keep in mind that it will pick the first thing it finds that matches the first couple character(s) you enter and assume that's what you wanted.
- Setting the CharsToAutocomplete config option to a value above the character length of certain options will require you to type the full name of the option.
- You can enable the ShowMinimumChars config option to show the minimum number of characters you need to enter next to the names of things in lists, such as on the `>MOONS` and `>STORE` pages.


# What's New
## Commands
The `>SHORTCUTS` command, which will list all of the shortcuts associated with every command.

The `>TARGETS` command, which lists all of the radar targets you can view on the monitor, or the terminal with the `>MONITOR` command.

A `>CLEAR` command that will clear all text from the terminal, useful if you can't see the monitor while the monitor is on screen after using the `>MONITOR` command.

The `>CODES` command, this lists all of the alphanumeric codes you can enter and what they are associated with (doors, turrets, or landmines) on the moon you are currently on.

Entering an alphanumeric code can now take an argument to decide what it should do. These options are Default, Toggle, Deactivate, and Activate.
- All but the default option will be disabled by default and will require their respective `alphanumericcode+<argument>` commands to be enabled in the synced config.

The `>DOOR` and `>LIGHTS` commands to toggle the ship's door open or closed and lights on or off.
The `>TP` and `>ITP` commands to remotely activate the teleporter and inverse teleporter.

The `>RELOAD` command, to reload the config and fix any inconsistencies experienced with the terminal. There shouldn't be any inconsistencies unless you have mods that change certain properties while you're in game, if these changes are made beforehand, then there should be no problems.
- A list of properties that may need reloading if altered by mods, though this list is not limited to these properties, potentially includes:
  - The list of purchaseble items and unlockables
  - Entity invulnerability
  - Entity power and the max that can spawn
  - Ect.

You can create and run macros that'll run a set of commands you give them (`>CREATE-MACRO` and `>RUN-MACRO`), great for setting up a 'kit' macro so that you can buy a lot of things you normally do quickly.


## Config Options and Keybinds
A clock has been added to the top right corner of the terminal, optional with the config option ShowTerminalClock.

Using the left and right arrow keys while on the terminal will make you switch between radar targets on the ships monitor. Keybinds can be changed in the game's keybinds menu.

Using the up and down arrow keys will let you go through your command history to run commands again or correct typos near the end of a command without having to type the full command again. Keybinds can be changed in the game's keybinds menu.

You can choose the padding character used in commands that show lists of things with the config option ListPaddingChar.

You can choose whether to show percentages (20%) or rarities (1/5) for things involving chance with the config option ShowPercentagesOrRarity.

There is a synced config that allows you to change the maximum amount of items on the delivery dropship, set the maximum number of commands executable per second within macros, enable or disable commands, and change the prices for every moon, item, and unlockable.
- All of these options will be shared by the host to the rest of the players who also have the mod. Joining players are not required to have the mod, but purchases made by them will be corrected by the host and they won't be blocked from running vanilla commands.
  - The host is not required to have the mod either, as players joining with the mod will use the default synced config options and not their set options.

You can optionally disable the introduction speech with the DisableIntroSpeech config option.


# What's Planned
Potentially changing more of the text within the terminal to use other colors.

Maybe adding conditional or looping instructions to macros? Maybe variables and being able to take user input?

Showing the entity's base health when viewing one using the `>BESTIARY` command.
- This isn't hard if done with fixed values, but will be annoying to do dynamically, which is how I'd want to do it.

Supporting translations, or mods that change text in several areas in general.


# Known Incompatibilities
[Advanced Company by PotatoePet](https://thunderstore.io/c/lethal-company/p/PotatoePet/AdvancedCompany/)
- Price changes don't sync, this has to do with how Advanced Company saves these changes, not making it possible for me to access them.
  - The creator of the mod has plans to make an api that will allow me and other mod creators to get and modify these prices.

[GeneralImprovements by ShaosilGaming](https://thunderstore.io/c/lethal-company/p/ShaosilGaming/GeneralImprovements/)
- When GeneralImprovements' UseBetterMonitors config option is set to true, it breaks the cameras from [Helmet_Cameras by RickArg](https://thunderstore.io/c/lethal-company/p/RickArg/Helmet_Cameras/) and [Solos_Bodycams by CapyCat](https://thunderstore.io/c/lethal-company/p/CapyCat/Solos_Bodycams/), making me unable to display their cameras on the monitor after using the `>MONITOR` command.
  - The [OpenBodyCams mod by Zaggy1024](https://thunderstore.io/c/lethal-company/p/Zaggy1024/OpenBodyCams/) is not broken by GeneralImprovements, as OpenBodyCams has compatibility for GeneralImprovements built in, and can be viewed on the terminal via the `>MONITOR` command as of v1.4.0 (so long as OpenBodyCams' GeneralImprovementsBetterMonitorIndex config option is set to 0 or 14, placing the camera on the bottom right monitor).

[OpenBodyCams by Zaggy1024](https://thunderstore.io/c/lethal-company/p/Zaggy1024/OpenBodyCams/)
- The helmet camera can sometimes become frozen on the terminal when using the `>MONITOR` command if the monitor isn't visible on the actual monitor.
  - I think it may be caused by OpenBodyCams freezing the camera to improve performance if it isn't visible on the monitor, though I'm not entirely sure.


# Contact
@lammas123 on Discord - [Lethal Company Modding Discord](https://discord.com/invite/lcmod) - [lammOS Thread](https://discord.com/channels/1168655651455639582/1196941743673847938)


# Changelog
## 1.4.0
- Changed the file macros are saved within from a .es3 file to a plain .txt file in the config folder, so they are easier to share with others and save elsewhere.
  - Your existing .es3 macros file will be converted to a .txt file upon launching the game, saving your existing macros. This functionality will be removed eventually, so be sure to launch the game soon to convert the file if you want to save your macros!
- The `>CODES` command will now show if doors are open or closed, and if turrets or landmines are active or deactivated.
- The `>TARGETS` command will now specify if a target is a radar booster.
- Added an error to let clients know that the `>EJECT` command only able to be used by the host. Not that they could use it before as the game would prevent it, but they had no reason to know that it was prevented besides nothing happening and an error log in the console.
- Added a light blue text color to my name on the startup screen, as well as a red text color for any lammOS command errors.
- Made the `>HELP` command's page automatically show any time after the first time you open the terminal after loading a save or joining a game.
- This update improves compatibility with a couple of terminal based mods that didn't work with lammOS before, such as [suitsTerminal by darmuh](https://thunderstore.io/c/lethal-company/p/darmuh/suitsTerminal/).
- Fixed an incompatibility with [Mimics by x753](https://thunderstore.io/c/lethal-company/p/x753/Mimics/) where the Mimic's bestiary entry couldn't be added, as the mod doesn't add the mimic entity in the same manner that other mods do.
- Slightly fixed some incompatibility with [GeneralImprovements by ShaosilGaming](https://thunderstore.io/c/lethal-company/p/ShaosilGaming/GeneralImprovements/), read the `Known Incompatibilities` section for GeneralImprovements for more information.
- Fixed a bug that would cause items you purchased as a client to not get sent to and synced with the host if you purchased exactly a multiple of 12 plus 1 (1, 13, 25, ect) until you purchased more.
- Changed the MacroInstructionsPerSecond synced config option from an integer to a float, so that instructions per second don't have to be whole numbers nor at least once per second.
- Removed the DisableTextPostProcess config option, as lammOS no longer needs to disable it.
- Added the DisableIntroSpeech config option, so that you don't have to stop it thousands of times during testing and go insane, like me :D
- Publicized the game's assembly within the .csproj file to be able to interact with the terminal more closely.
- Switched to pre-patching and preventing default execution of the Terminal.OnSubmit method conditionally, based on if a lammOS command was found and ran, otherwise it'll allow default execution of the Terminal.OnSubmit method.
  - The mod now removes terminal keywords that are already implemented via lammOS commands, to prevent getting around the functionality implemented by lammOS.
- Created the NewTerminal static class with a few helper methods to make interacting with the new terminal easier.
- Made several improvements to how commands are made and handled via lammOS.
  - Commands no longer return or use terminal nodes, they now directly interact with the terminal's methods through helper methods in the new NewTerminal class.
- Added a static helper method to the Command class for formatting any type of list with padding. For example, the responses of `>MOONS` and `>STORE` commands.
- Varied the types of commands there can be internally by creating more derived command classes.
- Overhauled how commands block command execution, for example by prompting confirmation or by disallowing input for a period of time.
  - The `>WAIT` command now functions on its own without the need to be used within a macro, if you'd ever want to use it outside of a macro *shrug*.
- Made changes to prevent the user from entering rich text (custom colors, size changes, ect) into the terminal, preventing strange visual bugs that could occur when the user did so.

## 1.3.1
- Reworked parsing and caching of moon's information to be much better, making it more like how the game already grabs this information by default.
  - I've tested this with all of the moons on the first page of r2modman after searching "moon", and all of them worked!
- Added a styledName property to the Variables.Moon class, which includes a - instead of a space between the moon's number and name, only if the moon's name starts with numbers and a space, otherwise it is the same as the moon's base name.

## 1.3.0
- Made lots of changes regarding my mod's .csproj file (thanks to @nyxchrono on Discord) and upgraded to VSCode 2022, leading to me making lots of minor changes here and there with the newer IDE.
  - Additionally, the project's .sln and .csproj files are available on the [GitHub](https://github.com/lammas321/lammOS-LethalCompany), contributions and suggestions are appreciated!
- Split the mod's underlying code into more than one .cs file.
- Made changes to command arguments to match more with already defined command argument standards.
- Made changes to the results of commands that list a lot of things to , like `>STORE` and `>SCAN`, giving them padding to make prices and values aligned.
  - The character used for this can be modified in the config using the ListPaddingChar option.
- Made lots of changes to the results of commands, making them more consistent throughout, giving them more detail, and making them feel less empty.
- Moved the `>CODES` and `>CODE` command to the new Alphanumeric Codes category.
- Made it easier to understand the `>CODE` command and why it's there.
- Added the `>WAIT` command to be used within macros to make the macro wait for a given amount of time.
  - This command doesn't do anything normally, and is only used for macros.
- Added a MacroInstructionsPerSecond synced config option, allowing the host to customize how many commands can be ran through a macro per second.
- Added host to client version mismatch warnings for the synced config to the console, this will help with diagnosing problems related to hosts and clients not having the same version of the mod.
- Changed the default value of all price modifiers in the synced config to -1. A value of -1 will signify that the multiplier should be ignored and to use the default price.
- Made several changed to how I load and cache game related information into lammOS on start.
  - Fixed an incompatibility with [More_Suits by x753](https://thunderstore.io/c/lethal-company/p/x753/More_Suits/) by improving how purchasable unlockables are loaded into lammOS.
  - Fixed a potential error that could have occured if modded planets didn't follow the same naming scheme that vanilla ones did.
- Made lots of changes to how commands are parsed and executed, removing unnecessary uses of the ref keyword and passes of the whole TerminalNode when just passing and or returning a string result would suffice.
- Fixed a typo I've been making throughout my code regarding 'purchaseables', it's purchasables haha.

## 1.2.3
- Added a helpful description to all of the first config options in the Synced config per category.
- Actually fixed the issue with items not being properly discounted.
- Made some code readability improvements to getting a moon, item, or unlockables price.

## 1.2.2
- Added `>MONITOR` body/helmet camera support for [Solos_Bodycams by CapyCat](https://thunderstore.io/c/lethal-company/p/CapyCat/Solos_Bodycams/) and [OpenBodyCams by Zaggy1024](https://thunderstore.io/c/lethal-company/p/Zaggy1024/OpenBodyCams/).
- Improved the valid radar targets filter on the `>TARGETS` command to use the same one that switching to targets on the monitor actually uses, rather than filtering out names that start with "Player #".
- Cached the Terminal object as a static property so that I don't need to use the slower FindObjectOfType method to get it in some places.
- Replaced the usages of FindObjectOfType<StartOfRound>() with StartOfRound.Instance.
- Fixed purchasable items not properly being discounted in the store or while purchasing items. (How did I miss this??)
- Fixed using the left arrow to switch to previous radar targets skipping some targets when it shouldn't.

## 1.2.1
- Added an `enabled` property to all commands that is set to true by default. Though some commands, like a few I am adding in this update, will be disabled by default.
  - Additionally, I've added a section to the Synced config for commands to be to set as enabled or disabled by the host.
- Added the `>LAND` and `>LAUNCH` commands to the Interactive help category as confirmation commands, meaning they depend on the ShowCommandConfirmations config option.
- Added the `>CODE` command to the Radar help category as a visual command. Typing 'code' or 'code b2' won't do anything, but typing 'b2' as usual will interact with objects on the radar map.
- Added the optional arguments default, toggle, deactivate, and activate to the alphanumeric code commands, with default just being normal functionality.
  - The toggle argument is disabled by default, but will only toggle door's open or closed states with the entered code.
  - The deactivate argument is disabled by default, but will only deactivate mines and turrets with the entered code.
  - The activate argument is disabled by default, but will trigger mines to detonate and turrets to go haywire with the entered code.
- If the [Helmet_Cameras mod by RickArg](https://thunderstore.io/c/lethal-company/p/RickArg/Helmet_Cameras/) is on, using the `>MONITOR` command will show both the map and radar target's helmet camera on the terminal side by side.
- Fixed an error that could occur if the amount of text you typed into the terminal was longer than the amount of text output by a command after submitting it, this error didn't cause any problems as far as I could tell.
- Patched an error that could occur if more than a single unlockable with the same name existed were added, causing lots of functionality with lammOS to break. This was seemingly caused by some additional suit mods.

## 1.2.0
- Added a command category feature, which sorts terminal commands into categories (based on what it does or what mod added it) in the `>HELP` command's response.
- Slightly modified how commands are added/registered with the mod, as well as how commands are handled.
- Added some command compatibility with the [Lategame Upgrades mod by malco](https://thunderstore.io/c/lethal-company/p/malco/Lategame_Upgrades/), by giving it it's own help page category if you have the mod enabled. These are purely visual, the commands are still parsed by Lategame Upgrades.
- Added functionality for confirmation screens after entering commands like `>BUY` and `>ROUTE`, as well as a config option to make them optional.
  - The confirmation screens will be enabled by default, you'll have to disable them through the config once you run the mod if you liked how commands didn't ask for confirmation before.
- Added the ability to create saveable macros and run them with several macro based commands, which when ran execute a series of instructions you tell it to.
  - These are saved in the file "lammas123.lammOS.Macros.es3" in BepInEx's config folder, making them shareable but sending this file to others. Alternatively you could share the command you used to create the macro and have them copy and paste it.
- Implemented between radar targets using the left and right arrow keys while on the terminal, like the [FastSwitchPlayerViewInRadar mod by kRYstall9](https://thunderstore.io/c/lethal-company/p/kRYstall9/FastSwitchPlayerViewInRadar/).
- Implemented a command history using the up and down arrow keys, like the [Terminal_History mod by NotAtomicBomb](https://thunderstore.io/c/lethal-company/p/NotAtomicBomb/Terminal_History/). The maximum amount of commands to save to the history configurable with the MaxCommandHistory config option.
  - The keybinds for switching between radar targets and shifting through command history are configurable within the game's keybind settings menu with the use of the [LethalCompany_InputUtils mod by Rune5680](https://thunderstore.io/c/lethal-company/p/Rune580/LethalCompany_InputUtils/).

## 1.1.2
- Fixed an incompatibility with [Lategame Upgrades by malco](https://thunderstore.io/c/lethal-company/p/malco/Lategame_Upgrades/) and other mods that modified the help node's results, making lammOS' replacement of it more mod friendly, as it was causing Lategame Upgrades to throw errors.
  - This incompatibility in particular caused quite a few problems for lammOS, as Lategame Upgrades throwing this error would cause a slight desync from the Terminal for lammOS. This lead to the case where routing to a paid moon would attempt to use the Terminal from the first lobby the host created, but never any lobby after that until they restarted their game.
- Made adding entities to the list of entities viewable with the `>BESTIARY` command more broad, as it wouldn't add the Rolling Giant from the [RollingGiant mod by NomnomAB](https://thunderstore.io/c/lethal-company/p/NomnomAB/RollingGiant/). 

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