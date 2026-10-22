using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RAMLIMITER
{
    // largest: the biggest instance of each app every tick (original behavior)
    // all: every instance every tick
    // idle: each instance once, after it has been idle for IdleIntervals ticks
    enum TrimMode { Largest, All, Idle }

    class Config
    {
        public const int DefaultIntervalMs = 5000;
        public const int DefaultIdleIntervals = 3;

        public string[] Processes { get; set; }
        public int IntervalMs { get; set; } = DefaultIntervalMs;
        public bool AutoStart { get; set; }
        public bool Tray { get; set; }
        // null = not given; the effective defaults are Largest and DefaultIdleIntervals
        public TrimMode? Mode { get; set; }
        public int? IdleIntervals { get; set; }

        public static TrimMode? ParseMode(string s) =>
            Enum.TryParse(s, true, out TrimMode m) && Enum.IsDefined(typeof(TrimMode), m) ? m : (TrimMode?)null;
        // "a, b,,c" -> a, b, c
        public static string[] SplitNames(string list) =>
            list.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
    }

    static class ConfigLoader
    {
        internal static Config LoadConfig(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                // crude JSON parsing: only the known keys are read
                var text = File.ReadAllText(path);
                var cfg = new Config { Processes = TryGetJsonArray(text, "processes") };
                var interval = TryGetJsonNumber(text, "intervalMs");
                if (interval.HasValue) cfg.IntervalMs = (int)interval.Value;
                cfg.AutoStart = TryGetJsonBool(text, "autostart") ?? false;
                var mode = TryGetJsonString(text, "mode");
                if (mode != null) cfg.Mode = Config.ParseMode(mode);
                var idle = TryGetJsonNumber(text, "idleIntervals");
                if (idle.HasValue && idle.Value >= 1) cfg.IdleIntervals = (int)idle.Value;
                return cfg;
            }
            catch { return null; }
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

        // The quoted, non-blank strings inside the [...] after the key. Bounds are checked
        // before every Substring, so these readers do not throw; LoadConfig still catches.
        static string[] TryGetJsonArray(string text, string key)
        {
            var idx = KeyIndex(text, key);
            if (idx == -1) return null;
            var start = text.IndexOf('[', idx);
            if (start == -1) return null;
            var end = text.IndexOf(']', start);
            if (end == -1) return null;
            var items = new List<string>();
            for (int i = text.IndexOf('"', start); i != -1 && i < end; i = text.IndexOf('"', i))
            {
                int e = text.IndexOf('"', i + 1);
                if (e == -1 || e > end) break;
                var item = text.Substring(i + 1, e - i - 1).Trim();
                if (item.Length > 0) items.Add(item);
                i = e + 1;
            }
            return items.ToArray();
        }

        static double? TryGetJsonNumber(string text, string key)
        {
            int i = ColonEnd(text, key);
            if (i == -1) return null;
            while (i < text.Length && (char.IsWhiteSpace(text[i]) || text[i] == '"')) i++;
            int j = i;
            while (j < text.Length && (char.IsDigit(text[j]) || text[j] == '.' || text[j] == '-')) j++;
            return double.TryParse(text.Substring(i, j - i), out var v) ? v : (double?)null;
        }

        static string TryGetJsonString(string text, string key)
        {
            int i = ColonEnd(text, key);
            if (i == -1) return null;
            while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            if (i >= text.Length || text[i] != '"') return null;
            int e = text.IndexOf('"', i + 1);
            return e == -1 ? null : text.Substring(i + 1, e - i - 1);
        }

        static bool? TryGetJsonBool(string text, string key)
        {
            int i = ColonEnd(text, key);
            if (i == -1) return null;
            while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            if (string.Compare(text, i, "true", 0, 4, StringComparison.OrdinalIgnoreCase) == 0) return true;
            if (string.Compare(text, i, "false", 0, 5, StringComparison.OrdinalIgnoreCase) == 0) return false;
            return null;
        }
    }
}
