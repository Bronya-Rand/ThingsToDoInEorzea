using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Dalamud.Game;

namespace ThingsToDoInEorzea.Services
{
    /// <summary>
    /// Provides localization services for retrieving localized strings based on the client's language settings.
    /// </summary>
    public sealed class LocalizationService
    {
        private readonly Dictionary<ClientLanguage, Dictionary<string, string>> catalogs = [];

        public LocalizationService()
        {
            LoadEmbeddedLocales();
        }

        private void LoadEmbeddedLocales()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var languageMap = new (ClientLanguage Lang, string ResourceName)[]
            {
                (ClientLanguage.English, "ThingsToDoInEorzea.Resources.loc.en.json"),
                (ClientLanguage.French, "ThingsToDoInEorzea.Resources.loc.fr.json"),
                (ClientLanguage.German, "ThingsToDoInEorzea.Resources.loc.de.json"),
                (ClientLanguage.Japanese, "ThingsToDoInEorzea.Resources.loc.ja.json"),
            };

            foreach (var (lang, resName) in languageMap)
            {
                var dict = LoadResourceJson(assembly, resName);
                catalogs[lang] = dict;
            }
        }

        private static Dictionary<string, string> LoadResourceJson(Assembly assembly, string resourceName)
        {
            try
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                return parsed != null
                           ? new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase)
                           : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public string GetMacro(string key, ClientLanguage language = ClientLanguage.English)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            // 1. Check requested language catalog
            if (catalogs.TryGetValue(language, out var langDict) &&
                langDict.TryGetValue(key, out var macro) &&
                !string.IsNullOrWhiteSpace(macro))
            {
                return macro;
            }

            // 2. Fallback to English catalog
            if (language != ClientLanguage.English &&
                catalogs.TryGetValue(ClientLanguage.English, out var enDict) &&
                enDict.TryGetValue(key, out var enMacro) &&
                !string.IsNullOrWhiteSpace(enMacro))
            {
                return enMacro;
            }

            // 3. Fallback to key
            return key;
        }

        public string GetString(string key, ClientLanguage? language = null, params object[] args)
        {
            var activeLanguage = language ?? GetActiveLanguage();
            var template = GetMacro(key, activeLanguage);
            if (args.Length > 0)
            {
                try
                {
                    return string.Format(template, args);
                }
                catch
                {
                    return template;
                }
            }

            return template;
        }

        public string T(string key, params object[] args) => GetString(key, null, args);

        public static ClientLanguage GetActiveLanguage()
        {
            try
            {
                return DalamudServices.ClientState?.ClientLanguage ?? ClientLanguage.English;
            }
            catch
            {
                return ClientLanguage.English;
            }
        }
    }
}
