namespace EightAID.EIGHTAIDLib.AudioSync
{
    /// <summary>
    /// Beat clock settings shared by audio-synchronized gameplay systems.
    /// </summary>
    public readonly struct AudioSyncClockSettings
    {
        public readonly float Bpm;
        public readonly int BeatsPerBar;
        public readonly float AudioOffsetSeconds;
        public readonly float VisualOffsetSeconds;

        public AudioSyncClockSettings(float bpm, int beatsPerBar, float audioOffsetSeconds, float visualOffsetSeconds)
        {
            Bpm = bpm;
            BeatsPerBar = beatsPerBar;
            AudioOffsetSeconds = audioOffsetSeconds;
            VisualOffsetSeconds = visualOffsetSeconds;
        }
    }
}
