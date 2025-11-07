using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace PlasteelSoul;

[BepInAutoPlugin]
[BepInDependency("org.silksong-modding.i18n")]
sealed partial class Mod : BaseUnityPlugin
{
    void Awake()
    {
        Mod._instance = this;
        this._config = new(base.Config);
        new Harmony(Mod.Id).PatchAll();
    }

    static Mod? _instance = null;
    static Mod Instance => Mod._instance ? Mod._instance :
        throw new NullReferenceException($"{nameof(Mod)} accessed before {nameof(Awake)}");

    ConfigEntries? _config = null;
    new internal static ConfigEntries Config => Mod.Instance._config!;

    internal sealed class ConfigEntries(ConfigFile file)
    {
        internal bool AccessSteelWish => this._accessSteelWish.Value;
        ConfigEntry<bool> _accessSteelWish = file.Bind(
            "General", "AccessSteelWish", true,
            "Allow access to the Steel Soul-exclusive wish in non-Steel Soul mode"
        );

        internal bool SellShellSatchel => this._sellShellSatchel.Value;
        ConfigEntry<bool> _sellShellSatchel = file.Bind(
            "General", "SellShellSatchel", true,
            "Allow purchasing the Shell Satchel from Jubilana in non-Steel Soul mode"
        );

        internal bool AccessSkynx => this._accessSkynx.Value;
        ConfigEntry<bool> _accessSkynx = file.Bind(
            "General", "AccessSkynx", true,
            "Allow access to Skynx to sell Silkeaters in non-Steel Soul mode"
        );

        internal bool PaintBellhomeChrome => this._paintBellhomeChrome.Value;
        ConfigEntry<bool> _paintBellhomeChrome = file.Bind(
            "General", "PaintBellhomeChrome", true,
            "Allow painting the Bellhome with the Chrome Bell Lacquer in non-Steel Soul mode"
        );
    }

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
                Mod.Config.AccessSteelWish
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
        item.name = $"Mods.{Mod.Id}.{nameof(JubilanaShopShellSatchel)}";
        item.displayName = new("Tools", "SHELL_SATCHEL_NAME");
        item.description = new($"Mods.{Mod.Id}", "JUBILANA_SHOP_SHELL_SATCHEL_DESC");
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
                !Mod.Config.SellShellSatchel
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
                Mod.Config.AccessSkynx
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
                Mod.Config.PaintBellhomeChrome &&
                PaintBellhomeChrome.bellhomePaintConditions.TryGetValue(__instance, out _)
            ) __result = true;
        }
    }
}
