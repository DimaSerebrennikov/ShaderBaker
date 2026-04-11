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
                    if (File.Exists(outputPath) && !settings.OverwriteExisting) {
                        return ShaderAnimationBakeResult.Failed($"Target file already exists and overwrite is disabled: {outputPath}");
                    }
                    ShaderBakeResult frameResult = BakeFrame(settings, outputPath, fileName);
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
        static ShaderBakeResult BakeFrame(ShaderAnimationBakeSettings settings, string outputPath, string textureName) {
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture renderTexture = null;
            Texture2D bakedTexture = null;
            try {
                renderTexture = new RenderTexture(settings.Width, settings.Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                renderTexture.name = "Shader animation bake render texture";
                renderTexture.filterMode = settings.FilterMode;
                renderTexture.wrapMode = settings.WrapMode;
                renderTexture.Create();
                Graphics.Blit(Texture2D.whiteTexture, renderTexture, settings.Material, 0);
                RenderTexture.active = renderTexture;
                bakedTexture = new Texture2D(settings.Width, settings.Height, TextureFormat.RGBA32, false, false);
                bakedTexture.name = Path.GetFileNameWithoutExtension(textureName);
                bakedTexture.filterMode = settings.FilterMode;
                bakedTexture.wrapMode = settings.WrapMode;
                bakedTexture.ReadPixels(new Rect(0.0f, 0.0f, settings.Width, settings.Height), 0, 0, false);
                bakedTexture.Apply(false, false);
                byte[] pngData = bakedTexture.EncodeToPNG();
                File.WriteAllBytes(outputPath, pngData);
                AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
                TextureImporter importer = AssetImporter.GetAtPath(outputPath) as TextureImporter;
                if (importer != null) {
                    importer.textureType = TextureImporterType.Default;
                    importer.alphaSource = TextureImporterAlphaSource.FromInput;
                    importer.alphaIsTransparency = true;
                    importer.sRGBTexture = true;
                    importer.mipmapEnabled = false;
                    importer.isReadable = false;
                    importer.wrapMode = settings.WrapMode;
                    importer.filterMode = settings.FilterMode;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
                Texture2D createdAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
                return ShaderBakeResult.Completed(outputPath, createdAsset);
            }
            catch (Exception exception) {
                return ShaderBakeResult.Failed(exception.Message);
            }
            finally {
                RenderTexture.active = previousActive;
                if (renderTexture != null) {
                    renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }
                if (bakedTexture != null) {
                    UnityEngine.Object.DestroyImmediate(bakedTexture);
                }
            }
        }
    }
}
#endif
