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
            ES3.Save("lammOS_Macros", macros, Utility.CombinePaths(Paths.ConfigPath, "lammas123.lammOS.Macros.es3"));
        }
        public static void Load()
        {
            if (File.Exists(Utility.CombinePaths(Paths.ConfigPath, "lammas123.lammOS.Macros.es3")))
            {
                macros = ES3.Load<Dictionary<string, List<string>>>("lammOS_Macros", Utility.CombinePaths(Paths.ConfigPath, "lammas123.lammOS.Macros.es3"));
                return;
            }

            macros = new();
            Save();
        }
    }
}