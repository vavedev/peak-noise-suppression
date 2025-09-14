using System.IO;
using UnityEngine;

namespace PeakNoiseSuppression
{
    public sealed class ConfigReloader : MonoBehaviour
    {
        private FileSystemWatcher? watcher;
        private string? configPath;

        private void Start()
        {
            configPath = Plugin.Instance.Config.ConfigFilePath;
            if (string.IsNullOrEmpty(configPath))
            {
                Plugin.Log.LogWarning("[ConfigReloader] Config path missing.");
                return;
            }

            watcher = new FileSystemWatcher(Path.GetDirectoryName(configPath)!)
            {
                Filter = Path.GetFileName(configPath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };

            watcher.Changed += (_, __) => OnConfigChanged();
            watcher.EnableRaisingEvents = true;

            Plugin.Log.LogInfo("[ConfigReloader] Watching config: " + configPath);
        }

        private void OnConfigChanged()
        {
            try
            {
                Plugin.Instance.Config.Reload();
                Plugin.Log.LogInfo("[ConfigReloader] Reloaded config.");

                // Apply updated parameters to active components
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
            catch (System.Exception ex)
            {
                Plugin.Log.LogError("[ConfigReloader] Reload failed: " + ex);
            }
        }

        private void OnDestroy()
        {
            watcher?.Dispose();
        }
    }
}
