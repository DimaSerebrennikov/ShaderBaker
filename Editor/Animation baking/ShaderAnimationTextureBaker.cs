#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Serebrennikov {
    public sealed class ShaderAnimationTextureBaker {
        const string BakeTimePropertyName = "_BakeTime";
        const string DurationPropertyName = "_Duration";
        readonly ShaderTextureBaker shaderTextureBaker;
        public ShaderAnimationTextureBaker(ShaderTextureBaker shaderTextureBaker) {
            this.shaderTextureBaker = shaderTextureBaker ?? throw new ArgumentNullException(nameof(shaderTextureBaker));
        }
        public ShaderAnimationBakeResult Bake(ShaderAnimationBakeSettings settings) {
            if (settings == null) {
                return ShaderAnimationBakeResult.Failed("Animation settings are null.");
            }
            if (settings.Material == null) {
                return ShaderAnimationBakeResult.Failed("Material is not selected.");
            }
            if (!settings.Material.HasProperty(BakeTimePropertyName)) {
                return ShaderAnimationBakeResult.Failed("Shader does not contain _BakeTime.");
            }
            if (!settings.Material.HasProperty(DurationPropertyName)) {
                return ShaderAnimationBakeResult.Failed("Shader does not contain _Duration.");
            }
            float duration = settings.Material.GetFloat(DurationPropertyName);
            if (duration <= 0.0f) {
                return ShaderAnimationBakeResult.Failed("Shader _Duration must be greater than 0.");
            }
            if (settings.FramesPerSecond <= 0) {
                return ShaderAnimationBakeResult.Failed("Animation FPS must be greater than 0.");
            }
            string outputFolderPath = settings.OutputFolderPath?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(outputFolderPath)) {
                return ShaderAnimationBakeResult.Failed("Animation output folder is empty.");
            }
            if (!Directory.Exists(outputFolderPath)) {
                Directory.CreateDirectory(outputFolderPath);
            }
            string filePrefix = Path.GetFileName(outputFolderPath);
            if (string.IsNullOrWhiteSpace(filePrefix)) {
                filePrefix = "frame";
            }
            int frameCount = Mathf.Max(1, Mathf.CeilToInt(duration * settings.FramesPerSecond));
            float previousBakeTime = settings.Material.GetFloat(BakeTimePropertyName);
            List<Texture2D> createdAssets = new(frameCount);
            try {
                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++) {
                    float normalizedTime = frameCount == 1 ? 0.0f : frameIndex / (float)(frameCount - 1);
                    float bakeTime = normalizedTime * duration;
                    settings.Material.SetFloat(BakeTimePropertyName, bakeTime);
                    string fileName = $"{filePrefix}_{frameIndex:D4}.png";
                    string outputPath = Path.Combine(outputFolderPath, fileName).Replace('\\', '/');
                    ShaderBakeSettings frameSettings = settings.CreateFrameBakeSettings(outputPath);
                    ShaderBakeResult frameResult = shaderTextureBaker.Bake(frameSettings);
                    if (!frameResult.Success) {
                        return ShaderAnimationBakeResult.Failed(frameResult.ErrorMessage);
                    }
                    if (frameResult.CreatedAsset != null) {
                        createdAssets.Add(frameResult.CreatedAsset);
                    }
                }
                AssetDatabase.Refresh();
                return ShaderAnimationBakeResult.Completed(outputFolderPath, createdAssets);
            }
            catch (Exception exception) {
                return ShaderAnimationBakeResult.Failed(exception.Message);
            }
            finally {
                settings.Material.SetFloat(BakeTimePropertyName, previousBakeTime);
            }
        }
    }
}
#endif
