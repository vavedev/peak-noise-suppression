using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace PeakNoiseSuppression
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "vavedev.PeakNoiseSuppression";
        public const string PLUGIN_NAME = "Peak Noise Suppression";
        public const string PLUGIN_VERSION = "2.2.0";

        internal static Plugin Instance { get; private set; } = null!;
        internal static ManualLogSource Log { get; private set; } = null!;
        private Harmony? harmony;

        internal static ConfigEntry<bool> EnablePlugin = null!;
        internal static ConfigEntry<bool> AddIfMissing = null!;

        internal static ConfigEntry<bool> EnableAGC = null!;
        internal static ConfigEntry<bool> EnableAEC = null!;
        internal static ConfigEntry<bool> EnableNoiseSuppression = null!;
        internal static ConfigEntry<int> NoiseSuppressionLevel = null!;

        internal static ConfigEntry<bool> EnableVoiceDetection = null!;
        internal static ConfigEntry<float> VoiceDetectionThreshold = null!;

        internal static ConfigEntry<bool> EnableSpectralGate = null!;
        internal static ConfigEntry<int> SpectralGateFFTSize = null!;
        internal static ConfigEntry<float> SpectralGateMagnitudeThreshold = null!;
        internal static ConfigEntry<float> SpectralGateSuppressionFactor = null!;
        internal static ConfigEntry<bool> EnableHighPass = null!;
        internal static ConfigEntry<float> HighPassCutoffHz = null!;

        internal static ConfigEntry<bool> EnableTransmitGate = null!;
        internal static ConfigEntry<float> TransmitEnableThreshold = null!;
        internal static ConfigEntry<float> TransmitDisableThreshold = null!;
        internal static ConfigEntry<float> TransmitEnvelopeAlpha = null!;

        internal static ConfigEntry<bool> EnableDebugLogger = null!;
        internal static ConfigEntry<bool> ShowOverlay = null!;
        internal static ConfigEntry<bool> ShowGuiPanel = null!;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            EnablePlugin = Config.Bind("General", "Enabled", true, "Master enable/disable.");
            AddIfMissing = Config.Bind("General", "AddWebRtcIfMissing", true);

            EnableAGC = Config.Bind("DSP", "AGC", true);
            EnableAEC = Config.Bind("DSP", "AEC", true);
            EnableNoiseSuppression = Config.Bind("DSP", "NoiseSuppression", true);
            NoiseSuppressionLevel = Config.Bind("DSP", "NoiseSuppressionLevel", 2);

            EnableVoiceDetection = Config.Bind("VoiceDetection", "Enabled", true);
            VoiceDetectionThreshold = Config.Bind("VoiceDetection", "Threshold", 0.05f);

            EnableSpectralGate = Config.Bind("SpectralGate", "Enabled", true);
            SpectralGateFFTSize = Config.Bind("SpectralGate", "FFTSize", 512);
            SpectralGateMagnitudeThreshold = Config.Bind("SpectralGate", "MagnitudeThreshold", 0.08f);
            SpectralGateSuppressionFactor = Config.Bind("SpectralGate", "SuppressionFactor", 0.05f);
            EnableHighPass = Config.Bind("SpectralGate", "EnableHighPass", true);
            HighPassCutoffHz = Config.Bind("SpectralGate", "HighPassCutoffHz", 120f);

            EnableTransmitGate = Config.Bind("TransmitGate", "Enabled", true);
            TransmitEnableThreshold = Config.Bind("TransmitGate", "EnableRms", 0.02f);
            TransmitDisableThreshold = Config.Bind("TransmitGate", "DisableRms", 0.012f);
            TransmitEnvelopeAlpha = Config.Bind("TransmitGate", "EnvelopeAlpha", 0.2f);

            EnableDebugLogger = Config.Bind("Debug", "EnableLogger", false);
            ShowOverlay = Config.Bind("Debug", "ShowOverlay", true);
            ShowGuiPanel = Config.Bind("Debug", "ShowGuiPanel", true);

            harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            gameObject.AddComponent<ConfigReloader>();
            Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded.");
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
            Log.LogInfo($"{PLUGIN_NAME} unloaded.");
        }
    }
}
