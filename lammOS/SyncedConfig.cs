using BepInEx.Configuration;
using BepInEx;
using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Collections;
using Unity.Netcode;
using static lammOS.lammOS;
using static lammOS.Commands.Commands;
using static lammOS.Variables.Variables;

namespace lammOS.SyncedConfig
{
    [Serializable]
    public class SyncedConfig
    {
        internal string ModVersion = BepInEx.Bootstrap.Chainloader.PluginInfos["lammas123.lammOS"].Metadata.Version.ToString();

        internal int MaxDropshipItemsValue = DefaultMaxDropshipItems;
        internal float MacroInstructionsPerSecondValue = DefaultMacroInstructionsPerSecond;

        internal Dictionary<string, bool> EnabledCommands;

        internal Dictionary<string, float> MoonPriceMultipliers;
        internal Dictionary<string, float> ItemPriceMultipliers;
        internal Dictionary<string, float> UnlockablePriceMultipliers;

        internal static ConfigFile Config;

        internal static SyncedConfig Instance;
        internal SyncedConfig()
        {
            Instance = this;
            Config = new(Utility.CombinePaths(Paths.ConfigPath, "lammas123.lammOS.Synced.cfg"), false, MetadataHelper.GetMetadata(lammOS.Instance));
        }

        internal static void LoadSyncedConfigValues()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                if (File.Exists(Config.ConfigFilePath))
                {
                    Config.Reload();
                }
                else
                {
                    Config.Clear();
                }

                Instance.MaxDropshipItemsValue = Config.Bind("Synced", "MaxDropshipItems", DefaultMaxDropshipItems, "The maximum amount of items the dropship can have in it at a time.").Value;
                if (Instance.MaxDropshipItemsValue < 1)
                {
                    Instance.MaxDropshipItemsValue = 1;
                }

                Instance.MacroInstructionsPerSecondValue = Config.Bind("Synced", "MacroInstructionsPerSecond", DefaultMacroInstructionsPerSecond, "The number of macro instructions that can be ran per second, ranging from just above 0 to 100.").Value;
                if (Instance.MacroInstructionsPerSecondValue <= 0)
                {
                    Instance.MacroInstructionsPerSecondValue = 0.001f;
                }
                else if (Instance.MacroInstructionsPerSecondValue > 100)
                {
                    Instance.MacroInstructionsPerSecondValue = 100;
                }

                Instance.EnabledCommands = new();
                bool giveDescription = true;
                foreach (Command command in GetCommands())
                {
                    if (command is AlphanumericCodeCommand || command is CompatibilityCommand)
                    {
                        continue;
                    }
                    ConfigEntry<bool> enabled;
                    if (giveDescription)
                    {
                        enabled = Config.Bind("Synced - Enabled Commands", command.id + "_Enabled", command.enabled, "Whether a command should be enabled or disabled.");
                        giveDescription = false;
                    }
                    else
                    {
                        enabled = Config.Bind("Synced - Enabled Commands", command.id + "_Enabled", command.enabled);
                    }
                    Instance.EnabledCommands.Add(command.id, enabled.Value);
                }

                Instance.MoonPriceMultipliers = new();
                giveDescription = true;
                foreach (string moonId in moons.Keys)
                {
                    ConfigEntry<float> priceMultiplier;
                    if (giveDescription)
                    {
                        priceMultiplier = Config.Bind("Synced - Moon Price Multipliers", moonId + "_PriceMultiplier", -1f, "If a moon's default price is 0, the multiplier will override the actual price of the moon, otherwise the multiplier will multiply the moon's default price. A price multiplier of -1 will count as the multiplier being disabled.");
                        giveDescription = false;
                    }
                    else
                    {
                        priceMultiplier = Config.Bind("Synced - Moon Price Multipliers", moonId + "_PriceMultiplier", -1f);
                    }
                    Instance.MoonPriceMultipliers.Add(moonId, priceMultiplier.Value >= 0 || priceMultiplier.Value == -1 ? priceMultiplier.Value : 0);
                }

                Instance.ItemPriceMultipliers = new();
                giveDescription = true;
                foreach (string itemId in purchasableItems.Keys)
                {
                    ConfigEntry<float> priceMultiplier;
                    if (giveDescription)
                    {
                        priceMultiplier = Config.Bind("Synced - Item Price Multipliers", itemId + "_PriceMultiplier", -1f, "If an item's default price is 0, the multiplier will override the actual price of the item, otherwise the multiplier will multiply the item's default price. A price multiplier of -1 will count as the multiplier being disabled.");
                        giveDescription = false;
                    }
                    else
                    {
                        priceMultiplier = Config.Bind("Synced - Item Price Multipliers", itemId + "_PriceMultiplier", -1f);
                    }
                    Instance.ItemPriceMultipliers.Add(itemId, priceMultiplier.Value >= 0 || priceMultiplier.Value == -1 ? priceMultiplier.Value : 0);
                }

                Instance.UnlockablePriceMultipliers = new();
                giveDescription = true;
                foreach (string unlockableId in purchasableUnlockables.Keys)
                {
                    ConfigEntry<float> priceMultiplier;
                    if (giveDescription)
                    {
                        priceMultiplier = Config.Bind("Synced - Unlockable Price Multipliers", unlockableId + "_PriceMultiplier", -1f, "If an unlockable's default price is 0, the multiplier will override the actual price of the unlockable, otherwise the multiplier will multiply the unlockable's default price. A price multiplier of -1 will count as the multiplier being disabled.");
                        giveDescription = false;
                    }
                    else
                    {
                        priceMultiplier = Config.Bind("Synced - Unlockable Price Multipliers", unlockableId + "_PriceMultiplier", -1f);
                    }
                    Instance.UnlockablePriceMultipliers.Add(unlockableId, priceMultiplier.Value >= 0 || priceMultiplier.Value == -1 ? priceMultiplier.Value : 0);
                }

