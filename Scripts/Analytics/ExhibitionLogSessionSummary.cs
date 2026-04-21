using System;
using UnityEngine;

namespace EightAID.EIGHTAIDLib.Analytics
{
    [Serializable]
    public sealed class ExhibitionLogSessionSummary
    {
        public string sessionId;
        public string machineId;
        public string buildVersion;
        public string exhibitionDate;
        public string saveSlotId;
        public string startedAtUtc;
        public string startedAtLocal;
        public string endedAtUtc;
        public string endedAtLocal;
        public float elapsedSeconds;
        public string startReason;
        public string endReason;
        public int isCleared;
        public int returnedToTitle;
        public int gameOverCount;
        public int retryCurrentStageCount;
        public int retryFromMapStartCount;
        public int battleCount;
        public int stageClearCount;
        public int menuOpenCount;
        public string lastScene;
        public string lastStageId;
        public string lastEventTitle;

        public string ToJson(bool prettyPrint = true)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }
    }
}
