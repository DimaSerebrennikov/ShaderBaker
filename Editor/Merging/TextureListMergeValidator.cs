#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
namespace Serebrennikov {
    public sealed class TextureListMergeValidator {
        public ShaderBakeValidationResult Validate(TextureListMergeSettings settings) {
            ShaderBakeValidationResult result = new();
            if (settings == null) {
                result.AddError("Merge settings are null.");
                return result;
            }
            settings.EnsureLayerCount();
            if (settings.ListCount <= 0) {
                result.AddError("List count must be greater than 0.");
                return result;
            }
            if (settings.Layers.Count == 0) {
                result.AddError("There are no lists to merge.");
                return result;
            }
            int expectedCount = -1;
            int referenceWidth = -1;
            int referenceHeight = -1;
            for (int layerIndex = 0; layerIndex < settings.Layers.Count; layerIndex++) {
                TextureListMergeLayer layer = settings.Layers[layerIndex];
                if (layer == null) {
                    result.AddError($"List {layerIndex + 1} is null.");
                    continue;
                }
                if (layer.Textures == null || layer.Textures.Count == 0) {
                    result.AddError($"List {layerIndex + 1} does not contain textures.");
                    continue;
                }
                if (expectedCount < 0) {
                    expectedCount = layer.Textures.Count;
                } else if (layer.Textures.Count != expectedCount) {
                    result.AddError($"All lists must contain the same number of textures. List {layerIndex + 1} contains {layer.Textures.Count}, expected {expectedCount}.");
                }
                for (int textureIndex = 0; textureIndex < layer.Textures.Count; textureIndex++) {
                    Texture2D texture = layer.Textures[textureIndex];
                    if (texture == null) {
                        result.AddError($"List {layerIndex + 1}, element {textureIndex + 1} is empty.");
                        continue;
                    }
                    if (referenceWidth < 0) {
                        referenceWidth = texture.width;
                        referenceHeight = texture.height;
                    } else if (texture.width != referenceWidth || texture.height != referenceHeight) {
                        result.AddError($"All textures must have the same size. Mismatch at list {layerIndex + 1}, element {textureIndex + 1}: {texture.width}x{texture.height}, expected {referenceWidth}x{referenceHeight}.");
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(settings.OutputFolderPath)) {
                result.AddError("Merge output folder is empty.");
            } else {
                if (!settings.OutputFolderPath.StartsWith("Assets/", StringComparison.Ordinal) && !string.Equals(settings.OutputFolderPath, "Assets", StringComparison.Ordinal)) {
                    result.AddError("Merge output folder must start with \"Assets/\".");
                }
                if (Path.HasExtension(settings.OutputFolderPath)) {
                    result.AddError("Merge output path must be a folder, not a file.");
                }
                if (Directory.Exists(settings.OutputFolderPath) && !settings.OverwriteExisting) {
                    string[] existingFiles = Directory.GetFiles(settings.OutputFolderPath, "*.png", SearchOption.TopDirectoryOnly);
                    if (existingFiles.Length > 0) {
                        result.AddError("Merge output folder already contains PNG files and overwrite is disabled.");
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(settings.OutputNamePrefix)) {
                result.AddError("Merge output name prefix is empty.");
            }
            return result;
        }
    }
}
#endif
