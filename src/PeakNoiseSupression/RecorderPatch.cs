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

            var audioSource = __instance.GetComponent<AudioSource>() ?? __instance.gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;

            var filter = __instance.GetComponent<SpectralGateFilter>();
            if (Plugin.EnableSpectralGate.Value)
            {
                if (filter == null) filter = __instance.gameObject.AddComponent<SpectralGateFilter>();
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
            else if (filter != null) Object.Destroy(filter);

            if (Plugin.EnableDebugLogger.Value && __instance.GetComponent<MicDebugLogger>() == null)
                __instance.gameObject.AddComponent<MicDebugLogger>();

            if (Plugin.ShowGuiPanel.Value && Object.FindFirstObjectByType<InGameTuner>() == null)
                __instance.gameObject.AddComponent<InGameTuner>();

            var dsp = __instance.GetComponent<WebRtcAudioDsp>() ?? (Plugin.AddIfMissing.Value ? __instance.gameObject.AddComponent<WebRtcAudioDsp>() : null);
            if (dsp != null)
            {
                try
                {
                    dsp.AGC = Plugin.EnableAGC.Value;
                    dsp.AEC = Plugin.EnableAEC.Value;
                    dsp.NoiseSuppression = Plugin.EnableNoiseSuppression.Value;
                    var levelProp = dsp.GetType().GetProperty("NoiseSuppressionLevel");
                    if (levelProp != null) levelProp.SetValue(dsp, Mathf.Clamp(Plugin.NoiseSuppressionLevel.Value, 0, 3), null);
                }
                catch { }
            }

            try
            {
                __instance.VoiceDetection = Plugin.EnableVoiceDetection.Value;
                __instance.VoiceDetectionThreshold = Plugin.VoiceDetectionThreshold.Value;
            }
            catch { }
        }
    }
}
