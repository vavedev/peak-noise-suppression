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
            if (!Plugin.EnablePlugin.Value) return true; // fallback to original

            try
            {
                // Prefer local filter on same GameObject, otherwise any filter
                SpectralGateFilter filter = __instance.GetComponent<SpectralGateFilter>() ?? Object.FindFirstObjectByType<SpectralGateFilter>();
                if (filter == null)
                {
                    // No filter -> fallback to original
                    return true;
                }

                float[] buffer = filter.LastProcessedFrame;
                if (buffer == null || buffer.Length == 0)
                {
                    // nothing processed yet; keep mouth silent
                    __instance.isSpeaking = false;
                    if (__instance.mouthRenderer != null) __instance.mouthRenderer.material.SetInt("_UseTalkSprites", 0);
                    return false; // skip original
                }

                // Compute peak energy (similar to original MicrophoneLevelMax)
                int check = Mathf.Min(128, buffer.Length);
                float maxSq = 0f;
                for (int i = 0; i < check; i++)
                {
                    float s = buffer[i];
                    float sq = s * s;
                    if (sq > maxSq) maxSq = sq;
                }

                // convert to decibels (avoid log(0))
                float db = 20f * Mathf.Log10(Mathf.Max(Mathf.Abs(maxSq), 1e-8f));

                // Push-to-talk handling: if local player and push-to settings block speech, force -80 dB
                if (__instance.character != null && __instance.character.IsLocal)
                {
                    var pushToTalkSetting = GameHandler.Instance.SettingsHandler.GetSetting<PushToTalkSetting>();
                    if ((pushToTalkSetting.Value == PushToTalkSetting.PushToTalkType.PushToTalk && !__instance.character.input.pushToTalkPressed) ||
                        (pushToTalkSetting.Value == PushToTalkSetting.PushToTalkType.PushToMute && __instance.character.input.pushToTalkPressed))
                    {
                        db = -80f;
                    }
                }

                // Evaluate animation curve (same field name as original)
                float amount = __instance.decibelToAmountCurve.Evaluate(db);

                // amplitude peak limiter logic (copied from original)
                if (amount > __instance.amplitudePeakLimiter)
                    __instance.amplitudePeakLimiter = amount;

                if (__instance.amplitudePeakLimiter > __instance.minAmplitudeThreshold)
                    __instance.amplitudePeakLimiter -= __instance.amplitudeHighestDecay * Time.deltaTime;

                __instance.volume = amount / (__instance.amplitudePeakLimiter <= 0f ? 1e-6f : __instance.amplitudePeakLimiter);

                if (__instance.volume > __instance.volumePeak) __instance.volumePeak = __instance.volume;
                __instance.volumePeak = Mathf.Lerp(__instance.volumePeak, 0f, Time.deltaTime * __instance.amplitudeSmoothing);

                bool speaking = __instance.volumePeak > __instance.talkThreshold;
                __instance.isSpeaking = speaking;
                if (__instance.mouthRenderer != null)
                {
                    __instance.mouthRenderer.material.SetInt("_UseTalkSprites", speaking ? 1 : 0);
                    int idx = (int)(Mathf.Clamp01(__instance.volumePeak * __instance.amplitudeMult) * (__instance.mouthTextures.Length - 1));
                    idx = Mathf.Clamp(idx, 0, __instance.mouthTextures.Length - 1);
                    __instance.amplitudeIndex = idx;
                    __instance.mouthRenderer.material.SetTexture("_TalkSprite", __instance.mouthTextures[idx]);
                }

                return false; // skip original Update
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError("[PeakNoiseSuppression] AnimatedMouthPatch error: " + ex);
                return true; // on error, fallback to original
            }
        }
    }
}
