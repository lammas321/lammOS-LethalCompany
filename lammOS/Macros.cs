using BepInEx;
using System.Collections.Generic;
using System.IO;

namespace lammOS.Macros
{
    public static class Macros
    {
        internal static Dictionary<string, List<string>> macros = new();

        public static bool AddMacro(string id, List<string> inputs)
        {
            if (HasMacro(id))
            {
                return false;
            }
            macros.Add(id, new List<string>(inputs));
            return true;
        }
        public static bool HasMacro(string id)
        {
            return macros.ContainsKey(id);
        }
        public static List<string> GetMacro(string id)
        {
            if (!HasMacro(id))
            {
                return null;
            }
            return new List<string>(macros[id]);
        }
        public static List<string> GetMacroIds()
        {
            return new List<string>(macros.Keys);
        }
        public static bool ModifyMacro(string id, List<string> inputs)
        {
            if (!HasMacro(id))
            {
                return false;
            }
            macros[id] = new List<string>(inputs);
            return true;
        }
        public static bool RemoveMacro(string id)
        {
            if (!HasMacro(id))
            {
                return false;
            }
            macros.Remove(id);
            return true;
        }

        public static void Save()
        {
            List<string> data = new();
            foreach (string macroId in macros.Keys)
            {
                data.Add("[" + macroId + "]");
                data.AddRange(macros[macroId]);
            }

            File.WriteAllText(Utility.CombinePaths(Paths.ConfigPath, "lammas123.lammOS.Macros.txt"), string.Join('\n', data));
        }
        public static void Load()
        {
            if (File.Exists(Utility.CombinePaths(Paths.ConfigPath, "lammas123.lammOS.Macros.es3")))
            {
                macros = ES3.Load<Dictionary<string, List<string>>>("lammOS_Macros", Utility.CombinePaths(Paths.ConfigPath, "lammas123.lammOS.Macros.es3"));
                File.Delete(Utility.CombinePaths(Paths.ConfigPath, "lammas123.lammOS.Macros.es3"));
                Save();
                return;
            }
            else if(File.Exists(Utility.CombinePaths(Paths.ConfigPath, "lammas123.lammOS.Macros.txt")))
            {
                string[] data = File.ReadAllText(Utility.CombinePaths(Paths.ConfigPath, "lammas123.lammOS.Macros.txt")).Split('\n');
                string currentMacroId = null;
                macros = new();

                foreach (string parsed in data)
                {
                    string line = parsed.Trim();

                    if (line == "")
                    {
                        continue;
                    }

                    if (line.StartsWith('[') && line.EndsWith(']'))
                    {
                        string macroId = line.Substring(1, line.Length - 2).Trim();
                        if (macroId == "" || macros.ContainsKey(macroId))
                        {
                            continue;
                        }
                        if (currentMacroId != null && macros[currentMacroId].Count == 0)
                        {
                            macros.Remove(currentMacroId);
                        }
                        currentMacroId = macroId;
                        macros.Add(currentMacroId, new());
                        continue;
                    }

                    if (currentMacroId == null)
                    {
                        continue;
                    }

                    macros[currentMacroId].Add(line);
                }
                if (currentMacroId != null && macros[currentMacroId].Count == 0)
                {
                    macros.Remove(currentMacroId);
                }
                return;
            }

            macros = new();
            Save();
        }
    }
}