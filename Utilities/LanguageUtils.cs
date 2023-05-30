using Terraria.Localization;

namespace tsorcRevamp.Utilities
{
    public static class LanguageUtils
    {
        public static string GetTextValue(string key)
        {
            return Language.GetTextValue("Mods.tsorcRevamp." + key);
        }

        public static string GetTextValue(string key, params object[] args)
        {
            return Language.GetTextValue("Mods.tsorcRevamp." + key, args);
        }
    }
}
