using HarmonyLib;
using Photon.Voice.Unity;
using UnityEngine;

namespace PeakNoiseSuppression
{
    [HarmonyPatch(typeof(Recorder), "RestartRecording")]
    public static class Recorder_RestartRecording_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(Recorder __instance)
        {
            if (!Plugin.EnablePlugin.Value || __instance == null) return;

            // Ensure AudioSource so Unity calls OnAudioFilterRead on components attached to this GameObject
            var audioSource = __instance.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = __instance.gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = 0f;
                Plugin.Log.LogInfo("[PeakNoiseSuppression] Added AudioSource for OnAudioFilterRead.");
            }

            // Ensure SpectralGateFilter attached if enabled
            var filter = __instance.GetComponent<SpectralGateFilter>();
            if (Plugin.EnableSpectralGate.Value)
            {
                if (filter == null)
                {
                    filter = __instance.gameObject.AddComponent<SpectralGateFilter>();
                    Plugin.Log.LogInfo("[PeakNoiseSuppression] Added SpectralGateFilter component.");
                }

                filter.SetParameters(
                    Plugin.SpectralGateFFTSize.Value,
                    Plugin.SpectralGateMagnitudeThreshold.Value,
                    Plugin.SpectralGateSuppressionFactor.Value,
                    Plugin.EnableHighPass.Value,
                    Plugin.HighPassCutoffHz.Value,
                    Plugin.EnableTransmitGate.Value,
                    Plugin.TransmitEnableThreshold.Value,
                    Plugin.TransmitDisableThreshold.Value,
                    Plugin.TransmitEnvelopeAlpha.Value,
                    __instance
                );
            }
            else if (filter != null)
            {
                Object.Destroy(filter);
                Plugin.Log.LogInfo("[PeakNoiseSuppression] Removed SpectralGateFilter (disabled in config).");
            }

            // Add debug/logger overlay panel
            if (Plugin.EnableDebugLogger.Value && __instance.GetComponent<MicDebugLogger>() == null)
            {
                __instance.gameObject.AddComponent<MicDebugLogger>();
                Plugin.Log.LogInfo("[PeakNoiseSuppression] Added MicDebugLogger.");
            }

            // Add ui tuner if requested (use FindFirstObjectByType to check)
            if (Plugin.ShowGuiPanel.Value && Object.FindFirstObjectByType<InGameTuner>() == null)
            {
                __instance.gameObject.AddComponent<InGameTuner>();
                Plugin.Log.LogInfo("[PeakNoiseSuppression] Added InGameTuner UI component.");
            }

            // Try to configure WebRtcAudioDsp if present or add if requested
            var dsp = __instance.GetComponent<WebRtcAudioDsp>();
            if (dsp == null && Plugin.AddIfMissing.Value)
            {
                dsp = __instance.gameObject.AddComponent<WebRtcAudioDsp>();
                Plugin.Log.LogInfo("[PeakNoiseSuppression] Added WebRtcAudioDsp component.");
            }

            if (dsp != null)
            {
                try
                {
                    dsp.AGC = Plugin.EnableAGC.Value;
                    dsp.AEC = Plugin.EnableAEC.Value;
                    dsp.NoiseSuppression = Plugin.EnableNoiseSuppression.Value;
                    var levelProp = dsp.GetType().GetProperty("NoiseSuppressionLevel");
                    if (levelProp != null)
                    {
                        levelProp.SetValue(dsp, Mathf.Clamp(Plugin.NoiseSuppressionLevel.Value, 0, 3), null);
                        Plugin.Log.LogInfo("[PeakNoiseSuppression] Set WebRtc NoiseSuppressionLevel.");
                    }

                    Plugin.Log.LogInfo($"[PeakNoiseSuppression] Applied DSP settings: AGC={dsp.AGC}, AEC={dsp.AEC}, NoiseSuppression={dsp.NoiseSuppression}");
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.LogError("[PeakNoiseSuppression] Failed to apply WebRtcAudioDsp settings: " + ex);
                }
            }

            // Apply voice detection settings to Recorder
            try
            {
                __instance.VoiceDetection = Plugin.EnableVoiceDetection.Value;
                __instance.VoiceDetectionThreshold = Plugin.VoiceDetectionThreshold.Value;
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError("[PeakNoiseSuppression] Failed to set Recorder voice detection: " + ex);
            }
        }
    }
}
