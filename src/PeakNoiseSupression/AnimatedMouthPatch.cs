using HarmonyLib;
using UnityEngine;

namespace PeakNoiseSuppression
{
    [HarmonyPatch(typeof(AnimatedMouth), "Update")]
    public static class AnimatedMouthPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(AnimatedMouth __instance)
        {
            if (!Plugin.EnablePlugin.Value) return true;

            try
            {
                SpectralGateFilter filter = __instance.GetComponent<SpectralGateFilter>() ?? Object.FindFirstObjectByType<SpectralGateFilter>();
                if (filter == null) return true;

                float envelope = filter.MouthEnvelope;
                __instance.isSpeaking = envelope > 0.05f;

                if (__instance.mouthRenderer != null)
                    __instance.mouthRenderer.material.SetInt("_UseTalkSprites", __instance.isSpeaking ? 1 : 0);

                return true;
            }
            catch { return true; }
        }
    }
}