                Config.Save();

                foreach (ulong clientId in StartOfRound.Instance.ClientPlayerList.Keys)
                {
                    SyncWithClient(clientId);
                }
            }
        }

        internal static void SetupSyncedConfig()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("lammOS_OnRequestConfigSync", OnRequestSync);
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("lammOS_OnNotifyVersionMismatch", OnNotifyVersionMismatch);
                return;
            }

            Instance.MaxDropshipItemsValue = DefaultMaxDropshipItems;
            Instance.MacroInstructionsPerSecondValue = DefaultMacroInstructionsPerSecond;
            Instance.EnabledCommands = new();
            Instance.MoonPriceMultipliers = new();
            Instance.ItemPriceMultipliers = new();
            Instance.UnlockablePriceMultipliers = new();

            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("lammOS_OnReceiveConfigSync", OnReceiveSync);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("lammOS_OnRequestConfigSync", 0uL, new(4, Allocator.Temp));
        }

        internal static void OnRequestSync(ulong clientId, FastBufferReader _)
        {
            SyncWithClient(clientId);
        }
        internal static void SyncWithClient(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsHost) return;

            byte[] array = SerializeToBytes(Instance);
            int value = array.Length;

            FastBufferWriter stream = new(array.Length + 4, Allocator.Temp);
            try
            {
                stream.WriteValueSafe(in value, default);
                stream.WriteBytesSafe(array);

                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("lammOS_OnReceiveConfigSync", clientId, stream, NetworkDelivery.ReliableFragmentedSequenced);
            }
            catch (Exception e)
            {
                Logger.LogError($"Error occurred syncing config with client: {clientId}\n{e}");
            }
        }
        internal static void OnReceiveSync(ulong _, FastBufferReader reader)
        {
            if (!reader.TryBeginRead(4))
            {
                Logger.LogError("Config sync error: Could not begin reading buffer.");
                return;
            }

            reader.ReadValueSafe(out int val, default);
            if (!reader.TryBeginRead(val))
            {
                Logger.LogError("Config sync error: Host could not sync.");
                return;
            }

            byte[] data = new byte[val];
            reader.ReadBytesSafe(ref data, val);

            SyncWithHost(DeserializeFromBytes<SyncedConfig>(data));
        }
        internal static void SyncWithHost(SyncedConfig newConfig)
        {
            if (Instance.ModVersion != newConfig.ModVersion)
            {
                Logger.LogWarning("The host is using a different version of the mod than you are, this may lead to certain synced options not syncing properly if the synced config was changed between versions! Your version: " + Instance.ModVersion + "   Host's version: " + newConfig.ModVersion);


                byte[] array = SerializeToBytes(Instance.ModVersion);
                int value = array.Length;

                FastBufferWriter stream = new(array.Length + 4, Allocator.Temp);
                try
                {
                    stream.WriteValueSafe(in value, default);
                    stream.WriteBytesSafe(array);

                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("lammOS_OnNotifyVersionMismatch", 0ul, stream);
                }
                catch (Exception e)
                {
                    Logger.LogError($"Error occurred sending version to host: {e}");
                }
            }

            Instance.MaxDropshipItemsValue = newConfig.MaxDropshipItemsValue;
            Instance.MacroInstructionsPerSecondValue = newConfig.MacroInstructionsPerSecondValue;
            Instance.EnabledCommands = newConfig.EnabledCommands;
            Instance.MoonPriceMultipliers = newConfig.MoonPriceMultipliers;
            Instance.ItemPriceMultipliers = newConfig.ItemPriceMultipliers;
            Instance.UnlockablePriceMultipliers = newConfig.UnlockablePriceMultipliers;
        }

        internal static void OnNotifyVersionMismatch(ulong clientId, FastBufferReader reader)
        {
            if (!reader.TryBeginRead(4))
            {
                Logger.LogError("Version notification error: Could not begin reading buffer.");
                return;
            }

            reader.ReadValueSafe(out int val, default);
            if (!reader.TryBeginRead(val))
            {
                Logger.LogError("Version notification error: Could not read version.");
                return;
            }

            byte[] data = new byte[val];
            reader.ReadBytesSafe(ref data, val);

            BinaryFormatter bf = new();
            using MemoryStream stream = new(data);

            string clientVersion = DeserializeFromBytes<string>(data);
            string name = "Unknown";
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player.actualClientId == clientId)
                {
                    name = player.usernameBillboardText.text;
                    break;
                }
            }

            Logger.LogWarning("The client '" + name + "' (" + clientId.ToString() + ") has a different version than your own, this may lead to certain synced options not syncing properly if the synced config was changed between versions! Your version: " + Instance.ModVersion + "   Client's version: " + clientVersion);
        }

        internal static byte[] SerializeToBytes<T>(T val)
        {
            BinaryFormatter bf = new();
            using MemoryStream stream = new();

            try
            {
                bf.Serialize(stream, val);
                return stream.ToArray();
            }
            catch (Exception e)
            {
                Logger.LogError($"Error serializing to bytes: {e}");
                return null;
            }
        }
        internal static T DeserializeFromBytes<T>(byte[] data)
        {
            BinaryFormatter bf = new();
            using MemoryStream stream = new(data);

            try
            {
                return (T)bf.Deserialize(stream);
            }
            catch (Exception e)
            {
                Logger.LogError($"Error deserializing from bytes: {e}");
                return default;
            }
        }
    }
}