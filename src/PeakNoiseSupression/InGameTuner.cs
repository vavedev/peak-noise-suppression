using UnityEngine;

namespace PeakNoiseSuppression
{
    public sealed class InGameTuner : MonoBehaviour
    {
        private Rect windowRect = new Rect(10, 40, 380, 420);
        private Vector2 scroll = Vector2.zero;

        private void OnGUI()
        {
            if (!Plugin.ShowGuiPanel.Value) return;

            GUI.Window(424242, windowRect, WindowFunc, "Peak Noise Suppression - Tuner");
        }

        private void WindowFunc(int id)
        {
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(windowRect.width - 10), GUILayout.Height(windowRect.height - 40));

            GUILayout.Label("DSP Settings");
            Plugin.EnableAGC.Value = GUILayout.Toggle(Plugin.EnableAGC.Value, "AGC");
            Plugin.EnableAEC.Value = GUILayout.Toggle(Plugin.EnableAEC.Value, "AEC");
            Plugin.EnableNoiseSuppression.Value = GUILayout.Toggle(Plugin.EnableNoiseSuppression.Value, "Noise Suppression");

            GUILayout.Label("Spectral Gate");
            Plugin.SpectralGateMagnitudeThreshold.Value = Mathf.Clamp(GUILayout.HorizontalSlider(Plugin.SpectralGateMagnitudeThreshold.Value, 0f, 0.5f), 0f, 0.5f);
            Plugin.SpectralGateSuppressionFactor.Value = Mathf.Clamp(GUILayout.HorizontalSlider(Plugin.SpectralGateSuppressionFactor.Value, 0f, 1f), 0f, 1f);
            Plugin.HighPassCutoffHz.Value = Mathf.Clamp(GUILayout.HorizontalSlider(Plugin.HighPassCutoffHz.Value, 20f, 800f), 20f, 800f);

            GUILayout.Label("Transmit Gate");
            Plugin.TransmitEnableThreshold.Value = Mathf.Clamp(GUILayout.HorizontalSlider(Plugin.TransmitEnableThreshold.Value, 0f, 0.1f), 0f, 0.1f);
            Plugin.TransmitDisableThreshold.Value = Mathf.Clamp(GUILayout.HorizontalSlider(Plugin.TransmitDisableThreshold.Value, 0f, Plugin.TransmitEnableThreshold.Value), 0f, Plugin.TransmitEnableThreshold.Value);

            if (GUILayout.Button("Apply"))
            {
                foreach (var f in Object.FindObjectsByType<SpectralGateFilter>(FindObjectsSortMode.None))
                    f.UpdateParameters();
            }

            if (GUILayout.Button("Close")) Plugin.ShowGuiPanel.Value = false;

            GUILayout.EndScrollView();
        }
    }
}
