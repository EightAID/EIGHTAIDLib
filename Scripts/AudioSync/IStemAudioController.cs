namespace EightAID.EIGHTAIDLib.AudioSync
{
    /// <summary>
    /// Controls named music stems. Part names must match the data authored for each controller.
    /// </summary>
    public interface IStemAudioController
    {
        float BgmVolume { get; }
        float SeVolume { get; }
        float EffectiveBgmVolume { get; }
        float EffectiveSeVolume { get; }
        bool SuppressRapidSameSe { get; }
        float SameSeIntervalSeconds { get; }

        bool PlayScheduled(double startDspTime);
        void StopAll();
        void ApplyCurrentBgmVolume();
        void SetBgmVolume(float value);
        void SetSeVolume(float value);
    }
}
