using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Nosebleed.Pancake.GameConfig;

namespace CardCrackThresholdMod
{
    [BepInPlugin("com.imoonday.cardcrackthresholdmod", "Card Crack Threshold Mod", "1.0.0")]
    public class CardCrackThresholdMod : BasePlugin
    {
        public static CardCrackThresholdMod Instance;

        private ConfigEntry<int> _timesPlayedToStartCracking;

        public override void Load()
        {
            Instance = this;

            _timesPlayedToStartCracking = Config.Bind(
                "Card Cracking",
                "TimesPlayedToStartCracking",
                999,
                "The same-turn play count at which the same card starts cracking."
            );

            ApplyCardCrackConfig();

            var harmony = new Harmony("com.imoonday.cardcrackthresholdmod");
            harmony.PatchAll();

            Log.LogInfo("Card Crack Threshold Mod loaded.");
        }

        public void ApplyCardCrackConfig()
        {
            int threshold = _timesPlayedToStartCracking.Value;

            if (threshold < 1)
                throw new ArgumentOutOfRangeException(nameof(threshold), "TimesPlayedToStartCracking must be at least 1.");

            GlobalConfig globalConfig = GlobalConfig.Instance;
            if (globalConfig == null)
            {
                Log.LogWarning("GlobalConfig.Instance is not ready yet; card cracking threshold will be applied when GlobalConfig enables.");
                return;
            }

            GlobalConfig.CardCrackingConfig cardCrackConfig = globalConfig.cardCrackConfig;
            int oldThreshold = cardCrackConfig.timesPlayedToStartCracking;

            cardCrackConfig.timesPlayedToStartCracking = threshold;
            globalConfig.cardCrackConfig = cardCrackConfig;

            Log.LogInfo("Card cracking threshold: " + oldThreshold + " -> " + threshold);
        }

        [HarmonyPatch(typeof(GlobalConfig), "OnEnable")]
        public static class GlobalConfigOnEnablePatch
        {
            public static void Postfix()
            {
                Instance?.ApplyCardCrackConfig();
            }
        }
    }
}
