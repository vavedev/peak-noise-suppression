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

            windowRect = GUI.Window(424242, windowRect, WindowFunc, "Peak Noise Suppression - Tuner");
        }

        private void WindowFunc(int id)
        {
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(windowRect.width - 10), GUILayout.Height(windowRect.height - 40));

            GUILayout.Label("Photon DSP");
            Plugin.EnableAGC.Value = GUILayout.Toggle(Plugin.EnableAGC.Value, "AGC");
            Plugin.EnableAEC.Value = GUILayout.Toggle(Plugin.EnableAEC.Value, "AEC");
            Plugin.EnableNoiseSuppression.Value = GUILayout.Toggle(Plugin.EnableNoiseSuppression.Value, "WebRTC Noise Suppression");

            GUILayout.Space(6);
            GUILayout.Label("Spectral Gate");
            GUILayout.Label($"MagnitudeThreshold: {Plugin.SpectralGateMagnitudeThreshold.Value:F3}");
            float newMag = GUILayout.HorizontalSlider(Plugin.SpectralGateMagnitudeThreshold.Value, 0f, 0.5f);
            if (Mathf.Abs(newMag - Plugin.SpectralGateMagnitudeThreshold.Value) > 1e-6f) Plugin.SpectralGateMagnitudeThreshold.Value = newMag;

            GUILayout.Label($"SuppressionFactor: {Plugin.SpectralGateSuppressionFactor.Value:F3}");
            float newSupp = GUILayout.HorizontalSlider(Plugin.SpectralGateSuppressionFactor.Value, 0f, 1f);
            if (Mathf.Abs(newSupp - Plugin.SpectralGateSuppressionFactor.Value) > 1e-6f) Plugin.SpectralGateSuppressionFactor.Value = newSupp;

            GUILayout.Label($"HighPassCutoffHz: {Plugin.HighPassCutoffHz.Value:F0} Hz");
            float newHp = GUILayout.HorizontalSlider(Plugin.HighPassCutoffHz.Value, 20f, 800f);
            if (Mathf.Abs(newHp - Plugin.HighPassCutoffHz.Value) > 1e-3f) Plugin.HighPassCutoffHz.Value = newHp;

            GUILayout.Space(6);
            GUILayout.Label("Transmit Gate");
            GUILayout.Label($"EnableRms: {Plugin.TransmitEnableThreshold.Value:F3}");
            float newEnable = GUILayout.HorizontalSlider(Plugin.TransmitEnableThreshold.Value, 0f, 0.1f);
            if (Mathf.Abs(newEnable - Plugin.TransmitEnableThreshold.Value) > 1e-6f) Plugin.TransmitEnableThreshold.Value = newEnable;

            GUILayout.Label($"DisableRms: {Plugin.TransmitDisableThreshold.Value:F3}");
            float newDisable = GUILayout.HorizontalSlider(Plugin.TransmitDisableThreshold.Value, 0f, Plugin.TransmitEnableThreshold.Value);
            if (Mathf.Abs(newDisable - Plugin.TransmitDisableThreshold.Value) > 1e-6f) Plugin.TransmitDisableThreshold.Value = newDisable;

            GUILayout.Space(8);
            if (GUILayout.Button("Apply now to active components"))
            {
                foreach (var f in Object.FindObjectsByType<SpectralGateFilter>(FindObjectsSortMode.None))
                    f.UpdateParameters();

                foreach (var r in Object.FindObjectsByType<Photon.Voice.Unity.Recorder>(FindObjectsSortMode.None))
                {
                    try
                    {
                        r.VoiceDetection = Plugin.EnableVoiceDetection.Value;
                        r.VoiceDetectionThreshold = Plugin.VoiceDetectionThreshold.Value;
                    }
                    catch { }
                }
            }

            GUILayout.Space(6);
            if (GUILayout.Button("Close"))
                Plugin.ShowGuiPanel.Value = false;

            GUILayout.EndScrollView();

            GUI.DragWindow();
        }
    }
}
