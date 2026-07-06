using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MorserinoWinKeyboard
{
    /// <summary>
    /// Stores the 16 quick-macro button labels in a small JSON file that
    /// sits next to the .exe -- the Windows counterpart of the Mac app's
    /// sibling QuickMacroButtons.plist (see AppDelegate.m's
    /// quickMacroPlistPath/quickMacroTitles). A file next to the executable
    /// is easy to inspect/back up by hand and isn't tied to any
    /// per-machine registry state, so it travels with the app if the whole
    /// install folder is copied elsewhere.
    ///
    /// There's no NSUserDefaults-style legacy store to migrate from here --
    /// this is a new app -- so unlike the Mac version there's no migration
    /// step, just a plain load-or-create.
    /// </summary>
    internal sealed class QuickMacroStore
    {
        private readonly string _path;
        private Dictionary<string, string> _titles;

        public QuickMacroStore()
        {
            _path = Path.Combine(AppContext.BaseDirectory, "QuickMacroButtons.json");
        }

        private Dictionary<string, string> Titles => _titles ??= Load();

        private Dictionary<string, string> Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    string json = File.ReadAllText(_path);
                    Dictionary<string, string> loaded =
                        JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (loaded != null)
                    {
                        return loaded;
                    }
                }
            }
            catch
            {
                // Corrupt or unreadable file -- fall back to an empty store
                // rather than crashing the app over saved button labels.
            }
            return new Dictionary<string, string>();
        }

        public string GetTitle(int tag)
        {
            Titles.TryGetValue(KeyForTag(tag), out string value);
            return value;
        }

        /// <summary>
        /// Saves a button's label. Returns false if the write failed (e.g.
        /// the install folder is read-only), so the caller can decide how
        /// to surface that -- the Mac app falls back to NSUserDefaults in
        /// this case; there's no equivalent fallback store on Windows, so
        /// MainForm just warns the user instead.
        /// </summary>
        public bool SetTitle(int tag, string value)
        {
            Titles[KeyForTag(tag)] = value;
            return Save();
        }

        private bool Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(
                    Titles, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_path, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string KeyForTag(int tag) => "QuickMacro" + tag;
    }
}
