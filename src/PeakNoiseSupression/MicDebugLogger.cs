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
        private Rect windowRect = new Rect(10, 40, 360, 140);

        private void Start()
        {
            // Use first-instance queries that replace deprecated API
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

            float raw = -1f;
            try
            {
                raw = recorder != null ? (recorder.LevelMeter?.CurrentAvgAmp ?? -1f) : -1f;
            }
            catch { raw = -1f; }

            float proc = filter != null ? filter.LastOutputRms : -1f;
            bool tx = recorder != null && recorder.TransmitEnabled;
            bool speaking = mouth != null && mouth.isSpeaking;

            overlayText = $"RawRMS={raw:F4} | ProcRMS={proc:F4} | TX={tx} | Mouth={speaking}";
            Plugin.Log.LogInfo("[MicDebug] " + overlayText);
        }

        private void OnGUI()
        {
            if (Plugin.ShowOverlay.Value && !string.IsNullOrEmpty(overlayText))
            {
                GUI.color = Color.cyan;
                GUI.Label(new Rect(10, 10, 1000, 22), overlayText);
            }

            if (!Plugin.ShowGuiPanel.Value) return;

            windowRect = GUI.Window(987654, windowRect, GuiWindow, "Peak Noise Suppression - Debug");
        }

        private Vector2 scroll = Vector2.zero;

        private void GuiWindow(int id)
        {
            GUILayout.BeginVertical();
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(Mathf.Max(80, windowRect.height - 60)));

            GUILayout.Label("Debug / Stats");
            GUILayout.Label(overlayText);

            GUILayout.Space(6);
            GUILayout.Label("Active filters:");
            var filters = Object.FindObjectsByType<SpectralGateFilter>(FindObjectsSortMode.None);
            foreach (var f in filters)
            {
                GUILayout.Label($" - {f.gameObject.name}: InRms={f.LastInputRms:F4} OutRms={f.LastOutputRms:F4}");
            }

            GUILayout.EndScrollView();
            if (GUILayout.Button("Close")) Plugin.ShowGuiPanel.Value = false;
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}
