using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FikaDynamicAI.Patches;
using FikaDynamicAI.Scripts;
using System;

namespace FikaDynamicAI;

[BepInPlugin("com.lacyway.fda", "FikaDynamicAI", "1.1.0")]
[BepInDependency("com.fika.core", BepInDependency.DependencyFlags.HardDependency)]
internal class FikaDynamicAI_Plugin : BaseUnityPlugin
{
    internal static ManualLogSource PluginLogger;

    // General Settings
    public static ConfigEntry<float> DynamicAIRange { get; set; }
    public static ConfigEntry<EDynamicAIRates> DynamicAIRate { get; set; }
    public static ConfigEntry<bool> UseMapSpecificDistances { get; set; }
    public static ConfigEntry<bool> ShowDebugInfo { get; set; }

    // Bot Type Filters - which types WILL be affected by Dynamic AI
    public static ConfigEntry<bool> AffectScavs { get; set; }
    public static ConfigEntry<bool> AffectPMCs { get; set; }
    public static ConfigEntry<bool> AffectRogues { get; set; }
    public static ConfigEntry<bool> AffectRaiders { get; set; }
    public static ConfigEntry<bool> AffectCultists { get; set; }
    public static ConfigEntry<bool> AffectBosses { get; set; }
    public static ConfigEntry<bool> AffectSnipers { get; set; }
    public static ConfigEntry<bool> AffectFollowers { get; set; }

    // Map Settings
    public static ConfigEntry<bool> EnableFactory { get; set; }
    public static ConfigEntry<bool> EnableCustoms { get; set; }
    public static ConfigEntry<bool> EnableWoods { get; set; }
    public static ConfigEntry<bool> EnableShoreline { get; set; }
    public static ConfigEntry<bool> EnableInterchange { get; set; }
    public static ConfigEntry<bool> EnableReserve { get; set; }
    public static ConfigEntry<bool> EnableLighthouse { get; set; }
    public static ConfigEntry<bool> EnableStreets { get; set; }
    public static ConfigEntry<bool> EnableGroundZero { get; set; }
    public static ConfigEntry<bool> EnableLabs { get; set; }

    // Map Range Settings
    public static ConfigEntry<float> RangeFactory { get; set; }
    public static ConfigEntry<float> RangeCustoms { get; set; }
    public static ConfigEntry<float> RangeWoods { get; set; }
    public static ConfigEntry<float> RangeShoreline { get; set; }
    public static ConfigEntry<float> RangeInterchange { get; set; }
    public static ConfigEntry<float> RangeReserve { get; set; }
    public static ConfigEntry<float> RangeLighthouse { get; set; }
    public static ConfigEntry<float> RangeStreets { get; set; }
    public static ConfigEntry<float> RangeGroundZero { get; set; }
    public static ConfigEntry<float> RangeLabs { get; set; }

    internal static void DynamicAIRate_SettingChanged(object sender, EventArgs e)
    {
        if (FikaDynamicAIManager.Instance != null)
        {
            FikaDynamicAIManager.Instance.RateChanged(DynamicAIRate.Value);
        }
    }

    internal static void BotTypeFilter_SettingChanged(object sender, EventArgs e)
    {
        if (FikaDynamicAIManager.Instance != null)
        {
            FikaDynamicAIManager.Instance.RefreshBotTracking();
        }
    }

