using System;
using System.Collections.Generic;
using UnityEngine;
using static lammOS.lammOS;
using static lammOS.Commands.Commands;

namespace lammOS.NewTerminal
{
    public static class NewTerminal
    {
        public static Terminal Terminal { get; internal set; }
        public static int inputIndex { get; internal set; } = 0;
        public static readonly int MaxTerminalCharWidth = 50;

        public static Command currentCommand { get; internal set; } = null;

        public static bool saveSubmissionsToHistory = true;
        internal static List<string> commandHistory = new();
        internal static int commandHistoryIndex = -1;
        internal static string lastTypedCommand = "";

        internal static string previousNodeText = "";
        public static readonly string NewTerminalStartupText = "Powered by lammOS     Created by <color=#007fff>lammas123</color>\n          Courtesy of the Company\n\nType HELP for a list of available commands.";

        public static string HandleDisplayText(string text)
        {
            if ((bool)Terminal.displayingPersistentImage)
            {
                text = text.Replace("\n\n\nn\n\n\n", "");
            }

            text = text.TrimStart('\n').TrimEnd('\n');

            if (text == "")
            {
                return "><noparse>";
            }

            if (text.EndsWith("\n\n><noparse>"))
            {
                return text;
            }

            return text + "\n\n><noparse>";
        }
        public static string HandleInputText(string input)
        {
            return input.Replace("<noparse>", "").Replace("</noparse>", "");
        }
        
        public static void SetTerminalText(string text, int forceScroll = 1, bool selectTextField = true)
        {
            Terminal.modifyingText = true;
            text = "\n\n" + HandleDisplayText(text);
            inputIndex = text.Length;
            Terminal.screenText.text = text;

            if (selectTextField)
            {
                Terminal.screenText.ActivateInputField();
                Terminal.screenText.Select();
            }
            
            ForceScrollbar(forceScroll);
        }
        public static void SetDisplayText(string text, int forceScroll = 1, bool selectTextField = true)
        {
            Terminal.modifyingText = true;
            string input = Terminal.screenText.text.Substring(inputIndex);
            text = "\n\n" + HandleDisplayText(text);
            inputIndex = text.Length - input.Length;
            Terminal.screenText.text = text + input;

            if (selectTextField)
            {
                Terminal.screenText.ActivateInputField();
                Terminal.screenText.Select();
            }

            ForceScrollbar(forceScroll);
        }
        public static void SetInputText(string text, int forceScroll = -1, bool selectTextField = true)
        {
            Terminal.screenText.text = Terminal.screenText.text.Substring(0, inputIndex) + HandleInputText(text);

            if (selectTextField)
            {
                Terminal.screenText.ActivateInputField();
                Terminal.screenText.Select();
            }

            ForceScrollbar(forceScroll);
        }

        public static void AppendTerminalText(string text, int forceScroll = -1, bool selectTextField = true)
        {
            Terminal.modifyingText = true;
            text = "</noparse>\n\n" + HandleDisplayText(text);
            inputIndex = Terminal.screenText.text.Length + text.Length;
            Terminal.screenText.text += text;

            if (selectTextField)
            {
                Terminal.screenText.ActivateInputField();
                Terminal.screenText.Select();
            }

            ForceScrollbar(forceScroll);
        }

        public static void PlaySyncedClip(int clipIndex)
        {
            if (clipIndex < 0 || clipIndex >= Terminal.syncedAudios.Length)
            {
                lammOS.Logger.LogWarning(clipIndex.ToString() + " is outside the synced clip index range of 0-" + (Terminal.syncedAudios.Length - 1).ToString() + ".");
                return;
            }

            Terminal.PlayTerminalAudioServerRpc(clipIndex);
        }
        public static void PlayClip(AudioClip clip)
        {
            if (clip == null)
            {
                lammOS.Logger.LogWarning("The provided audio clip may not be null.");
                return;
            }

            Terminal.terminalAudio.PlayOneShot(clip);
        }

