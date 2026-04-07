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
            PostProcessParameterType.BlurIntensity,
        };

        public static float GetMinValue(PostProcessParameterType parameterType)
        {
            switch (parameterType)
            {
                case PostProcessParameterType.Grayscale:
                case PostProcessParameterType.VignetteIntensity:
                case PostProcessParameterType.ChromaticAberrationIntensity:
                case PostProcessParameterType.BlurIntensity:
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
                case PostProcessParameterType.BlurIntensity:
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
                    return "Grayscale";
                case PostProcessParameterType.PostExposure:
                    return "Post Exposure";
                case PostProcessParameterType.Contrast:
                    return "Contrast";
                case PostProcessParameterType.BloomIntensity:
                    return "Bloom";
                case PostProcessParameterType.VignetteIntensity:
                    return "Vignette";
                case PostProcessParameterType.ChromaticAberrationIntensity:
                    return "Chromatic Aberration";
                case PostProcessParameterType.LensDistortionIntensity:
                    return "Lens Distortion";
                case PostProcessParameterType.BlurIntensity:
                    return "Blur";
            }

            return parameterType.ToString();
        }

        public static float Clamp(PostProcessParameterType parameterType, float value)
        {
            return Mathf.Clamp(value, GetMinValue(parameterType), GetMaxValue(parameterType));
        }
    }
}
