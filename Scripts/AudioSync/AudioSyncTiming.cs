namespace EightAID.EIGHTAIDLib.AudioSync
{
    public enum AudioSyncTimingMode
    {
        Immediate,
        NextBeat,
        NextHalfBeat,
        NextQuarterBeat,
        NextBar,
        BeatsLater,
        BarsLater
    }

    /// <summary>
    /// Describes when a gameplay event should happen relative to the current music clock.
    /// </summary>
    public readonly struct AudioSyncTiming
    {
        public readonly AudioSyncTimingMode Mode;
        public readonly double Amount;

        public AudioSyncTiming(AudioSyncTimingMode mode, double amount = 1d)
        {
            Mode = mode;
            Amount = amount;
        }
    }
}
