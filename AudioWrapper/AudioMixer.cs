using SharpDX.XAudio2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioWrapper
{
    public class AudioMixer : IDisposable
    {
        internal XAudio2 Mixer { get; private set; }
        internal MasteringVoice MasterVoice { get; private set; }
        public bool IsDisposed { get; private set; }

        public AudioMixer()
        {
            Mixer = new XAudio2();
            MasterVoice = new MasteringVoice(Mixer);
            IsDisposed = false;

        }

        public AudioMixer(int channels = 2, int sampleRate = 44100)
        {
            Mixer = new XAudio2();
            MasterVoice = new MasteringVoice(Mixer, channels, sampleRate);
            IsDisposed = false;
        }

        public SoundBuffer NewSound(string fileName, int defaultRepeat = 0)
        {
            return new SoundBuffer(this, fileName, defaultRepeat);
        }

        public float Volume
        {
            get
            {
                return MasterVoice.Volume;
            }
            set
            {
                MasterVoice.SetVolume(value);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (!MasterVoice.IsDisposed)
            {
                MasterVoice.DestroyVoice();
                MasterVoice.Dispose();
            }

            if (!Mixer.IsDisposed)
            {
                Mixer.StopEngine();
                Mixer.Dispose();
            }

            if (disposing)
            {
                MasterVoice = null;
                Mixer = null;
            }

            IsDisposed = true;
        }

        ~AudioMixer()
        {
            Dispose(false);
        }
    }
}
