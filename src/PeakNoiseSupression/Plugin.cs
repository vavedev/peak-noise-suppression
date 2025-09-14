using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace PeakNoiseSuppression
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "vavedev.PeakNoiseSuppression";
        public const string PLUGIN_NAME = "Peak Noise Suppression";
        public const string PLUGIN_VERSION = "2.1.1";

        internal static Plugin Instance { get; private set; } = null!;
        internal static ManualLogSource Log { get; private set; } = null!;

        private Harmony? harmony;

        // --- Config entries (initialized in Awake) ---
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

            // General
            EnablePlugin = Config.Bind("General", "Enabled", true, "Master enable/disable for this plugin.");
            AddIfMissing = Config.Bind("General", "AddWebRtcIfMissing", true, "If true, add WebRtcAudioDsp when missing.");

            // Photon DSP toggles
            EnableAGC = Config.Bind("DSP", "AGC", true, "Enable Automatic Gain Control (AGC).");
            EnableAEC = Config.Bind("DSP", "AEC", true, "Enable Acoustic Echo Cancellation (AEC).");
            EnableNoiseSuppression = Config.Bind("DSP", "NoiseSuppression", true, "Enable Photon/WebRTC Noise Suppression if supported.");
            NoiseSuppressionLevel = Config.Bind("DSP", "NoiseSuppressionLevel", 2, "Noise suppression aggressiveness: 0=Low,1=Moderate,2=High,3=VeryHigh.");

            // Voice detection
            EnableVoiceDetection = Config.Bind("VoiceDetection", "Enabled", true, "Enable Recorder.VoiceDetection.");
            VoiceDetectionThreshold = Config.Bind("VoiceDetection", "Threshold", 0.05f, "Recorder.VoiceDetectionThreshold (0 disables).");

            // Spectral gate + HP
            EnableSpectralGate = Config.Bind("SpectralGate", "Enabled", true, "Enable managed spectral gate/noise suppression.");
            SpectralGateFFTSize = Config.Bind("SpectralGate", "FFTSize", 512, "FFT window size (power of 2).");
            SpectralGateMagnitudeThreshold = Config.Bind("SpectralGate", "MagnitudeThreshold", 0.08f, "Relative magnitude threshold (0–1).");
            SpectralGateSuppressionFactor = Config.Bind("SpectralGate", "SuppressionFactor", 0.05f, "Attenuation for bins below threshold (0=mute,1=no suppression).");
            EnableHighPass = Config.Bind("SpectralGate", "EnableHighPass", true, "Apply high-pass filter before gating.");
            HighPassCutoffHz = Config.Bind("SpectralGate", "HighPassCutoffHz", 120f, "High-pass cutoff frequency (Hz).");

            // Transmit gating
            EnableTransmitGate = Config.Bind("TransmitGate", "Enabled", true, "Enable gating of Recorder.TransmitEnabled based on post-processed RMS.");
            TransmitEnableThreshold = Config.Bind("TransmitGate", "EnableRms", 0.02f, "RMS level to enable transmit.");
            TransmitDisableThreshold = Config.Bind("TransmitGate", "DisableRms", 0.012f, "RMS level to disable transmit (hysteresis).");
            TransmitEnvelopeAlpha = Config.Bind("TransmitGate", "EnvelopeAlpha", 0.2f, "Envelope smoothing alpha (0-1).");

            // Debug/UI
            EnableDebugLogger = Config.Bind("Debug", "EnableLogger", false, "Enable mic debug logger.");
            ShowOverlay = Config.Bind("Debug", "ShowOverlay", true, "Show on-screen debug overlay.");
            ShowGuiPanel = Config.Bind("Debug", "ShowGuiPanel", true, "Show UI tuning panel.");

            // Start Harmony patches
            harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Start ConfigReloader on plugin GameObject
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
