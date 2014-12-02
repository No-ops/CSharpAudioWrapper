using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioWrapper
{
    public class PlayOptions
    {
        public float Volume { get; set; }
        public float Pitch { get; set; }
        public float Pan { get; set; }
        public int Repeats { get; set; }

        public PlayOptions(float volume = 1, int repeats = 0, float pitch = 1, float pan = 0)
        {
            Volume = volume;
            Pitch = pitch;
            Pan = pan;
            Repeats = repeats;
        }
    }
}
