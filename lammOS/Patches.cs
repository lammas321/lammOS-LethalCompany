using HarmonyLib;
using UnityEngine;
using static lammOS.lammOS;
using static lammOS.Commands.Commands;
using static lammOS.NewTerminal.NewTerminal;
using static lammOS.Variables.Variables;

namespace lammOS.Patches
{
    public static class Patches
    {
        [HarmonyPatch(typeof(Terminal))]
        public static class TerminalPatches
        {
            [HarmonyPatch("Awake")]
            [HarmonyPrefix]
            [HarmonyPriority(2147483647)]
            public static void PostAwake(ref Terminal __instance)
            {
                NewTerminal.NewTerminal.Terminal = __instance;
            }

            [HarmonyPatch("Start")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostStart(ref Terminal __instance)
            {
                Setup();
            }

            [HarmonyPatch("QuitTerminal")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostQuitTerminal(ref Terminal __instance)
            {
                __instance.textAdded = 0;
                inputIndex = 0;

                if (currentCommand != null)
                {
                    currentCommand.QuitTerminal(__instance);
                    currentCommand = null;
                }

                commandHistoryIndex = -1;
            }

            [HarmonyPatch("TextChanged")]
            [HarmonyPrefix]
            [HarmonyPriority(2147483647)]
            public static bool PreTextChanged(ref Terminal __instance, ref string newText)
            {
                OnTextChanged();

                return false;
            }

            [HarmonyPatch("OnSubmit")]
            [HarmonyPrefix]
            [HarmonyPriority(2147483647)]
            public static bool PreOnSubmit(ref Terminal __instance)
            {
                if (__instance.currentNode != null && (__instance.currentNode.acceptAnything || __instance.currentNode.overrideOptions))
                {
                    RemoveVanillaKeywords();
                    return true;
                }

                OnSubmit();

                if (currentCommand == null)
                {
                    RemoveVanillaKeywords();
                    return true;
                }
                return false;
            }

            [HarmonyPatch("OnSubmit")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostOnSubmit()
            {
                if (terminalKeywordsBackup != null)
                {
                    NewTerminal.NewTerminal.Terminal.terminalNodes.allKeywords = terminalKeywordsBackup;
                    terminalKeywordsBackup = null;
                }
                if (currentCommand != null && currentCommand.blockingLevel == BlockingLevel.None)
                {
                    currentCommand = null;
                }
            }

            [HarmonyPatch("LoadNewNode")]
            [HarmonyPrefix]
            [HarmonyPriority(2147483647)]
            public static void PreLoadNewNode(ref TerminalNode node)
            {
                node.maxCharactersToType = 2147483647;

                if (!node.clearPreviousText && !node.displayText.StartsWith("</noparse>"))
                {
                    previousNodeText = node.displayText;
                    node.displayText = "</noparse>" + node.displayText;
                    return;
                }

                if (node.displayText.StartsWith(">MOONS\n"))
                {
                    previousNodeText = node.displayText;
                    node.displayText = HelpCommand.GenerateHelpPage();
                    node.displayText = HandleDisplayText(node.displayText);
                    return;
                }

                if (node.displayText.Contains("Halden Electronics Inc."))
                {
                    previousNodeText = node.displayText;
                    node.displayText = HandleDisplayText(node.displayText.Replace("Halden Electronics Inc.", "lammas123").Replace("FORTUNE-9", "lammOS"));
                    return;
                }

                int startupIndex = node.displayText.IndexOf("Welcome to the FORTUNE-9 OS");
                if (startupIndex != -1)
                {
                    previousNodeText = node.displayText;
                    node.displayText = HandleDisplayText(node.displayText.Substring(0, startupIndex) + NewTerminalStartupText);
                    return;
                }
            }

            [HarmonyPatch("LoadNewNode")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostLoadNewNode(ref Terminal __instance, ref TerminalNode node)
            {
                SetTerminalText(__instance.screenText.text, 1, false);

                if (previousNodeText != "")
                {
                    node.displayText = previousNodeText;
                    previousNodeText = "";
                }

                SetBodyHelmetCameraVisibility();
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

                if (numItemsInShip > SyncedConfig.SyncedConfig.Instance.MaxDropshipItemsValue)
                {
                    lammOS.Logger.LogWarning("The item amount purchased by a client goes over max dropship items, canceling purchase");
                    __instance.SyncGroupCreditsServerRpc(__instance.groupCredits, __instance.numberOfItemsInDropship);
                    return false;
                }

                int cost = 0;
                foreach (int itemIndex in boughtItems)
                {
                    foreach (string itemId in Variables.Variables.purchasableItems.Keys)
                    {
                        if (Variables.Variables.purchasableItems[itemId].index == itemIndex)
                        {
                            cost += Variables.Variables.GetItemCost(itemId, true);
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
        }

        [HarmonyPatch(typeof(StartOfRound))]
        public static class StartOfRoundPatches
        {
            [HarmonyPatch("Start")]
            [HarmonyPostfix]
            [HarmonyPriority(-2147483648)]
            public static void PostStart()
            {
                if (DisableIntroSpeechValue)
                {
                    savedShipIntroSpeechSFX = StartOfRound.Instance.shipIntroSpeechSFX;
                    StartOfRound.Instance.shipIntroSpeechSFX = StartOfRound.Instance.disableSpeakerSFX;
                }
            }

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
                foreach (string m in Variables.Variables.moons.Keys)
                {
                    if (Variables.Variables.moons[m].node.buyRerouteToMoon == levelID)
                    {
                        moonId = m;
                        break;
                    }
                }
                if (moonId == null)
                {
                    return true;
                }

                int cost = Variables.Variables.GetMoonCost(moonId);
                if (NewTerminal.NewTerminal.Terminal.groupCredits - cost != newGroupCreditsAmount)
                {
                    lammOS.Logger.LogWarning("Routing to moon was bought by a client for an incorrect price");

                    if (NewTerminal.NewTerminal.Terminal.groupCredits - cost < 0)
                    {
                        lammOS.Logger.LogWarning("Resulting credits of routing is negative, canceling purchase");
                        NewTerminal.NewTerminal.Terminal.SyncGroupCreditsServerRpc(NewTerminal.NewTerminal.Terminal.groupCredits, NewTerminal.NewTerminal.Terminal.numberOfItemsInDropship);
                        return false;
                    }
                    lammOS.Logger.LogWarning("Resulting credits of routing is positive, fix and sync credits but allow purchase");
                    newGroupCreditsAmount = Mathf.Clamp(NewTerminal.NewTerminal.Terminal.groupCredits - cost, 0, 10000000);
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
                foreach (string u in Variables.Variables.purchasableUnlockables.Keys)
                {
                    if (Variables.Variables.purchasableUnlockables[u].node.shipUnlockableID == unlockableID)
                    {
                        unlockableId = u;
                        break;
                    }
                }
                if (unlockableId == null)
                {
                    return true;
                }

                int cost = Variables.Variables.GetUnlockableCost(unlockableId);
                if (NewTerminal.NewTerminal.Terminal.groupCredits - cost != newGroupCreditsAmount)
                {
                    lammOS.Logger.LogWarning("Unlockable bought by a client for an incorrect price");
                    if (NewTerminal.NewTerminal.Terminal.groupCredits - cost < 0)
                    {
                        lammOS.Logger.LogWarning("Resulting credits of purchase is negative, canceling purchase");
                        NewTerminal.NewTerminal.Terminal.SyncGroupCreditsServerRpc(NewTerminal.NewTerminal.Terminal.groupCredits, NewTerminal.NewTerminal.Terminal.numberOfItemsInDropship);
                        return false;
                    }
                    lammOS.Logger.LogWarning("Resulting credits of purchase is positive, fix and sync credits but allow purchase");
                    newGroupCreditsAmount = Mathf.Clamp(NewTerminal.NewTerminal.Terminal.groupCredits - cost, 0, 10000000);
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