using System;
using UnityEngine;
using Photon.Voice.Unity;

namespace PeakNoiseSuppression
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SpectralGateFilter : MonoBehaviour
    {
        // parameters (set by RecorderPatch)
        private int fftSize = 512;
        private float magnitudeThreshold = 0.08f;
        private float suppressionFactor = 0.05f;
        private bool hpEnabled = true;
        private float hpCutoffHz = 120f;

        // transmit gate
        private bool transmitGateEnabled = true;
        private float transmitEnableRms = 0.02f;
        private float transmitDisableRms = 0.012f;
        private float envelopeAlpha = 0.2f;
        private Recorder linkedRecorder = null!;

        // internal state
        private float hpPrevInput = 0f;
        private float hpPrevOutput = 0f;
        private float hpAlpha = 0f;
        private float sampleRate = 48000f;
        private float processedEnvelope = 0f;

        // outputs for other components
        public float[] LastProcessedFrame { get; private set; } = Array.Empty<float>();
        public float LastInputRms { get; private set; }
        public float LastOutputRms { get; private set; }

        // Called by RecorderPatch
        public void SetParameters(int fftSize, float magnitudeThreshold, float suppressionFactor,
            bool hpEnabled, float hpCutoffHz,
            bool transmitGateEnabled, float transmitEnableRms, float transmitDisableRms, float envelopeAlpha,
            Recorder recorder)
        {
            this.fftSize = Mathf.Clamp(fftSize, 128, 4096);
            // round up to nearest power of two
            int p2 = 128;
            while (p2 < this.fftSize) p2 <<= 1;
            this.fftSize = Mathf.Clamp(p2, 128, 4096);

            this.magnitudeThreshold = Mathf.Clamp01(magnitudeThreshold);
            this.suppressionFactor = Mathf.Clamp01(suppressionFactor);
            this.hpEnabled = hpEnabled;
            this.hpCutoffHz = Mathf.Max(10f, hpCutoffHz);

            this.transmitGateEnabled = transmitGateEnabled;
            this.transmitEnableRms = Mathf.Max(0f, transmitEnableRms);
            this.transmitDisableRms = Mathf.Max(0f, transmitDisableRms);
            this.envelopeAlpha = Mathf.Clamp01(envelopeAlpha);
            this.linkedRecorder = recorder;

            sampleRate = AudioSettings.outputSampleRate;
            RecalculateHpAlpha();
        }

        public void UpdateParameters() // used by ConfigReloader / UI
        {
            sampleRate = AudioSettings.outputSampleRate;
            RecalculateHpAlpha();
        }

        private void RecalculateHpAlpha()
        {
            if (!hpEnabled) { hpAlpha = 0f; return; }
            float fc = Mathf.Max(10f, hpCutoffHz);
            float rc = 1f / (2f * Mathf.PI * fc);
            float dt = 1f / sampleRate;
            hpAlpha = rc / (rc + dt);
            hpAlpha = Mathf.Clamp(hpAlpha, 0f, 0.9999f);
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!Plugin.EnablePlugin.Value) return;
            if (data == null || data.Length == 0) return;

            int frames = data.Length / channels;

            if (LastProcessedFrame.Length != data.Length)
                LastProcessedFrame = new float[data.Length];

            // Compute input RMS (mono approximation using first channel)
            double sumIn = 0;
            for (int i = 0; i < frames; i++)
            {
                float s = data[i * channels];
                sumIn += s * s;
            }
            LastInputRms = Mathf.Sqrt((float)(sumIn / frames));

            // Apply HP to mono buffer
            float[] mono = new float[frames];
            for (int i = 0; i < frames; i++)
            {
                float sample = data[i * channels];
                if (hpEnabled)
                {
                    float y = hpAlpha * (hpPrevOutput + sample - hpPrevInput);
                    hpPrevInput = sample;
                    hpPrevOutput = y;
                    sample = y;
                }
                mono[i] = sample;
            }

            // Compute processed RMS
            double sumProcSq = 0;
            for (int i = 0; i < frames; i++) sumProcSq += mono[i] * mono[i];
            float procRms = Mathf.Sqrt((float)(sumProcSq / frames));
            LastOutputRms = procRms;

            // Simple envelope-based gate/detector (drives processedEnvelope)
            float detectorTarget = procRms >= transmitEnableRms ? 1f : 0f;
            processedEnvelope = envelopeAlpha * detectorTarget + (1f - envelopeAlpha) * processedEnvelope;
            float gateGain = processedEnvelope; // [0..1]

            // Apply gate and write back to all channels; also fill LastProcessedFrame
            double sumOutSq = 0;
            for (int i = 0; i < frames; i++)
            {
                float outSample = mono[i] * gateGain;
                for (int c = 0; c < channels; c++)
                {
                    data[i * channels + c] = outSample;
                }
                LastProcessedFrame[i * channels] = outSample;
                sumOutSq += outSample * outSample;
            }

            // Final RMS after gating
            float outRms = Mathf.Sqrt((float)(sumOutSq / frames));
            LastOutputRms = outRms;

            // Transmit gating (hysteresis)
            if (transmitGateEnabled && linkedRecorder != null)
            {
                try
                {
                    bool currently = linkedRecorder.TransmitEnabled;
                    if (!currently && outRms >= transmitEnableRms) linkedRecorder.TransmitEnabled = true;
                    else if (currently && outRms <= transmitDisableRms) linkedRecorder.TransmitEnabled = false;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning("[PeakNoiseSuppression] Transmit toggle exception: " + ex.Message);
                }
            }
        }

        // helper used by logger
        public float ComputeRMS(float[] buffer)
        {
            if (buffer == null || buffer.Length == 0) return 0f;
            double sum = 0;
            foreach (var v in buffer) sum += v * v;
            return Mathf.Sqrt((float)(sum / buffer.Length));
        }
    }
}
