using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Nosebleed.Pancake.GameConfig;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Object = UnityEngine.Object;

namespace CardCrackThresholdMod
{
    public enum MessageLanguage
    {
        English,
        ChineseSimplified
    }

    [BepInPlugin("com.imoonday.cardcrackthresholdmod", "Card Crack Threshold Mod", "1.0.1")]
    public class CardCrackThresholdMod : BasePlugin
    {
        private const int GlobalConfigReadyFramesBeforeCapture = 30;

        public static CardCrackThresholdMod Instance;

        private ConfigEntry<int> _timesPlayedToStartCracking;
        private ConfigEntry<Key> _toggleKey;
        private int? _defaultTimesPlayedToStartCrackingRaw;
        private bool _customThresholdActive = true;
        private int _globalConfigReadyFrames;
        private bool _cardCrackConfigInitialized;

        public override void Load()
        {
            Instance = this;

            _timesPlayedToStartCracking = Config.Bind(
                "Card Cracking",
                "TimesPlayedToStartCracking",
                999,
                "The same-turn play count at which the same card starts cracking."
            );
            _toggleKey = Config.Bind(
                "Hotkeys",
                "ToggleCustomThresholdKey",
                Key.F8,
                "Press this key to temporarily restore the game's default card cracking threshold. Press it again to reapply the configured threshold."
            );
            ClassInjector.RegisterTypeInIl2Cpp<CardCrackThresholdHotkeyListener>();
            GameObject hotkeyListener = new GameObject("Card Crack Threshold Mod Hotkey Listener");
            Object.DontDestroyOnLoad(hotkeyListener);
            hotkeyListener.hideFlags = HideFlags.HideAndDontSave;
            hotkeyListener.AddComponent<CardCrackThresholdHotkeyListener>();

            var harmony = new Harmony("com.imoonday.cardcrackthresholdmod");
            harmony.PatchAll();

            Log.LogInfo("Card Crack Threshold Mod loaded. The default threshold will be captured during initialization, and the configured threshold starts active.");
        }

        public Key ToggleKey => _toggleKey.Value;

        public void OnGlobalConfigEnabled()
        {
            _globalConfigReadyFrames = 0;
            _cardCrackConfigInitialized = false;
            Log.LogInfo("GlobalConfig.OnEnable detected; waiting briefly before capturing the default card cracking threshold.");
        }

        public void TryInitializeCardCrackConfig()
        {
            if (_cardCrackConfigInitialized)
                return;

            GlobalConfig globalConfig = GlobalConfig.Instance;
            if (globalConfig == null)
            {
                _globalConfigReadyFrames = 0;
                return;
            }

            _globalConfigReadyFrames++;
            if (_globalConfigReadyFrames < GlobalConfigReadyFramesBeforeCapture)
                return;

            CaptureDefaultCardCrackConfig(globalConfig);
            ApplyCardCrackConfig();

            _cardCrackConfigInitialized = true;
        }

        public void ApplyCardCrackConfig()
        {
            int configuredThreshold = _timesPlayedToStartCracking.Value;

            if (configuredThreshold < 1)
                throw new ArgumentOutOfRangeException(nameof(configuredThreshold), "TimesPlayedToStartCracking must be at least 1.");

            GlobalConfig globalConfig = GlobalConfig.Instance;
            if (globalConfig == null)
            {
                Log.LogWarning("GlobalConfig.Instance is not ready yet; card cracking threshold will be applied when GlobalConfig enables.");
                return;
            }

            GlobalConfig.CardCrackingConfig cardCrackConfig = globalConfig.cardCrackConfig;
            int oldThreshold = cardCrackConfig.timesPlayedToStartCracking;

            if (!_defaultTimesPlayedToStartCrackingRaw.HasValue)
            {
                throw new InvalidOperationException("Default card cracking threshold must be captured before applying config.");
            }

            int threshold = _customThresholdActive ? configuredThreshold : _defaultTimesPlayedToStartCrackingRaw.Value;

            cardCrackConfig.timesPlayedToStartCracking = threshold;
            globalConfig.cardCrackConfig = cardCrackConfig;

            Log.LogInfo("Card cracking threshold: " + oldThreshold + " -> " + threshold);
        }

        public void ToggleCustomThreshold()
        {
            TryInitializeCardCrackConfig();
            if (!_cardCrackConfigInitialized)
            {
                string waitingMessage = GetInitializationPendingMessage();
                CardCrackThresholdHotkeyListener.ShowMessage(waitingMessage);
                Log.LogWarning(waitingMessage);
                return;
            }

            _customThresholdActive = !_customThresholdActive;
            ApplyCardCrackConfig();

            string message = GetToggleMessage();

            CardCrackThresholdHotkeyListener.ShowMessage(message);
            Log.LogInfo(message);
        }

