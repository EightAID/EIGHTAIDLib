using System;
using UnityEngine;

namespace EightAID.EIGHTAIDLib.Effect
{
    [Serializable]
    public struct PostProcessParameterValue
    {
        [SerializeField] private PostProcessParameterType parameterType;
        [SerializeField] private float value;

        public PostProcessParameterValue(PostProcessParameterType parameterType, float value)
        {
            this.parameterType = parameterType;
            this.value = value;
        }

        public PostProcessParameterType ParameterType => parameterType;
        public float Value => value;
    }
}
