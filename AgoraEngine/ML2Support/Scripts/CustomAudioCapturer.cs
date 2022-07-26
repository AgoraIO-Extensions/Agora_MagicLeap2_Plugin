using System.Threading;
using UnityEngine;
using agora_gaming_rtc;
using RingBuffer;
using System;
using UnityEngine.XR.MagicLeap;

namespace agora_sample
{
    /// <summary>
    /// The Custom Audio Capturer class uses Microphone audio source to
    /// capture voice input through ML2Audio.  The audio buffer is pushed
    /// constantly using the PushAudioFrame API in a thread. 
    /// </summary>
    public class CustomAudioCapturer : MonoBehaviour
    {
        [SerializeField]
        private AudioSource InputAudioSource = null;

        // Audio stuff
        public static int CHANNEL = 1;
        public const int
            SAMPLE_RATE = 48000; // Please do not change this value because Unity re-samples the sample rate to 48000.

        private const int RESCALE_FACTOR = 32767; // for short to byte conversion
        private int PUSH_FREQ_PER_SEC = 100;

        private RingBuffer<byte> _audioBuffer;
        private bool _startConvertSignal = false;

        private Thread _pushAudioFrameThread;
        private bool _pushAudioFrameThreadSignal = false;
        private int _count;

        private bool _startSignal = false;

        const int AUDIO_CLIP_LENGTH_SECONDS = 60;

        IRtcEngine mRtcEngine;

        private ML2BufferClip mlAudioBufferClip;

        private void Awake()
        {
            StartMicrophone();
        }

        private void OnDestroy()
        {
            StopAudioPush();
        }

        // Find and configure audio input, called during Awake
        private void StartMicrophone()
        {
            var captureType = MLAudioInput.MicCaptureType.VoiceCapture;
            if (!MLPermissions.CheckPermission(MLPermission.RecordAudio).IsOk)
            {
                Debug.LogError($"AudioCaptureExample.StartMicrophone() cannot start, {MLPermission.RecordAudio} not granted.");
                return;
            }
            mlAudioBufferClip = new ML2BufferClip(MLAudioInput.MicCaptureType.VoiceCapture, AUDIO_CLIP_LENGTH_SECONDS, MLAudioInput.GetSampleRate(captureType));
            mlAudioBufferClip.OnReceiveSampleCallback += HandleAudioBuffer;
        }

        public void StartPushAudioFrame()
        {
            var bufferLength = SAMPLE_RATE / PUSH_FREQ_PER_SEC * CHANNEL * 10000;
            _audioBuffer = new RingBuffer<byte>(bufferLength);
            _startConvertSignal = true;

            _pushAudioFrameThreadSignal = true;
            _pushAudioFrameThread = new Thread(PushAudioFrameThread);
            _pushAudioFrameThread.Start();
        }

        public void StopAudioPush()
        {
            _pushAudioFrameThreadSignal = false;
        }

        void PushAudioFrameThread()
        {
            var bytesPerSample = 2;
            var type = AUDIO_FRAME_TYPE.FRAME_TYPE_PCM16;
            var channels = CHANNEL;
            var samples = SAMPLE_RATE / PUSH_FREQ_PER_SEC;
            var samplesPerSec = SAMPLE_RATE;
            var buffer = new byte[samples * bytesPerSample * CHANNEL];
            var freq = 1000 / PUSH_FREQ_PER_SEC;

            var tic = new TimeSpan(DateTime.Now.Ticks);

            mRtcEngine = IRtcEngine.QueryEngine();

            while (_pushAudioFrameThreadSignal)
            {
                if (!_startSignal)
                {
                    tic = new TimeSpan(DateTime.Now.Ticks);
                }

                var toc = new TimeSpan(DateTime.Now.Ticks);

                if (toc.Subtract(tic).Duration().Milliseconds >= freq)
                {
                    tic = new TimeSpan(DateTime.Now.Ticks);

                    for (var i = 0; i < 2; i++)
                    {
                        lock (_audioBuffer)
                        {
                            if (_audioBuffer.Size > samples * bytesPerSample * CHANNEL)
                            {
                                for (var j = 0; j < samples * bytesPerSample * CHANNEL; j++)
                                {
                                    buffer[j] = _audioBuffer.Get();
                                }

                                var audioFrame = new AudioFrame
                                {
                                    bytesPerSample = bytesPerSample,
                                    type = type,
                                    samples = samples,
                                    samplesPerSec = samplesPerSec,
                                    channels = channels,
                                    buffer = buffer,
                                    renderTimeMs = freq
                                };

                                mRtcEngine.PushAudioFrame(audioFrame);
                            }
                        }
                    }
                }
            }
        }


        private void HandleAudioBuffer(float[] data)
        {
            if (!_startConvertSignal) return;

            foreach (var t in data)
            {
                var sample = t;
                if (sample > 1) sample = 1;
                else if (sample < -1) sample = -1;

                var shortData = (short)(sample * RESCALE_FACTOR);
                var byteArr = new byte[2];
                byteArr = BitConverter.GetBytes(shortData);
                lock (_audioBuffer)
                {
                    if (_audioBuffer.Count <= _audioBuffer.Capacity - 2)
                    {
                        _audioBuffer.Put(byteArr[0]);
                        _audioBuffer.Put(byteArr[1]);
                    }
                }
            }

            _count += 1;
            if (_count == 20) _startSignal = true;
        }
    }

    /// <summary>
    ///   Extending BufferClip class for callback function
    /// </summary>
    public class ML2BufferClip : MLAudioInput.BufferClip
    {
        public ML2BufferClip(MLAudioInput.MicCaptureType captureType, int lengthSec, int frequency) : this(captureType, (uint)lengthSec, (uint)frequency, (uint)MLAudioInput.GetChannels(captureType)) { }

        public ML2BufferClip(MLAudioInput.MicCaptureType captureType, uint samplesLengthInSeconds, uint sampleRate, uint channels)
            : base(captureType, samplesLengthInSeconds, sampleRate, channels) { }

        public event Action<float[]> OnReceiveSampleCallback;

        protected override void OnReceiveSamples(float[] samples)
        {
            base.OnReceiveSamples(samples);
            if (OnReceiveSampleCallback != null)
            {
                OnReceiveSampleCallback(samples);
            }
        }
    }
}