        private string GetToggleMessage()
        {
            string defaultThreshold = _defaultTimesPlayedToStartCrackingRaw?.ToString() ?? GetLocalizedWhenReady();

            if (GetEffectiveMessageLanguage() == MessageLanguage.ChineseSimplified)
            {
                return _customThresholdActive
                    ? "卡牌裂纹阈值修改已启用：" + _timesPlayedToStartCracking.Value
                    : "卡牌裂纹阈值修改已暂停：已还原默认值 " + defaultThreshold;
            }

            return _customThresholdActive
                ? "Card crack threshold mod enabled: " + _timesPlayedToStartCracking.Value
                : "Card crack threshold mod disabled: restored default " + defaultThreshold;
        }

        private string GetLocalizedWhenReady()
        {
            return GetEffectiveMessageLanguage() == MessageLanguage.ChineseSimplified ? "等待捕获" : "when ready";
        }

        private string GetInitializationPendingMessage()
        {
            return GetEffectiveMessageLanguage() == MessageLanguage.ChineseSimplified
                ? "卡牌裂纹阈值默认值尚未捕获，请稍后再试"
                : "Card crack default threshold is not captured yet. Try again shortly.";
        }

        private MessageLanguage GetEffectiveMessageLanguage()
        {
            UnityEngine.Localization.Locale selectedLocale = LocalizationSettings.SelectedLocale;
            if (selectedLocale != null)
            {
                string localeCode = selectedLocale.Identifier.Code;
                if (!string.IsNullOrEmpty(localeCode) && localeCode.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                    return MessageLanguage.ChineseSimplified;

                string localeName = selectedLocale.LocaleName;
                if (!string.IsNullOrEmpty(localeName) && localeName.Contains("Chinese", StringComparison.OrdinalIgnoreCase))
                    return MessageLanguage.ChineseSimplified;

                return MessageLanguage.English;
            }

            return Application.systemLanguage is SystemLanguage.Chinese
                or SystemLanguage.ChineseSimplified
                or SystemLanguage.ChineseTraditional
                ? MessageLanguage.ChineseSimplified
                : MessageLanguage.English;
        }

        private void CaptureDefaultCardCrackConfig(GlobalConfig globalConfig)
        {
            if (_defaultTimesPlayedToStartCrackingRaw.HasValue)
                return;

            GlobalConfig.CardCrackingConfig cardCrackConfig = globalConfig.cardCrackConfig;
            _defaultTimesPlayedToStartCrackingRaw = cardCrackConfig.timesPlayedToStartCracking;

            Log.LogInfo(
                "Captured default card cracking threshold: raw=" + _defaultTimesPlayedToStartCrackingRaw.Value
            );
        }

        [HarmonyPatch(typeof(GlobalConfig), "OnEnable")]
        public static class GlobalConfigOnEnablePatch
        {
            public static void Postfix()
            {
                Instance?.OnGlobalConfigEnabled();
            }
        }
    }

    public class CardCrackThresholdHotkeyListener : MonoBehaviour
    {
        private static string _message;
        private static float _messageUntil;
        private static GUIStyle _messageStyle;
        private bool _toggleKeyWasPressed;

        public CardCrackThresholdHotkeyListener(IntPtr ptr) : base(ptr)
        {
        }

        public void Update()
        {
            CardCrackThresholdMod plugin = CardCrackThresholdMod.Instance;
            if (plugin == null)
                return;

            plugin.TryInitializeCardCrackConfig();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || plugin.ToggleKey == Key.None)
                return;

            KeyControl keyControl = keyboard[plugin.ToggleKey];
            bool toggleKeyPressed = keyControl != null && keyControl.isPressed;
            if (toggleKeyPressed && !_toggleKeyWasPressed)
                plugin.ToggleCustomThreshold();

            _toggleKeyWasPressed = toggleKeyPressed;
        }

        public void OnGUI()
        {
            if (string.IsNullOrEmpty(_message) || Time.unscaledTime >= _messageUntil)
                return;

            _messageStyle ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                wordWrap = true
            };

            float width = Mathf.Min(560f, Mathf.Max(240f, Screen.width - 24f));
            const float height = 48f;
            float left = (Screen.width - width) * 0.5f;
            GUI.Box(new Rect(left, 36f, width, height), _message, _messageStyle);
        }

        public static void ShowMessage(string message)
        {
            _message = message;
            _messageUntil = Time.unscaledTime + 2.5f;
        }
    }
}
