using GameNetcodeStuff;
using LethalCompanyInputUtils.Api;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace lammOS.Keybinds
{
    internal class Keybinds : LcInputActions
    {
        internal static List<string> commandHistory = new();
        internal static int commandHistoryIndex = -1;
        internal static string lastTypedCommand = "";

        [InputAction("<Keyboard>/leftArrow", Name = "Previous Radar Target")]
        internal InputAction PreviousRadarTargetKey { get; set; }
        [InputAction("<Keyboard>/rightArrow", Name = "Next Radar Target")]
        internal InputAction NextRadarTargetKey { get; set; }

        [InputAction("<Keyboard>/upArrow", Name = "Previous Command History")]
        internal InputAction PreviousCommandHistoryKey { get; set; }
        [InputAction("<Keyboard>/downArrow", Name = "Next Command History")]
        internal InputAction NextCommandHistoryKey { get; set; }

        internal static Keybinds Instance;

        internal static void Setup()
        {
            Instance = new Keybinds();

            Instance.PreviousRadarTargetKey.performed += OnPreviousRadarTargetKeyPressed;
            Instance.NextRadarTargetKey.performed += OnNextRadarTargetKeyPressed;
            Instance.PreviousCommandHistoryKey.performed += OnPreviousCommandHistoryKeyPressed;
            Instance.NextCommandHistoryKey.performed += OnNextCommandHistoryKeyPressed;
        }

        internal static void OnPreviousRadarTargetKeyPressed(InputAction.CallbackContext context)
        {
            PreviousRadarTarget();
        }
        public static void PreviousRadarTarget()
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
                if (component == null || component.isPlayerControlled || component.isPlayerDead || component.redirectToEnemy != null)
                {
                    break;
                }
                index--;
                continue;
            }

            StartOfRound.Instance.mapScreen.SwitchRadarTargetAndSync(index);
        }
        internal static void OnNextRadarTargetKeyPressed(InputAction.CallbackContext context)
        {
            NextRadarTarget();
        }
        public static void NextRadarTarget()
        {
            if (GameNetworkManager.Instance.localPlayerController.inTerminalMenu)
            {
                StartOfRound.Instance.mapScreen.SwitchRadarTargetAndSync((StartOfRound.Instance.mapScreen.targetTransformIndex + 1) % StartOfRound.Instance.mapScreen.radarTargets.Count);
            }
        }

        internal static void OnPreviousCommandHistoryKeyPressed(InputAction.CallbackContext context)
        {
            PreviousCommandHistory();
        }
        public static void PreviousCommandHistory()
        {
            if (!GameNetworkManager.Instance.localPlayerController.inTerminalMenu || commandHistoryIndex == 0 || commandHistory.Count == 0)
            {
                return;
            }

            if (commandHistoryIndex == -1)
            {
                lastTypedCommand = Variables.Variables.Terminal.screenText.text.Substring(Variables.Variables.Terminal.screenText.text.Length - Variables.Variables.Terminal.textAdded);
                commandHistoryIndex = commandHistory.Count;
            }

            commandHistoryIndex--;
            Variables.Variables.Terminal.screenText.text = Variables.Variables.Terminal.screenText.text.Substring(0, Variables.Variables.Terminal.screenText.text.Length - Variables.Variables.Terminal.textAdded) + commandHistory[commandHistoryIndex];
        }
        internal static void OnNextCommandHistoryKeyPressed(InputAction.CallbackContext context)
        {
            NextCommandHistory();
        }
        public static void NextCommandHistory()
        {
            if (!GameNetworkManager.Instance.localPlayerController.inTerminalMenu || commandHistoryIndex == -1)
            {
                return;
            }
            commandHistoryIndex++;

            if (commandHistoryIndex == commandHistory.Count)
            {
                commandHistoryIndex = -1;
                Variables.Variables.Terminal.screenText.text = Variables.Variables.Terminal.screenText.text.Substring(0, Variables.Variables.Terminal.screenText.text.Length - Variables.Variables.Terminal.textAdded) + lastTypedCommand;
                return;
            }

            Variables.Variables.Terminal.screenText.text = Variables.Variables.Terminal.screenText.text.Substring(0, Variables.Variables.Terminal.screenText.text.Length - Variables.Variables.Terminal.textAdded) + commandHistory[commandHistoryIndex];
        }
    }
}