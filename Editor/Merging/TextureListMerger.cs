#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Serebrennikov {
    public sealed class TextureListMerger {
        public TextureListMergeResult Merge(TextureListMergeSettings settings) {
            if (settings == null) {
                return TextureListMergeResult.Failed("Merge settings are null.");
            }
            settings.EnsureLayerCount();
            TextureListMergeValidator validator = new();
            ShaderBakeValidationResult validation = validator.Validate(settings);
            if (validation.HasErrors) {
                return TextureListMergeResult.Failed(validation.Messages[0].Text);
            }
            string outputFolderPath = settings.OutputFolderPath.Replace('\\', '/');
            if (!Directory.Exists(outputFolderPath)) {
                Directory.CreateDirectory(outputFolderPath);
            }
            int outputCount = settings.Layers[0].Textures.Count;
            List<Texture2D> createdAssets = new(outputCount);
            try {
                for (int textureIndex = 0; textureIndex < outputCount; textureIndex++) {
                    Texture2D mergedTexture = CreateMergedTexture(settings, textureIndex);
                    string fileName = $"{settings.OutputNamePrefix}_{textureIndex:D4}.png";
                    string outputPath = Path.Combine(outputFolderPath, fileName).Replace('\\', '/');
                    if (File.Exists(outputPath) && !settings.OverwriteExisting) {
                        UnityEngine.Object.DestroyImmediate(mergedTexture);
                        return TextureListMergeResult.Failed($"Target file already exists and overwrite is disabled: {outputPath}");
                    }
                    WriteTextureAsset(settings, mergedTexture, outputPath);
                    UnityEngine.Object.DestroyImmediate(mergedTexture);
                    Texture2D createdAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
                    if (createdAsset != null) {
                        createdAssets.Add(createdAsset);
                    }
                }
                AssetDatabase.Refresh();
                return TextureListMergeResult.Completed(outputFolderPath, createdAssets);
            }
            catch (Exception exception) {
                return TextureListMergeResult.Failed(exception.Message);
            }
        }
        static Texture2D CreateMergedTexture(TextureListMergeSettings settings, int textureIndex) {
            Texture2D firstTexture = settings.Layers[0].Textures[textureIndex];
            int width = firstTexture.width;
            int height = firstTexture.height;
            Color[] accumulator = new Color[width * height];
            Color[] sourcePixels = null;
            for (int layerIndex = 0; layerIndex < settings.Layers.Count; layerIndex++) {
                Texture2D layerTexture = settings.Layers[layerIndex].Textures[textureIndex];
                sourcePixels = ReadTexturePixels(layerTexture);
                for (int pixelIndex = 0; pixelIndex < accumulator.Length; pixelIndex++) {
                    accumulator[pixelIndex] = AlphaBlend(accumulator[pixelIndex], sourcePixels[pixelIndex]);
                }
            }
            Texture2D mergedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            mergedTexture.name = $"{settings.OutputNamePrefix}_{textureIndex:D4}";
            mergedTexture.filterMode = settings.FilterMode;
            mergedTexture.wrapMode = settings.WrapMode;
            mergedTexture.SetPixels(accumulator);
            mergedTexture.Apply(false, false);
            return mergedTexture;
        }
        static Color[] ReadTexturePixels(Texture texture) {
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture renderTexture = null;
            Texture2D readableTexture = null;
            try {
                renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                Graphics.Blit(texture, renderTexture);
                RenderTexture.active = renderTexture;
                readableTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, false);
                readableTexture.ReadPixels(new Rect(0.0f, 0.0f, texture.width, texture.height), 0, 0, false);
                readableTexture.Apply(false, false);
                return readableTexture.GetPixels();
            }
            finally {
                RenderTexture.active = previousActive;
                if (renderTexture != null) {
                    RenderTexture.ReleaseTemporary(renderTexture);
                }
                if (readableTexture != null) {
                    UnityEngine.Object.DestroyImmediate(readableTexture);
                }
            }
        }
        static Color AlphaBlend(Color destination, Color source) {
            float outputAlpha = source.a + destination.a * (1.0f - source.a);
            if (outputAlpha <= 0.0f) {
                return Color.clear;
            }
            Color output = new();
            output.r = (source.r * source.a + destination.r * destination.a * (1.0f - source.a)) / outputAlpha;
            output.g = (source.g * source.a + destination.g * destination.a * (1.0f - source.a)) / outputAlpha;
            output.b = (source.b * source.a + destination.b * destination.a * (1.0f - source.a)) / outputAlpha;
            output.a = outputAlpha;
            return output;
        }
        static void WriteTextureAsset(TextureListMergeSettings settings, Texture2D texture, string outputPath) {
            byte[] pngData = texture.EncodeToPNG();
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
        }
    }
}
#endif
