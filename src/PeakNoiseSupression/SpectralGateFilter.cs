using System;
using UnityEngine;
using Photon.Voice.Unity;

namespace PeakNoiseSuppression
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SpectralGateFilter : MonoBehaviour
    {
        private int fftSize = 512;
        private float magnitudeThreshold = 0.08f;
        private float suppressionFactor = 0.05f;
        private bool hpEnabled = true;
        private float hpCutoffHz = 120f;

        private bool transmitGateEnabled = true;
        private float transmitEnableRms = 0.02f;
        private float transmitDisableRms = 0.012f;
        private float envelopeAlpha = 0.2f;
        private Recorder linkedRecorder = null!;

        private float hpPrevInput = 0f;
        private float hpPrevOutput = 0f;
        private float hpAlpha = 0f;
        private float sampleRate = 48000f;
        private float processedEnvelope = 0f;

        public float[] LastProcessedFrame { get; private set; } = Array.Empty<float>();
        public float LastInputRms { get; private set; }
        public float LastOutputRms { get; private set; }
        public float MouthEnvelope => processedEnvelope;

        public void SetParameters(int fftSize, float magnitudeThreshold, float suppressionFactor,
            bool hpEnabled, float hpCutoffHz,
            bool transmitGateEnabled, float transmitEnableRms, float transmitDisableRms, float envelopeAlpha,
            Recorder recorder)
        {
            this.fftSize = Mathf.Clamp(fftSize, 128, 4096);
            int p2 = 128; while (p2 < this.fftSize) p2 <<= 1;
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

        public void UpdateParameters()
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
            if (!Plugin.EnablePlugin.Value || data == null || data.Length == 0) return;

            int frames = data.Length / channels;
            if (LastProcessedFrame.Length != data.Length) LastProcessedFrame = new float[data.Length];

            double sumIn = 0;
            for (int i = 0; i < frames; i++) { float s = data[i * channels]; sumIn += s * s; }
            LastInputRms = Mathf.Sqrt((float)(sumIn / frames));

            float[] mono = new float[frames];
            for (int i = 0; i < frames; i++)
            {
                float sample = data[i * channels];
                if (hpEnabled) { float y = hpAlpha * (hpPrevOutput + sample - hpPrevInput); hpPrevInput = sample; hpPrevOutput = y; sample = y; }
                mono[i] = sample;
            }

            double sumProcSq = 0;
            for (int i = 0; i < frames; i++) sumProcSq += mono[i] * mono[i];
            float procRms = Mathf.Sqrt((float)(sumProcSq / frames));
            LastOutputRms = procRms;

            float detectorTarget = procRms >= transmitEnableRms ? 1f : 0f;
            processedEnvelope = envelopeAlpha * detectorTarget + (1f - envelopeAlpha) * processedEnvelope;
            float gateGain = processedEnvelope;

            double sumOutSq = 0;
            for (int i = 0; i < frames; i++)
            {
                float outSample = mono[i] * gateGain;
                for (int c = 0; c < channels; c++) data[i * channels + c] = outSample;
                LastProcessedFrame[i * channels] = outSample;
                sumOutSq += outSample * outSample;
            }
            LastOutputRms = Mathf.Sqrt((float)(sumOutSq / frames));

            if (transmitGateEnabled && linkedRecorder != null)
            {
                try
                {
                    bool currently = linkedRecorder.TransmitEnabled;
                    if (!currently && LastOutputRms >= transmitEnableRms) linkedRecorder.TransmitEnabled = true;
                    else if (currently && LastOutputRms <= transmitDisableRms) linkedRecorder.TransmitEnabled = false;
                }
                catch { }
            }
        }
    }
}
