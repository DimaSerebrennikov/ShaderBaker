using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Serebrennikov {
    public sealed class ShaderTextureBaker {
        public ShaderBakeResult Bake(ShaderBakeSettings settings) {
            if (settings == null) {
                return ShaderBakeResult.Failed("Settings are null.");
            }
            string directory = Path.GetDirectoryName(settings.OutputPath);
            if (string.IsNullOrEmpty(directory)) {
                return ShaderBakeResult.Failed("Incorrect output path.");
            }
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture renderTexture = null;
            Texture2D blackTexture = null;
            Texture2D whiteTexture = null;
            Texture2D bakedTexture = null;
            try {
                renderTexture = new RenderTexture(settings.Width, settings.Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                renderTexture.name = "Shader bake render texture";
                renderTexture.filterMode = settings.FilterMode;
                renderTexture.wrapMode = settings.WrapMode;
                renderTexture.useMipMap = false;
                renderTexture.autoGenerateMips = false;
                renderTexture.Create();
                blackTexture = RenderMaterialToTexture(settings, renderTexture, Color.black);
                whiteTexture = RenderMaterialToTexture(settings, renderTexture, Color.white);
                Color[] blackPixels = blackTexture.GetPixels();
                Color[] whitePixels = whiteTexture.GetPixels();
                Color[] resultPixels = new Color[blackPixels.Length];
                for (int i = 0; i < resultPixels.Length; i++) {
                    Color black = blackPixels[i];
                    Color white = whitePixels[i];
                    float alphaR = 1.0f - Mathf.Clamp01(white.r - black.r);
                    float alphaG = 1.0f - Mathf.Clamp01(white.g - black.g);
                    float alphaB = 1.0f - Mathf.Clamp01(white.b - black.b);
                    float alpha = (alphaR + alphaG + alphaB) / 3.0f;
                    alpha = Mathf.Clamp01(alpha);
                    float r = 0.0f;
                    float g = 0.0f;
                    float b = 0.0f;
                    if (alpha > 0.00001f) {
                        r = Mathf.Clamp01(black.r / alpha);
                        g = Mathf.Clamp01(black.g / alpha);
                        b = Mathf.Clamp01(black.b / alpha);
                    }
                    resultPixels[i] = new Color(r, g, b, alpha);
                }
                bool projectIsLinear = QualitySettings.activeColorSpace == ColorSpace.Linear;
                if (projectIsLinear) {
                    for (int i = 0; i < resultPixels.Length; i++) {
                        Color color = resultPixels[i];
                        color.r = Mathf.LinearToGammaSpace(color.r);
                        color.g = Mathf.LinearToGammaSpace(color.g);
                        color.b = Mathf.LinearToGammaSpace(color.b);
                        resultPixels[i] = color;
                    }
                }
                bakedTexture = new Texture2D(settings.Width, settings.Height, TextureFormat.RGBA32, false, false);
                bakedTexture.name = Path.GetFileNameWithoutExtension(settings.OutputPath);
                bakedTexture.filterMode = settings.FilterMode;
                bakedTexture.wrapMode = settings.WrapMode;
                bakedTexture.SetPixels(resultPixels);
                bakedTexture.Apply(false, false);
                byte[] pngData = bakedTexture.EncodeToPNG();
                File.WriteAllBytes(settings.OutputPath, pngData);
                AssetDatabase.ImportAsset(settings.OutputPath, ImportAssetOptions.ForceUpdate);
                TextureImporter importer = AssetImporter.GetAtPath(settings.OutputPath) as TextureImporter;
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
                AssetDatabase.Refresh();
                Texture2D createdAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(settings.OutputPath);
                return ShaderBakeResult.Completed(settings.OutputPath, createdAsset);
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
                if (blackTexture != null) {
                    UnityEngine.Object.DestroyImmediate(blackTexture);
                }
                if (whiteTexture != null) {
                    UnityEngine.Object.DestroyImmediate(whiteTexture);
                }
                if (bakedTexture != null) {
                    UnityEngine.Object.DestroyImmediate(bakedTexture);
                }
            }
        }
        private static Texture2D RenderMaterialToTexture(ShaderBakeSettings settings, RenderTexture renderTexture, Color clearColor) {
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, clearColor);
            Graphics.Blit(Texture2D.whiteTexture, renderTexture, settings.Material, 0);
            Texture2D texture = new Texture2D(settings.Width, settings.Height, TextureFormat.RGBA32, false, true);
            texture.filterMode = settings.FilterMode;
            texture.wrapMode = settings.WrapMode;
            texture.ReadPixels(new Rect(0.0f, 0.0f, settings.Width, settings.Height), 0, 0, false);
            texture.Apply(false, false);
            RenderTexture.active = previousActive;
            return texture;
        }
    }
}
