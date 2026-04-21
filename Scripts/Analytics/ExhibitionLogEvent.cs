using System;
using UnityEngine;

namespace EightAID.EIGHTAIDLib.Analytics
{
    [Serializable]
    public sealed class ExhibitionLogEvent
    {
        public string sessionId;
        public string machineId;
        public string buildVersion;
        public string exhibitionDate;
        public string eventTimeUtc;
        public string eventTimeLocal;
        public float elapsedSeconds;
        public string eventName;
        public string sceneName;
        public int sceneBuildIndex;
        public string stageId;
        public string eventTitle;
        public string enemyId;
        public string enemyName;
        public int turnCount;
        public int value;
        public string result;
        public string reason;
        public string payload;

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }
}
