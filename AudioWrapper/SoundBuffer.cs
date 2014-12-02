using SharpDX;
using SharpDX.IO;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioWrapper
{
    public class SoundBuffer : IDisposable
    {
        string fileName;
        XAudio2 xAudio2;
        MasteringVoice masteringVoice;
        DataStream stream;
        WaveFormat waveFormat;
        bool isLoaded;
        SourceVoice nextVoice;
        int defaultRepeat;
        int voiceListKey;
        public Dictionary<int, SourceVoice> voiceList;

        internal SoundBuffer(AudioMixer audioMixer, string fileName, int defaultRepeat)
        {
            xAudio2 = audioMixer.Mixer;
            masteringVoice = audioMixer.MasterVoice;
            this.defaultRepeat = defaultRepeat;
            this.fileName = fileName;
            voiceList = new Dictionary<int, SourceVoice>();
            voiceListKey = 0;
            isLoaded = false;
        }

        ~SoundBuffer()
        {
            Dispose();
        }

        public void Dispose()
        {
            foreach (KeyValuePair<int, SourceVoice> pair in voiceList)
            {
                if (!pair.Value.IsDisposed)
                {
                    pair.Value.DestroyVoice();
                    pair.Value.Dispose();
                }
            }
            if (nextVoice != null && !nextVoice.IsDisposed)
            {
                nextVoice.DestroyVoice();
                nextVoice.Dispose();
            }

            if (stream != null)
                stream.Dispose();
        }

        public void Load()
        {
            NativeFileStream nativeFileStream = new NativeFileStream(fileName, NativeFileMode.Open, NativeFileAccess.Read);
            SoundStream soundStream = new SoundStream(nativeFileStream);
            stream = soundStream.ToDataStream();
            waveFormat = soundStream.Format;
            LoadNextVoice(defaultRepeat);
            isLoaded = true;

        }

        void LoadNextVoice(int repeat)
        {
            AudioBuffer buffer = new AudioBuffer()
            {
                Stream = stream,
                AudioBytes = (int)stream.Length,
                LoopCount = repeat,
                Flags = BufferFlags.EndOfStream
            };
            nextVoice = new SourceVoice(xAudio2, waveFormat);
            nextVoice.SubmitSourceBuffer(buffer, null);
        }

        public int Play()
        {
            if (!isLoaded)
                Load();

            voiceListKey++;
            voiceList.Add(voiceListKey, nextVoice);
            nextVoice.Start();
            LoadNextVoice(defaultRepeat);

            Dictionary<int, SourceVoice> newVoiceList = new Dictionary<int, SourceVoice>();

            foreach (KeyValuePair<int, SourceVoice> pair in voiceList)
            {
                if (pair.Value.State.BuffersQueued == 0)
                {
                    pair.Value.DestroyVoice();
                    pair.Value.Dispose();
                }
                else
                    newVoiceList.Add(pair.Key, pair.Value);
            }

            voiceList = newVoiceList;

            return voiceListKey;
        }

        public int Play(PlayOptions options)
        {
            if (!isLoaded)
                Load();

            if (options.Repeats != defaultRepeat)
            {
                int loopCount;
                if (options.Repeats == -1)
                    loopCount = AudioBuffer.LoopInfinite;
                else
                    loopCount = options.Repeats;

                LoadNextVoice(loopCount);
            }

            if (options.Pan != 0)
            {
                float panLeft = (float)0.5 - (options.Pan / 2);
                float panRight = (float)0.5 + (options.Pan / 2);
                int matrixSize = masteringVoice.VoiceDetails.InputChannelCount * nextVoice.VoiceDetails.InputChannelCount;
                float[] matrix = new float[matrixSize];

                for (int i = 0; i < matrixSize; i++)
                {
                    if (i % 2 == 0)
                        matrix[i] = panLeft;
                    else
                        matrix[i] = panRight;
                }

                nextVoice.SetOutputMatrix(nextVoice.VoiceDetails.InputChannelCount, masteringVoice.VoiceDetails.InputChannelCount, matrix);
            }

            nextVoice.SetFrequencyRatio(options.Pitch);
            nextVoice.SetVolume(options.Volume);

            Play();

            return voiceListKey;
        }

        public void Stop(int indexToStop)
        {
            if (voiceList.ContainsKey(indexToStop))
            {
                voiceList[indexToStop].Stop();
                voiceList.Remove(indexToStop);
            }
        }

        public void Stop()
        {
            foreach (KeyValuePair<int, SourceVoice> pair in voiceList)
            {
                pair.Value.Stop();
            }
            voiceList = new Dictionary<int, SourceVoice>();
        }
    }
}
