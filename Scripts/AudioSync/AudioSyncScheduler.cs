using System;
using System.Collections.Generic;
using UnityEngine;

namespace EightAID.EIGHTAIDLib.AudioSync
{
    /// <summary>
    /// Runs callbacks on Unity's audio DSP timeline. It schedules gameplay callbacks, not AudioSource playback itself.
    /// </summary>
    public sealed class AudioSyncScheduler
    {
        private readonly List<ScheduledAction> scheduledActions = new();
        private int nextId = 1;

        public int ScheduledCount => scheduledActions.Count;

        public int Schedule(double dspTime, Action action)
        {
            if (action == null)
            {
                return 0;
            }

            if (AudioSettings.dspTime >= dspTime)
            {
                action.Invoke();
                return 0;
            }

            int id = nextId++;
            scheduledActions.Add(new ScheduledAction(id, dspTime, action));
            return id;
        }

        public int Schedule(AudioSyncClock clock, AudioSyncTiming timing, double inputGraceSeconds, Action action)
        {
            if (clock == null)
            {
                return Schedule(AudioSettings.dspTime, action);
            }

            return Schedule(clock.ResolveTiming(timing, inputGraceSeconds), action);
        }

        public void Cancel(int id)
        {
            scheduledActions.RemoveAll(item => item.Id == id);
        }

        public void Clear()
        {
            scheduledActions.Clear();
        }

        public void Tick(double currentDspTime)
        {
            for (int i = scheduledActions.Count - 1; i >= 0; i--)
            {
                if (currentDspTime >= scheduledActions[i].DspTime)
                {
                    scheduledActions[i].Action?.Invoke();
                    scheduledActions.RemoveAt(i);
                }
            }
        }

        private readonly struct ScheduledAction
        {
            public readonly int Id;
            public readonly double DspTime;
            public readonly Action Action;

            public ScheduledAction(int id, double dspTime, Action action)
            {
                Id = id;
                DspTime = dspTime;
                Action = action;
            }
        }
    }
}
