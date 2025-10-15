using System.Text;
using Newtonsoft.Json;
using TeamCherry.Localization;
using HarmonyLib;

namespace PlasteelSoul;

static class I18n
{
    internal static LocalisedString Key(string key) => new($"Mods.{PluginInfo.PLUGIN_GUID}", key);

    internal static void PatchOnSceneLoad(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(Language), nameof(Language.DoSwitch)),
            postfix: new HarmonyMethod(typeof(I18n), nameof(I18n.PostfixSwitchLanguage))
        );

        Language.SwitchLanguage(Language.CurrentLanguage());
    }

    static void PostfixSwitchLanguage()
    {
        var lang = Language._currentLanguage;
        var sheet = I18n.LoadSheet(lang);
        if (sheet is not null)
        {
            Language._currentEntrySheets[$"Mods.{PluginInfo.PLUGIN_GUID}"] = sheet;
            Mod.LogDebug($"injected sheet Mods.{PluginInfo.PLUGIN_GUID} for language {lang}");
        }
    }

    static Dictionary<string, string>? LoadSheet(LanguageCode lang)
    {
        var path = Path.Combine(
            Path.GetDirectoryName(typeof(Mod).Assembly.Location),
            "languages",
            $"{lang.ToString().ToLower()}.json"
        );

        try
        {
            using var s = new StreamReader(
                File.OpenRead(path),
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false
            );

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(s.ReadToEnd());
        }
        catch (Exception ex)
        {
            Mod.LogWarning($"unable to read i18n file for language {lang}: {path}\n{ex}");
            return null;
        }
    }
}
