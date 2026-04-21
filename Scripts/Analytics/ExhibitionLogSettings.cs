using UnityEngine;

namespace EightAID.EIGHTAIDLib.Analytics
{
    [CreateAssetMenu(fileName = "ExhibitionLogSettings", menuName = "EightAID/Analytics/Exhibition Log Settings")]
    public sealed class ExhibitionLogSettings : ScriptableObject
    {
        public bool enabled = true;
        public bool verboseEvents = true;
        public bool writeJsonl = true;
        public bool writeCsv = true;
        public bool writeSessionSummaryCsv = true;
        public bool writeEventCsv = true;
        public int maxSessionFiles = 200;
    }
}
