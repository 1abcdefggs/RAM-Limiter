using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RAMLIMITER
{
    class Config
    {
        public const int DefaultIntervalMs = 5000;

        public string[] Processes { get; set; }
        public int IntervalMs { get; set; } = DefaultIntervalMs;
        public bool AutoStart { get; set; }
        public bool Tray { get; set; }

        // "a, b,,c" -> a, b, c
        public static string[] SplitNames(string list) =>
            list.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
    }

    static class ConfigLoader
    {
        internal static Config LoadConfig(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                var text = File.ReadAllText(path);
                var cfg = new Config();

                // crude JSON parsing: processes array
                var procList = TryGetJsonArray(text, "processes");
                if (procList != null) cfg.Processes = procList.ToArray();

                var interval = TryGetJsonNumber(text, "intervalMs");
                if (interval.HasValue) cfg.IntervalMs = (int)interval.Value;

                var autostart = TryGetJsonBool(text, "autostart");
                if (autostart.HasValue) cfg.AutoStart = autostart.Value;

                return cfg;
            }
            catch
            {
                return null;
            }
        }

        static int KeyIndex(string text, string key) =>
            text.IndexOf('"' + key + '"', StringComparison.OrdinalIgnoreCase);

        // index just after the ':' that follows the key, or -1
        static int ColonEnd(string text, string key)
        {
            var idx = KeyIndex(text, key);
            if (idx == -1) return -1;
            var colon = text.IndexOf(':', idx);
            return colon == -1 ? -1 : colon + 1;
        }

        static List<string> TryGetJsonArray(string text, string key)
        {
            try
            {
                var idx = KeyIndex(text, key);
                if (idx == -1) return null;
                var start = text.IndexOf('[', idx);
                if (start == -1) return null;
                var end = text.IndexOf(']', start);
                if (end == -1) return null;
                var inside = text.Substring(start + 1, end - start - 1);
                var items = new List<string>();
                int i = 0;
                while (i < inside.Length)
                {
                    // find next quoted string
                    while (i < inside.Length && inside[i] != '"') i++;
                    if (i >= inside.Length) break;
                    int s = i + 1;
                    int e = inside.IndexOf('"', s);
                    if (e == -1) break;
                    var item = inside.Substring(s, e - s).Trim();
                    if (!string.IsNullOrEmpty(item)) items.Add(item);
                    i = e + 1;
                }
                return items;
            }
            catch { return null; }
        }

        static double? TryGetJsonNumber(string text, string key)
        {
            try
            {
                int i = ColonEnd(text, key);
                if (i == -1) return null;
                while (i < text.Length && (char.IsWhiteSpace(text[i]) || text[i] == '\"')) i++;
                int j = i;
                while (j < text.Length && (char.IsDigit(text[j]) || text[j] == '.' || text[j] == '-')) j++;
                var numS = text.Substring(i, j - i);
                if (double.TryParse(numS, out var v)) return v;
                return null;
            }
            catch { return null; }
        }

        static bool? TryGetJsonBool(string text, string key)
        {
            try
            {
                int i = ColonEnd(text, key);
                if (i == -1) return null;
                while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
                var rest = text.Substring(i);
                if (rest.StartsWith("true", StringComparison.OrdinalIgnoreCase)) return true;
                if (rest.StartsWith("false", StringComparison.OrdinalIgnoreCase)) return false;
                return null;
            }
            catch { return null; }
        }
    }
}
