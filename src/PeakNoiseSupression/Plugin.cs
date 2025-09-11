using BepInEx;
using BepInEx.Logging;

namespace PeakNoiseSupression;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    public const string PLUGIN_GUID = "vavedev.PeakNoiseSuppression";

    public const string PLUGIN_NAME = "Peak Noise Suppression";

    public const string PLUGIN_VERSION = "1.0.0";

    internal static ManualLogSource Log { get; private set; } = null!;

    internal static Harmony? Harmony { get; set; }

    private void Awake()
    {
        Log = Logger;
        Log.LogInfo($"Plugin {PLUGIN_NAME} is loaded!");
        Patch();
    }

    private void Patch()
    {
        Harmony = new Harmony("vavedev.PeakNoiseSuppression");
        Harmony.PatchAll(typeof(RecorderPatch));
    }
}

[HarmonyPatch(typeof(Recorder), "RestartRecording")]
public static class RecorderPatch
{
    [HarmonyPrefix]
    public static void Prefix(Recorder __instance)
    {
        if (!((Object)(object)__instance == (Object)null))
        {
            WebRtcAudioDsp val = ((Component)__instance).GetComponent<WebRtcAudioDsp>();
            if ((Object)(object)val == (Object)null)
            {
                val = ((Component)__instance).gameObject.AddComponent<WebRtcAudioDsp>();
                Debug.Log((object)"[VoiceNoiseSuppressionMod] Added WebRtcAudioDsp component.");
            }
            val.AGC = true;
            val.NoiseSuppression = true;
            val.AEC = true;
            Debug.Log((object)"[VoiceNoiseSuppressionMod] Enabled Noise Suppression, Echo Cancellation, and AGC.");
        }
    }
}