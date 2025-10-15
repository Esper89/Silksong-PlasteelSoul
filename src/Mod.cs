using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace PlasteelSoul;

[BepInPlugin(PluginInfo.PLUGIN_GUID, "Plasteel Soul", PluginInfo.PLUGIN_VERSION)]
class Mod : BaseUnityPlugin
{
    void Awake()
    {
        Mod._instance = this;

        this._accessSteelWish = base.Config.Bind(
            "General", "AccessSteelWish", true,
            "Allow access to the Steel Soul-exclusive wish in non-Steel Soul mode"
        );
        this._sellShellSatchel = base.Config.Bind(
            "General", "SellShellSatchel", true,
            "Allow purchasing the Shell Satchel from Jubilana in non-Steel Soul mode"
        );
        this._accessSkynx = base.Config.Bind(
            "General", "AccessSkynx", true,
            "Allow access to Skynx to sell Silkeaters in non-Steel Soul mode"
        );
        this._paintBellhomeChrome = base.Config.Bind(
            "General", "PaintBellhomeChrome", true,
            "Allow painting the Bellhome with the Chrome Bell Lacquer in non-Steel Soul mode"
        );

        var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();

        var sceneLoaded = false;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, _) =>
        {
            if (!sceneLoaded)
            {
                sceneLoaded = true;
                I18n.PatchOnSceneLoad(harmony);
            }
        };
    }

    static Mod? _instance = null;
    static Mod Instance => Mod._instance ??
        throw new NullReferenceException("Mod method called before Awake");

    ConfigEntry<bool>? _accessSteelWish = null;
    internal static bool AccessSteelWish => Mod.Instance._accessSteelWish!.Value;

    ConfigEntry<bool>? _sellShellSatchel = null;
    internal static bool SellShellSatchel => Mod.Instance._sellShellSatchel!.Value;

    ConfigEntry<bool>? _accessSkynx = null;
    internal static bool AccessSkynx => Mod.Instance._accessSkynx!.Value;

    ConfigEntry<bool>? _paintBellhomeChrome = null;
    internal static bool PaintBellhomeChrome => Mod.Instance._paintBellhomeChrome!.Value;

    internal static void LogDebug(object msg) => Mod.Instance.Logger.LogDebug(msg);
    internal static void LogInfo(object msg) => Mod.Instance.Logger.LogInfo(msg);
    internal static void LogMessage(object msg) => Mod.Instance.Logger.LogMessage(msg);
    internal static void LogWarning(object msg) => Mod.Instance.Logger.LogWarning(msg);
    internal static void LogError(object msg) => Mod.Instance.Logger.LogError(msg);
    internal static void LogFatal(object msg) => Mod.Instance.Logger.LogFatal(msg);
}

static class AccessSteelWish
{
    static int runningEvaluations = 0;

    [HarmonyPatch(typeof(TestGameObjectActivator), nameof(TestGameObjectActivator.Evaluate))]
    static class SetRunningEvaluationsInScene
    {
        static void Prefix(TestGameObjectActivator __instance, ref bool __state)
        {
            if (__instance.gameObject.scene.name == "Coral_37")
            {
                AccessSteelWish.runningEvaluations++;
                __state = true;
            }
            else __state = false;
        }

        static void Postfix(bool __state)
        {
            if (__state) AccessSteelWish.runningEvaluations--;
        }
    }

    [HarmonyPatch(typeof(PlayerDataTest.Test), nameof(PlayerDataTest.Test.IsFulfilled))]
    static class FakePermadeathEnabled
    {
        static void Postfix(PlayerDataTest.Test __instance, ref bool __result)
        {
            if (
                AccessSteelWish.runningEvaluations > 0 &&
                __instance.FieldName == "permadeathMode" &&
                Mod.AccessSteelWish
            )
            {
			    float val = __instance.IntValue;
			    __result = __instance.NumType switch
			    {
				    PlayerDataTest.NumTestType.Equal => 1f == val,
				    PlayerDataTest.NumTestType.NotEqual => 1f != val,
				    PlayerDataTest.NumTestType.LessThan => 1f < val,
				    PlayerDataTest.NumTestType.MoreThan => 1f > val,
				    _ => throw new ArgumentOutOfRangeException(),
			    };
            }
        }
    }
}

static class SellShellSatchel
{
    static ShopItem JubilanaShopShellSatchel = SellShellSatchel.CreateItem();

