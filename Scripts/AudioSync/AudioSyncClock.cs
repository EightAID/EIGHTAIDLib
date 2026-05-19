using System;
using UnityEngine;

namespace EightAID.EIGHTAIDLib.AudioSync
{
    /// <summary>
    /// Converts Unity DSP time into song time, beats, and bars for audio-synchronized gameplay.
    /// DSP time is the audio system timeline, while visual/gameplay time may be shifted by offsets.
    /// </summary>
    public sealed class AudioSyncClock
    {
        private double songStartDspTime;
        private double secondsPerBeat = 0.5d;
        private int beatsPerBar = 4;
        private double audioOffsetSeconds;
        private double visualOffsetSeconds;

        public bool IsRunning { get; private set; }
        public double CurrentDspTime { get; private set; }
        public double SongTimeSeconds { get; private set; }
        public double CurrentBeat { get; private set; }
        public int CurrentBar { get; private set; }

        public void Start(double startDspTime, AudioSyncClockSettings settings)
        {
            songStartDspTime = startDspTime;
            ApplySettingsInternal(settings);
            IsRunning = true;
            UpdateNow();
        }

        public void Stop()
        {
            IsRunning = false;
            UpdateNow();
        }

        public void Reset()
        {
            IsRunning = false;
            CurrentDspTime = AudioSettings.dspTime;
            SongTimeSeconds = 0d;
            CurrentBeat = 0d;
            CurrentBar = 0;
        }

        public void ApplySettings(AudioSyncClockSettings settings)
        {
            ApplySettingsInternal(settings);
            UpdateNow();
        }

        public void UpdateNow()
        {
            CurrentDspTime = AudioSettings.dspTime;
            if (!IsRunning)
            {
                return;
            }

            SongTimeSeconds = Math.Max(0d, CurrentDspTime - songStartDspTime + audioOffsetSeconds);
            CurrentBeat = SongTimeSeconds / secondsPerBeat;
            CurrentBar = (int)Math.Floor(CurrentBeat / beatsPerBar) + 1;
        }

        public double GetNextBeatDspTime(double inputGraceSeconds)
        {
            return GetNextSubdivisionDspTime(1d, inputGraceSeconds);
        }

        public double GetNextBarDspTime()
        {
            UpdateNow();
            double currentBeatWithVisual = (SongTimeSeconds + visualOffsetSeconds) / secondsPerBeat;
            double nextBarBeat = Math.Ceiling(currentBeatWithVisual / beatsPerBar) * beatsPerBar;
            if (nextBarBeat <= currentBeatWithVisual + 0.0001d)
            {
                nextBarBeat += beatsPerBar;
            }

            return BeatToDspTime(nextBarBeat);
        }

        public double GetNextSubdivisionDspTime(double subdivisionsPerBeat, double inputGraceSeconds)
        {
            UpdateNow();

            double step = 1d / Math.Max(1d, subdivisionsPerBeat);
            double currentBeatWithGrace = (SongTimeSeconds + visualOffsetSeconds + inputGraceSeconds) / secondsPerBeat;
            double nextBeat = Math.Ceiling(currentBeatWithGrace / step) * step;
            if (nextBeat <= CurrentBeat + 0.0001d)
            {
                nextBeat += step;
            }

            return BeatToDspTime(nextBeat);
        }

        public double ResolveTiming(AudioSyncTiming timing, double inputGraceSeconds)
        {
            UpdateNow();
            return timing.Mode switch
            {
                AudioSyncTimingMode.Immediate => AudioSettings.dspTime,
                AudioSyncTimingMode.NextHalfBeat => GetNextSubdivisionDspTime(2d, inputGraceSeconds),
                AudioSyncTimingMode.NextQuarterBeat => GetNextSubdivisionDspTime(4d, inputGraceSeconds),
                AudioSyncTimingMode.NextBar => GetNextBarDspTime(),
                AudioSyncTimingMode.BeatsLater => BeatToDspTime(CurrentBeat + Math.Max(0d, timing.Amount)),
                AudioSyncTimingMode.BarsLater => BeatToDspTime(CurrentBeat + Math.Max(0d, timing.Amount) * beatsPerBar),
                _ => GetNextBeatDspTime(inputGraceSeconds)
            };
        }

        public double BeatToDspTime(double beat)
        {
            return songStartDspTime + beat * secondsPerBeat - audioOffsetSeconds;
        }

        public double DspTimeToBeat(double dspTime)
        {
            return Math.Max(0d, dspTime - songStartDspTime + audioOffsetSeconds) / secondsPerBeat;
        }

        private void ApplySettingsInternal(AudioSyncClockSettings settings)
        {
            beatsPerBar = Mathf.Max(1, settings.BeatsPerBar);
            secondsPerBeat = 60d / Math.Max(1d, settings.Bpm);
            audioOffsetSeconds = settings.AudioOffsetSeconds;
            visualOffsetSeconds = settings.VisualOffsetSeconds;
        }
    }
}