    protected void Awake()
    {
        PluginLogger = Logger;
        PluginLogger.LogInfo($"{nameof(FikaDynamicAI_Plugin)} v1.1.0 has been loaded.");

        const string generalHeader = "1. General";
        const string botTypesHeader = "2. Bot Types to Affect";

        // General Settings
        DynamicAIRange = Config.Bind(generalHeader, "Dynamic AI Range", 100f,
            new ConfigDescription("The global fallback range at which AI will be disabled.\n- Used if 'Use Map Specific Distances' is FALSE.\n- Used if the current map has no specific config defined.",
            new AcceptableValueRange<float>(50f, 1000f)));
        DynamicAIRate = Config.Bind(generalHeader, "Dynamic AI Rate", EDynamicAIRates.Medium,
            new ConfigDescription("How often DynamicAI checks bot distances.\nActs as a multiplier for the Smart LOD system (Low = slower checks, High = faster checks)."));
        UseMapSpecificDistances = Config.Bind(generalHeader, "Use Map Specific Distances", true,
            new ConfigDescription("If TRUE, the mod uses the custom ranges defined in '4. Map Distances'.\nIf FALSE, it ignores map settings and uses 'Dynamic AI Range' for all maps."));
        ShowDebugInfo = Config.Bind(generalHeader, "Show Debug Info", false,
            new ConfigDescription("If enabled, shows real-time logs in the BepInEx console:\n- Distance to player\n- Current update interval (Smart LOD)\nUseful for testing performance."));

        // Bot Type Filters
        AffectScavs = Config.Bind(botTypesHeader, "Affect Scavs", true,
            new ConfigDescription("Whether Dynamic AI should affect regular Scavs."));
        AffectPMCs = Config.Bind(botTypesHeader, "Affect PMCs", false,
            new ConfigDescription("Whether Dynamic AI should affect PMC bots. Disable to let PMCs roam freely."));
        AffectRogues = Config.Bind(botTypesHeader, "Affect Rogues", false,
            new ConfigDescription("Whether Dynamic AI should affect Rogues (exUsec) at Lighthouse."));
        AffectRaiders = Config.Bind(botTypesHeader, "Affect Raiders", true,
            new ConfigDescription("Whether Dynamic AI should affect Raiders."));
        AffectCultists = Config.Bind(botTypesHeader, "Affect Cultists", true,
            new ConfigDescription("Whether Dynamic AI should affect Cultists."));
        AffectBosses = Config.Bind(botTypesHeader, "Affect Bosses", false,
            new ConfigDescription("Whether Dynamic AI should affect Boss characters."));
        AffectSnipers = Config.Bind(botTypesHeader, "Affect Snipers", false,
            new ConfigDescription("Whether Dynamic AI should affect Sniper Scavs (marksman)."));
        AffectFollowers = Config.Bind(botTypesHeader, "Affect Followers", true,
            new ConfigDescription("Whether Dynamic AI should affect Boss followers/guards."));

        // Subscribe to setting changes for live updates
        AffectScavs.SettingChanged += BotTypeFilter_SettingChanged;
        AffectPMCs.SettingChanged += BotTypeFilter_SettingChanged;
        AffectRogues.SettingChanged += BotTypeFilter_SettingChanged;
        AffectRaiders.SettingChanged += BotTypeFilter_SettingChanged;
        AffectCultists.SettingChanged += BotTypeFilter_SettingChanged;
        AffectBosses.SettingChanged += BotTypeFilter_SettingChanged;
        AffectSnipers.SettingChanged += BotTypeFilter_SettingChanged;
        AffectFollowers.SettingChanged += BotTypeFilter_SettingChanged;

        // Map Settings
        const string mapHeader = "3. Map Filtering";
        EnableFactory = Config.Bind(mapHeader, "Factory", true, "Enable Dynamic AI on Factory");
        EnableCustoms = Config.Bind(mapHeader, "Customs", true, "Enable Dynamic AI on Customs");
        EnableWoods = Config.Bind(mapHeader, "Woods", true, "Enable Dynamic AI on Woods");
        EnableShoreline = Config.Bind(mapHeader, "Shoreline", true, "Enable Dynamic AI on Shoreline");
        EnableInterchange = Config.Bind(mapHeader, "Interchange", true, "Enable Dynamic AI on Interchange");
        EnableReserve = Config.Bind(mapHeader, "Reserve", true, "Enable Dynamic AI on Reserve");
        EnableLighthouse = Config.Bind(mapHeader, "Lighthouse", true, "Enable Dynamic AI on Lighthouse");
        EnableStreets = Config.Bind(mapHeader, "Streets", true, "Enable Dynamic AI on Streets");
        EnableGroundZero = Config.Bind(mapHeader, "Ground Zero", true, "Enable Dynamic AI on Ground Zero");
        EnableLabs = Config.Bind(mapHeader, "Labs", true, "Enable Dynamic AI on Labs");

        // Map Range Settings
        const string rangeHeader = "4. Map Distances";
        RangeFactory = Config.Bind(rangeHeader, "Factory", 80f, "Enable range for Factory");
        RangeLabs = Config.Bind(rangeHeader, "Labs", 120f, "Enable range for Labs");
        RangeStreets = Config.Bind(rangeHeader, "Streets", 150f, "Enable range for Streets of Tarkov");
        RangeGroundZero = Config.Bind(rangeHeader, "Ground Zero", 150f, "Enable range for Ground Zero");
        RangeCustoms = Config.Bind(rangeHeader, "Customs", 180f, "Enable range for Customs");
        RangeInterchange = Config.Bind(rangeHeader, "Interchange", 200f, "Enable range for Interchange");
        RangeReserve = Config.Bind(rangeHeader, "Reserve", 250f, "Enable range for Reserve");
        RangeShoreline = Config.Bind(rangeHeader, "Shoreline", 280f, "Enable range for Shoreline");
        RangeWoods = Config.Bind(rangeHeader, "Woods", 350f, "Enable range for Woods");
        RangeLighthouse = Config.Bind(rangeHeader, "Lighthouse", 400f, "Enable range for Lighthouse");

        // Map Range Updates
        RangeFactory.SettingChanged += BotTypeFilter_SettingChanged;
        RangeLabs.SettingChanged += BotTypeFilter_SettingChanged;
        RangeStreets.SettingChanged += BotTypeFilter_SettingChanged;
        RangeGroundZero.SettingChanged += BotTypeFilter_SettingChanged;
        RangeCustoms.SettingChanged += BotTypeFilter_SettingChanged;
        RangeInterchange.SettingChanged += BotTypeFilter_SettingChanged;
        RangeReserve.SettingChanged += BotTypeFilter_SettingChanged;
        RangeShoreline.SettingChanged += BotTypeFilter_SettingChanged;
        RangeWoods.SettingChanged += BotTypeFilter_SettingChanged;
        RangeLighthouse.SettingChanged += BotTypeFilter_SettingChanged;

        new BotsController_SetSettings_Postfix().Enable();
        new BotsEventsController_SpawnAction_Postfix().Enable();
        new HostGameController_StopBotsSystem_Postfix().Enable();
    }

    public enum EDynamicAIRates
    {
        Low,
        Medium,
        High
    }
}
