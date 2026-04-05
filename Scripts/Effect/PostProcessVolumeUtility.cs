using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EightAID.EIGHTAIDLib.Effect
{
    public static class PostProcessVolumeUtility
    {
        public static bool TryCaptureRawValue(VolumeProfile profile, PostProcessParameterType parameterType, out float rawValue)
        {
            rawValue = default;
            if (profile == null)
                return false;

            switch (parameterType)
            {
                case PostProcessParameterType.Grayscale:
                    if (!profile.TryGet(out ColorAdjustments colorAdjustments))
                        return false;

                    rawValue = colorAdjustments.saturation.value;
                    return true;

                case PostProcessParameterType.PostExposure:
                    if (!profile.TryGet(out colorAdjustments))
                        return false;

                    rawValue = colorAdjustments.postExposure.value;
                    return true;

                case PostProcessParameterType.Contrast:
                    if (!profile.TryGet(out colorAdjustments))
                        return false;

                    rawValue = colorAdjustments.contrast.value;
                    return true;

                case PostProcessParameterType.BloomIntensity:
                    if (!profile.TryGet(out Bloom bloom))
                        return false;

                    rawValue = bloom.intensity.value;
                    return true;

                case PostProcessParameterType.VignetteIntensity:
                    if (!profile.TryGet(out Vignette vignette))
                        return false;

                    rawValue = vignette.intensity.value;
                    return true;

                case PostProcessParameterType.ChromaticAberrationIntensity:
                    if (!profile.TryGet(out ChromaticAberration chromaticAberration))
                        return false;

                    rawValue = chromaticAberration.intensity.value;
                    return true;

                case PostProcessParameterType.LensDistortionIntensity:
                    if (!profile.TryGet(out LensDistortion lensDistortion))
                        return false;

                    rawValue = lensDistortion.intensity.value;
                    return true;
            }

            return false;
        }

        public static bool TrySetValue(VolumeProfile profile, PostProcessParameterType parameterType, float value, float baselineRawValue, bool autoCreateMissingOverrides = true)
        {
            if (profile == null)
                return false;

            switch (parameterType)
            {
                case PostProcessParameterType.Grayscale:
                {
                    ColorAdjustments colorAdjustments = GetOrCreate<ColorAdjustments>(profile, autoCreateMissingOverrides);
                    if (colorAdjustments == null)
                        return false;

                    colorAdjustments.saturation.overrideState = true;
                    colorAdjustments.saturation.value = Mathf.Lerp(baselineRawValue, -100f, Mathf.Clamp01(value));
                    return true;
                }

                case PostProcessParameterType.PostExposure:
                {
                    ColorAdjustments colorAdjustments = GetOrCreate<ColorAdjustments>(profile, autoCreateMissingOverrides);
                    if (colorAdjustments == null)
                        return false;

                    colorAdjustments.postExposure.overrideState = true;
                    colorAdjustments.postExposure.value = Mathf.Clamp(value, -5f, 5f);
                    return true;
                }

                case PostProcessParameterType.Contrast:
                {
                    ColorAdjustments colorAdjustments = GetOrCreate<ColorAdjustments>(profile, autoCreateMissingOverrides);
                    if (colorAdjustments == null)
                        return false;

                    colorAdjustments.contrast.overrideState = true;
                    colorAdjustments.contrast.value = Mathf.Clamp(value, -100f, 100f);
                    return true;
                }

                case PostProcessParameterType.BloomIntensity:
                {
                    Bloom bloom = GetOrCreate<Bloom>(profile, autoCreateMissingOverrides);
                    if (bloom == null)
                        return false;

                    bloom.intensity.overrideState = true;
                    bloom.intensity.value = Mathf.Max(0f, value);
                    return true;
                }

                case PostProcessParameterType.VignetteIntensity:
                {
                    Vignette vignette = GetOrCreate<Vignette>(profile, autoCreateMissingOverrides);
                    if (vignette == null)
                        return false;

                    vignette.intensity.overrideState = true;
                    vignette.intensity.value = Mathf.Clamp01(value);
                    return true;
                }

                case PostProcessParameterType.ChromaticAberrationIntensity:
                {
                    ChromaticAberration chromaticAberration = GetOrCreate<ChromaticAberration>(profile, autoCreateMissingOverrides);
                    if (chromaticAberration == null)
                        return false;

                    chromaticAberration.intensity.overrideState = true;
                    chromaticAberration.intensity.value = Mathf.Clamp01(value);
                    return true;
                }

                case PostProcessParameterType.LensDistortionIntensity:
                {
                    LensDistortion lensDistortion = GetOrCreate<LensDistortion>(profile, autoCreateMissingOverrides);
                    if (lensDistortion == null)
                        return false;

                    lensDistortion.intensity.overrideState = true;
                    lensDistortion.intensity.value = Mathf.Clamp(value, -1f, 1f);
                    return true;
                }
            }

            return false;
        }

        private static T GetOrCreate<T>(VolumeProfile profile, bool autoCreateMissingOverrides) where T : VolumeComponent
        {
            if (profile.TryGet(out T component))
                return component;

            if (!autoCreateMissingOverrides)
                return null;

            return profile.Add<T>(true);
        }
    }
}