    static ShopItem CreateItem()
    {
        var item = (ShopItem)ScriptableObject.CreateInstance(typeof(ShopItem));
        item.name = $"Mods.{PluginInfo.PLUGIN_GUID}.{nameof(JubilanaShopShellSatchel)}";
        item.displayName = new("Tools", "SHELL_SATCHEL_NAME");
        item.description = I18n.Key("JUBILANA_SHOP_SHELL_SATCHEL_DESC");
        item.cost = 290;
        item.savedItem = GlobalSettings.Gameplay.ShellSatchelTool;
	    item.questsAppearConditions = [];
        item.extraAppearConditions = new()
        {
            TestGroups = [
                new()
                {
                    Tests = [
                        new()
                        {
                            Type = PlayerDataTest.TestType.Enum,
                            FieldName = "permadeathMode",
                            NumType = PlayerDataTest.NumTestType.Equal,
                            IntValue = (int)GlobalEnums.PermadeathModes.Off
                        }
                    ]
                }
            ]
        };
	    item.spawnOnPurchaseConditionals = [];
	    item.setExtraPlayerDataBools = [];
	    item.setExtraPlayerDataInts = [];
	    return item;
    }

    [HarmonyPatch(typeof(ShopItemList), nameof(ShopItemList.ShopItems), MethodType.Getter)]
    static class RegisterShopItem
    {
        static void Prefix(ShopItemList __instance)
        {
            if (__instance.name == "City Merchant Stock" && __instance.shopItems is not null)
            {
                if (!__instance.shopItems.Contains(SellShellSatchel.JubilanaShopShellSatchel))
                {
                    var list = __instance.shopItems.ToList();

                    int i;
                    for (i = 0; i < list.Count(); i++)
                    {
                        var slot = list[i];
                        if (slot && slot.name == "City Merchant Needolin Tool")
                        {
                            var quests = slot.questsAppearConditions;
                            SellShellSatchel.JubilanaShopShellSatchel.questsAppearConditions =
                                quests;
                            break;
                        }
                    }
                    i++;
                    if (i > list.Count()) i = 0;

                    list.Insert(
                        Math.Clamp(i, 0, list.Count()),
                        SellShellSatchel.JubilanaShopShellSatchel
                    );
                    __instance.shopItems = list.ToArray();
                }
            }
        }
    }

    [HarmonyPatch(typeof(ShopItem), nameof(ShopItem.IsAvailable), MethodType.Getter)]
    static class HideIfDisabled
    {
        static void Postfix(ShopItem __instance, ref bool __result)
        {
            if (
                __instance.name == SellShellSatchel.JubilanaShopShellSatchel.name &&
                !Mod.SellShellSatchel
            ) __result = false;
        }
    }
}

static class AccessSkynx
{
    [HarmonyPatch(typeof(TestGameObjectActivator), nameof(TestGameObjectActivator.Evaluate))]
    static class RearrangeStyxRoomHierarchy
    {
        static void Postfix(TestGameObjectActivator __instance)
        {
            if (
                __instance.gameObject.scene.name == "Dust_11" &&
                __instance.name == "Steel Soul States" &&
                Mod.AccessSkynx
            )
            {
                var steelSoulStates = __instance.transform;

                var regular = steelSoulStates.Find("Regular");
                if (regular)
                {
                    var rightWall = regular.Find("right_wall");
                    if (rightWall) rightWall.gameObject.SetActive(false);
                }

                var steelSoul = steelSoulStates.Find("Steel Soul");
                if (steelSoul)
                {
                    var breakableWall = steelSoul.Find("Breakable Wall");
                    if (breakableWall) breakableWall.parent = steelSoulStates;

                    var group = steelSoul.Find("Group");
                    if (group && group.Find("Grub Farmer Mimic")) group.parent = steelSoulStates;
                }
            }
        }
    }
}

static class PaintBellhomeChrome
{
    static object unit = true;
    static ConditionalWeakTable<PlayerDataTest, object> bellhomePaintConditions = new();

    [HarmonyPatch(typeof(ShopItem), nameof(ShopItem.Awake))]
    static class RegisterBellhomePaintConditions
    {
        static void Postfix(ShopItem __instance)
        {
            if (__instance.name == "Bellhart Furnishing Paint")
            {
                foreach (var subItem in __instance.subItems)
                {
                    PaintBellhomeChrome.bellhomePaintConditions.AddOrUpdate(
                        subItem.Condition,
                        PaintBellhomeChrome.unit
                    );
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerDataTest), nameof(PlayerDataTest.IsFulfilled), MethodType.Getter)]
    static class MakeAllBellhomeColorsAvailable
    {
        static void Postfix(PlayerDataTest __instance, ref bool __result)
        {
            if (
                Mod.PaintBellhomeChrome &&
                PaintBellhomeChrome.bellhomePaintConditions.TryGetValue(__instance, out _)
            ) __result = true;
        }
    }
}
