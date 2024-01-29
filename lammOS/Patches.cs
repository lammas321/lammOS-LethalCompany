using HarmonyLib;
using System;
using UnityEngine;
using static lammOS.lammOS;
using static lammOS.Commands.Commands;
using static lammOS.Keybinds.Keybinds;
using static lammOS.Variables.Variables;

namespace lammOS.Patches
{
    public static class Patches
    {
        internal static string currentLoadedNode = "";
        internal static string startupNodeText = "";
        internal static string helpNodeText = "";

        [HarmonyPatch(typeof(Terminal))]
        public static partial class TerminalPatches
        {
            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostAwake(ref Terminal __instance)
            {
                Variables.Variables.Terminal = __instance;
            }

            [HarmonyPatch("Start")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostStart(ref Terminal __instance)
            {
                Variables.Variables.Terminal = __instance;

                lammOS.Setup();
            }

            [HarmonyPatch("ParsePlayerSentence")]
            [HarmonyPrefix]
            [HarmonyPriority(2147483647)]
            public static bool PreParsePlayerSentence(ref Terminal __instance, ref TerminalNode __result)
            {
                Variables.Variables.Terminal = __instance;

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

                HandleCommand(__instance, input, ref node);

                __result = node;
                return false;
            }

            [HarmonyPatch("ParsePlayerSentence")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostParsePlayerSentence(ref Terminal __instance, ref TerminalNode __result)
            {
                Variables.Variables.Terminal = __instance;

                HandleCommandResult(__instance, __result);
            }

            [HarmonyPatch("BuyItemsServerRpc")]
            [HarmonyPrefix]
            [HarmonyPriority(2147483647)]
            public static bool PreBuyItemsServerRpc(ref Terminal __instance, ref int[] boughtItems, ref int newGroupCredits, ref int numItemsInShip)
            {
                Variables.Variables.Terminal = __instance;

                if (!__instance.IsHost)
                {
                    return true;
                }

                if (numItemsInShip > SyncedConfig.SyncedConfig.Instance.MaxDropshipItemsValue)
                {
                    lammOS.Logger.LogWarning("The item amount purchased by a client goes over max dropship items, canceling purchase");
                    __instance.SyncGroupCreditsServerRpc(__instance.groupCredits, __instance.numberOfItemsInDropship);
                    return false;
                }

                int cost = 0;
                foreach (int itemIndex in boughtItems)
                {
                    foreach (string itemId in purchasableItems.Keys)
                    {
                        if (purchasableItems[itemId].index == itemIndex)
                        {
                            cost += GetItemCost(itemId, true);
                        }
                    }
                }

                if (__instance.groupCredits - cost != newGroupCredits)
                {
                    lammOS.Logger.LogWarning("Items were bought by a client for the incorrect price");
                    if (__instance.groupCredits - cost < 0)
                    {
                        lammOS.Logger.LogWarning("Resulting credits of this purchase is negative, canceling purchase");
                        __instance.SyncGroupCreditsServerRpc(__instance.groupCredits, __instance.numberOfItemsInDropship);
                        return false;
                    }
                    lammOS.Logger.LogWarning("Resulting credits of this purchase is positive, fix and sync credits but allow purchase");
                    newGroupCredits = Mathf.Clamp(__instance.groupCredits - cost, 0, 10000000);
                }

                return true;
            }

            [HarmonyPatch("LoadNewNode")]
            [HarmonyPrefix]
            [HarmonyPriority(2147483647)]
            public static void PreLoadNewNode(ref Terminal __instance, ref TerminalNode node)
            {
                Variables.Variables.Terminal = __instance;

                if (node.displayText.StartsWith("Welcome to the FORTUNE-9 OS"))
                {
                    startupNodeText = node.displayText;
                    node.displayText = newText;
                    node.maxCharactersToType = 99999;
                    currentLoadedNode = "startup";
                }
                else if (node.displayText.StartsWith(">MOONS\n"))
                {
                    helpNodeText = node.displayText;
                    node.displayText = newText;
                    node.maxCharactersToType = 99999;
                    currentLoadedNode = "help";
                }
            }

            [HarmonyPatch("LoadNewNode")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostLoadNewNode(ref Terminal __instance, ref TerminalNode node)
            {
                Variables.Variables.Terminal = __instance;

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

                SetBodyHelmetCamera();
            }

            [HarmonyPatch("BeginUsingTerminal")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostBeginUsingTerminal(ref Terminal __instance)
            {
                Variables.Variables.Terminal = __instance;
                
                currentCommand = null;
                if (runningMacroCoroutine != null)
                {
                    StartOfRound.Instance.StopCoroutine(runningMacroCoroutine);
                    runningMacroCoroutine = null;
                    macroAppendingText = false;
                }
                commandHistoryIndex = -1;
            }

            [HarmonyPatch("QuitTerminal")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostQuitTerminal(ref Terminal __instance)
            {
                Variables.Variables.Terminal = __instance;

                currentCommand = null;
                if (runningMacroCoroutine != null)
                {
                    StartOfRound.Instance.StopCoroutine(runningMacroCoroutine);
                    runningMacroCoroutine = null;
                    macroAppendingText = false;
                }
                commandHistoryIndex = -1;
            }

            [HarmonyPatch("TextChanged")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostTextChanged(ref Terminal __instance, ref string newText)
            {
                Variables.Variables.Terminal = __instance;

                if (commandHistoryIndex != -1 && commandHistory[commandHistoryIndex] != __instance.screenText.text.Substring(Math.Max(0, __instance.screenText.text.Length - __instance.textAdded)))
                {
                    commandHistoryIndex = -1;
                }

                if (runningMacroCoroutine != null && !macroAppendingText)
                {
                    StartOfRound.Instance.StopCoroutine(runningMacroCoroutine);
                    runningMacroCoroutine = null;

                    TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
                    node.displayText = "Macro execution interrupted by key press.\n\n>";
                    node.clearPreviousText = true;
                    node.terminalEvent = "";
                    node.playSyncedClip = terminalSyncedSounds["error"];

                    Variables.Variables.Terminal.LoadNewNode(node);
                }
            }

            [HarmonyPatch("TextPostProcess")]
            [HarmonyPrefix]
            [HarmonyPriority(2147483647)]
            public static bool PreTextPostProcess(ref Terminal __instance, ref string __result, string modifiedDisplayText)
            {
                Variables.Variables.Terminal = __instance;

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
            #if DEBUG
            [HarmonyPatch("Start")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostStart()
            {
                StartOfRound.Instance.shipIntroSpeechSFX = StartOfRound.Instance.disableSpeakerSFX;
            }
            #endif

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
                    if (moons[m].node.buyRerouteToMoon == levelID)
                    {
                        moonId = m;
                        break;
                    }
                }
                if (moonId == null)
                {
                    return true;
                }

                int cost = GetMoonCost(moonId);
                if (Variables.Variables.Terminal.groupCredits - cost != newGroupCreditsAmount)
                {
                    lammOS.Logger.LogWarning("Routing to moon was bought by a client for an incorrect price");

                    if (Variables.Variables.Terminal.groupCredits - cost < 0)
                    {
                        lammOS.Logger.LogWarning("Resulting credits of routing is negative, canceling purchase");
                        Variables.Variables.Terminal.SyncGroupCreditsServerRpc(Variables.Variables.Terminal.groupCredits, Variables.Variables.Terminal.numberOfItemsInDropship);
                        return false;
                    }
                    lammOS.Logger.LogWarning("Resulting credits of routing is positive, fix and sync credits but allow purchase");
                    newGroupCreditsAmount = Mathf.Clamp(Variables.Variables.Terminal.groupCredits - cost, 0, 10000000);
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
                foreach (string u in purchasableUnlockables.Keys)
                {
                    if (purchasableUnlockables[u].node.shipUnlockableID == unlockableID)
                    {
                        unlockableId = u;
                        break;
                    }
                }
                if (unlockableId == null)
                {
                    return true;
                }

                int cost = GetUnlockableCost(unlockableId);
                if (Variables.Variables.Terminal.groupCredits - cost != newGroupCreditsAmount)
                {
                    lammOS.Logger.LogWarning("Unlockable bought by a client for an incorrect price");
                    if (Variables.Variables.Terminal.groupCredits - cost < 0)
                    {
                        lammOS.Logger.LogWarning("Resulting credits of purchase is negative, canceling purchase");
                        Variables.Variables.Terminal.SyncGroupCreditsServerRpc(Variables.Variables.Terminal.groupCredits, Variables.Variables.Terminal.numberOfItemsInDropship);
                        return false;
                    }
                    lammOS.Logger.LogWarning("Resulting credits of purchase is positive, fix and sync credits but allow purchase");
                    newGroupCreditsAmount = Mathf.Clamp(Variables.Variables.Terminal.groupCredits - cost, 0, 10000000);
                }

                return true;
            }

            [HarmonyPatch("SwitchMapMonitorPurpose")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostSwitchMapMonitorPurpose(ref bool displayInfo)
            {
                if (displayInfo && bodyHelmetCameraImage != null)
                {
                    bodyHelmetCameraImage.enabled = false;
                }
            }
        }

        [HarmonyPatch(typeof(HUDManager))]
        public static class HUDManagerPatches
        {
            [HarmonyPatch("SetClock")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostSetClock(ref HUDManager __instance)
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
            public static void PostFillEndGameStats()
            {
                ClockText.text = "";
            }
        }
    }
}