        public static void ForceScrollbar(int forceScroll = 1)
        {
            if (forceScroll < 0)
            {
                if (Terminal.forceScrollbarCoroutine != null)
                {
                    Terminal.StopCoroutine(Terminal.forceScrollbarCoroutine);
                }
                Terminal.forceScrollbarCoroutine = Terminal.StartCoroutine(Terminal.forceScrollbarDown());
            }
            else if (forceScroll > 0)
            {
                if (Terminal.forceScrollbarCoroutine != null)
                {
                    Terminal.StopCoroutine(Terminal.forceScrollbarCoroutine);
                }
                Terminal.forceScrollbarCoroutine = Terminal.StartCoroutine(Terminal.forceScrollbarUp());
            }
        }

        public static void OnTextChanged()
        {
            if (commandHistoryIndex != -1 && (inputIndex > Terminal.screenText.text.Length || commandHistory[commandHistoryIndex] != Terminal.screenText.text.Substring(inputIndex)))
            {
                commandHistoryIndex = -1;
            }

            if (Terminal.modifyingText)
            {
                Terminal.modifyingText = false;
                Terminal.currentText = Terminal.screenText.text;
                Terminal.textAdded = Terminal.screenText.text.Length - inputIndex;
                return;
            }

            if (inputIndex > Terminal.screenText.text.Length)
            {
                Terminal.screenText.text = Terminal.currentText;
                return;
            }

            string input = Terminal.screenText.text.Substring(inputIndex);
            string handledInput = HandleInputText(input);
            if (input != handledInput)
            {
                Terminal.screenText.text = Terminal.screenText.text.Substring(0, inputIndex) + handledInput;
                return;
            }

            if (currentCommand != null && currentCommand.blockingLevel >= BlockingLevel.UntilTyping)
            {
                currentCommand.Handle(Terminal, Terminal.screenText.text.Substring(inputIndex));
                return;
            }

            Terminal.currentText = Terminal.screenText.text;
            Terminal.textAdded = Terminal.screenText.text.Length - inputIndex;
        }

        public static void OnSubmit()
        {
            if (!Terminal.terminalInUse)
            {
                return;
            }

            string input = Terminal.screenText.text.Substring(inputIndex);
            if (input == "")
            {
                return;
            }

            if (saveSubmissionsToHistory)
            {
                commandHistory.Add(input);
                while (commandHistory.Count > MaxCommandHistoryValue)
                {
                    commandHistory.RemoveAt(0);
                }
            }
            
            HandleCommand(input);
        }

        public static void HandleCommand(string input)
        {
            if (currentCommand != null)
            {
                try
                {
                    currentCommand.Handle(Terminal, input);
                }
                catch (Exception e)
                {
                    lammOS.Logger.LogError("An error occurred handling the current command: '" + currentCommand.id + "'\n" + e.ToString());
                    Command.ErrorResponse("An error occurred handling the current command: '" + currentCommand.id + "'");
                }
                return;
            }

            int split = input.IndexOf(' ');
            string commandId, args;
            if (split == -1)
            {
                commandId = input;
                args = "";
            }
            else
            {
                commandId = input.Substring(0, split);
                args = input.Substring(split + 1);
            }

            if (IsShortcut(commandId))
            {
                commandId = GetCommandIdByShortcut(commandId);
                SetInputText((commandId + " " + args).TrimEnd(' '));
            }

            Command command = GetCommand(commandId);
            if (command == null || command is CommandArgument || command is CompatibilityCommand)
            {
                return;
            }

            currentCommand = command;
            if (!command.enabled)
            {
                Command.ErrorResponse("That command has been disabled by the host.");
                return;
            }

            try
            {
                command.Execute(Terminal, args);
            }
            catch (Exception e)
            {
                lammOS.Logger.LogError("An error occurred executing the command: '" + commandId + "'\n" + e.ToString());
                Command.ErrorResponse("An error occurred executing the command: '" + commandId + "'");
            }
        }
    }
}