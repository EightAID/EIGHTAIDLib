using UnityEngine;

namespace EightAID.EIGHTAIDLib.Effect
{
    public static class PostProcessParameterMetadataUtility
    {
        public static PostProcessParameterType[] AllParameterTypes { get; } =
        {
            PostProcessParameterType.Grayscale,
            PostProcessParameterType.PostExposure,
            PostProcessParameterType.Contrast,
            PostProcessParameterType.BloomIntensity,
            PostProcessParameterType.VignetteIntensity,
            PostProcessParameterType.ChromaticAberrationIntensity,
            PostProcessParameterType.LensDistortionIntensity,
        };

        public static float GetMinValue(PostProcessParameterType parameterType)
        {
            switch (parameterType)
            {
                case PostProcessParameterType.Grayscale:
                case PostProcessParameterType.VignetteIntensity:
                case PostProcessParameterType.ChromaticAberrationIntensity:
                    return 0f;

                case PostProcessParameterType.PostExposure:
                    return -5f;

                case PostProcessParameterType.Contrast:
                    return -100f;

                case PostProcessParameterType.BloomIntensity:
                    return 0f;

                case PostProcessParameterType.LensDistortionIntensity:
                    return -1f;
            }

            return 0f;
        }

        public static float GetMaxValue(PostProcessParameterType parameterType)
        {
            switch (parameterType)
            {
                case PostProcessParameterType.Grayscale:
                case PostProcessParameterType.VignetteIntensity:
                case PostProcessParameterType.ChromaticAberrationIntensity:
                    return 1f;

                case PostProcessParameterType.PostExposure:
                    return 5f;

                case PostProcessParameterType.Contrast:
                    return 100f;

                case PostProcessParameterType.BloomIntensity:
                    return 10f;

                case PostProcessParameterType.LensDistortionIntensity:
                    return 1f;
            }

            return 1f;
        }

        public static string GetDisplayName(PostProcessParameterType parameterType)
        {
            switch (parameterType)
            {
                case PostProcessParameterType.Grayscale:
                    return "白黒";
                case PostProcessParameterType.PostExposure:
                    return "露出";
                case PostProcessParameterType.Contrast:
                    return "コントラスト";
                case PostProcessParameterType.BloomIntensity:
                    return "ブルーム";
                case PostProcessParameterType.VignetteIntensity:
                    return "ビネット";
                case PostProcessParameterType.ChromaticAberrationIntensity:
                    return "色収差";
                case PostProcessParameterType.LensDistortionIntensity:
                    return "レンズ歪み";
            }

            return parameterType.ToString();
        }

        public static float Clamp(PostProcessParameterType parameterType, float value)
        {
            return Mathf.Clamp(value, GetMinValue(parameterType), GetMaxValue(parameterType));
        }
    }
}
