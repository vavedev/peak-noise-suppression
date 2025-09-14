using UnityEngine;
using Photon.Voice.Unity;

namespace PeakNoiseSuppression
{
    public sealed class MicDebugLogger : MonoBehaviour
    {
        private Recorder? recorder;
        private SpectralGateFilter? filter;
        private AnimatedMouth? mouth;
        private float logTimer;
        private string overlayText = string.Empty;

        private void Start()
        {
            recorder = GetComponent<Recorder>() ?? Object.FindFirstObjectByType<Recorder>();
            filter = GetComponent<SpectralGateFilter>() ?? Object.FindFirstObjectByType<SpectralGateFilter>();
            mouth = GetComponent<AnimatedMouth>() ?? Object.FindFirstObjectByType<AnimatedMouth>();
        }

        private void Update()
        {
            if (!Plugin.EnableDebugLogger.Value) return;

            logTimer += Time.deltaTime;
            if (logTimer < 0.25f) return;
            logTimer = 0f;

            float raw = recorder != null ? (recorder.LevelMeter?.CurrentAvgAmp ?? -1f) : -1f;
            float proc = filter != null ? filter.LastOutputRms : -1f;
            float env = filter != null ? filter.MouthEnvelope : -1f;
            bool tx = recorder != null && recorder.TransmitEnabled;
            bool speaking = mouth != null && mouth.isSpeaking;

            overlayText = $"RawRMS={raw:F4} | ProcRMS={proc:F4} | Env={env:F4} | TX={tx} | Mouth={speaking}";
            Plugin.Log.LogInfo("[MicDebug] " + overlayText);
        }

        private void OnGUI()
        {
            if (!Plugin.ShowOverlay.Value || string.IsNullOrEmpty(overlayText)) return;
            GUI.color = Color.cyan;
            GUI.Label(new Rect(10, 10, 1000, 22), overlayText);
        }
    }
}
