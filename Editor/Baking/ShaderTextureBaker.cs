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
                renderTexture = new RenderTexture(settings.Width, settings.Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                renderTexture.name = "Shader bake render texture";
                renderTexture.filterMode = settings.FilterMode;
                renderTexture.wrapMode = settings.WrapMode;
                renderTexture.Create();
                blackTexture = RenderMaterialToTexture(settings, renderTexture, Color.black);
                whiteTexture = RenderMaterialToTexture(settings, renderTexture, Color.white);
                bakedTexture = new Texture2D(settings.Width, settings.Height, TextureFormat.RGBA32, false, false);
                bakedTexture.name = Path.GetFileNameWithoutExtension(settings.OutputPath);
                bakedTexture.filterMode = settings.FilterMode;
                bakedTexture.wrapMode = settings.WrapMode;
                Color32[] blackPixels = blackTexture.GetPixels32();
                Color32[] whitePixels = whiteTexture.GetPixels32();
                Color32[] resultPixels = new Color32[blackPixels.Length];
                for (int i = 0; i < resultPixels.Length; i++) {
                    float br = blackPixels[i].r / 255.0f;
                    float bg = blackPixels[i].g / 255.0f;
                    float bb = blackPixels[i].b / 255.0f;
                    float wr = whitePixels[i].r / 255.0f;
                    float wg = whitePixels[i].g / 255.0f;
                    float wb = whitePixels[i].b / 255.0f;
                    float alphaR = 1.0f - Mathf.Clamp01(wr - br);
                    float alphaG = 1.0f - Mathf.Clamp01(wg - bg);
                    float alphaB = 1.0f - Mathf.Clamp01(wb - bb);
                    float alpha = (alphaR + alphaG + alphaB) / 3.0f;
                    alpha = Mathf.Clamp01(alpha);
                    float r = 0.0f;
                    float g = 0.0f;
                    float b = 0.0f;
                    if (alpha > 0.0001f) {
                        r = Mathf.Clamp01(br / alpha);
                        g = Mathf.Clamp01(bg / alpha);
                        b = Mathf.Clamp01(bb / alpha);
                    }
                    resultPixels[i] = new Color(r, g, b, alpha);
                }
                bakedTexture.SetPixels32(resultPixels);
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
            Texture2D texture = new Texture2D(settings.Width, settings.Height, TextureFormat.RGBA32, false, false);
            texture.filterMode = settings.FilterMode;
            texture.wrapMode = settings.WrapMode;
            texture.ReadPixels(new Rect(0.0f, 0.0f, settings.Width, settings.Height), 0, 0, false);
            texture.Apply(false, false);
            RenderTexture.active = previousActive;
            return texture;
        }
    }
}
