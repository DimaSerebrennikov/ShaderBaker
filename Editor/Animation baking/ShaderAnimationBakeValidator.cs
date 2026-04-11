#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Serebrennikov {
    public sealed class ShaderAnimationBakeValidator {
        const string BakeTimePropertyName = "_BakeTime";
        const string DurationPropertyName = "_Duration";
        public ShaderBakeValidationResult Validate(ShaderAnimationBakeSettings settings) {
            ShaderBakeValidationResult result = new();
            if (settings == null) {
                result.AddError("Animation settings are null.");
                return result;
            }
            if (settings.Material == null) {
                result.AddError("Material is not selected.");
                return result;
            }
            if (settings.Material.shader == null) {
                result.AddError("Material has no shader.");
                return result;
            }
            if (!settings.Material.HasProperty(BakeTimePropertyName)) {
                result.AddError("Shader does not contain _BakeTime.");
            }
            if (!settings.Material.HasProperty(DurationPropertyName)) {
                result.AddError("Shader does not contain _Duration.");
            }
            if (settings.Material.HasProperty(DurationPropertyName)) {
                float duration = settings.Material.GetFloat(DurationPropertyName);
                if (duration <= 0.0f) {
                    result.AddError("Shader _Duration must be greater than 0.");
                }
            }
            if (settings.Width <= 0) {
                result.AddError("Width must be greater than 0.");
            }
            if (settings.Height <= 0) {
                result.AddError("Height must be greater than 0.");
            }
            if (settings.FramesPerSecond <= 0) {
                result.AddError("Animation FPS must be greater than 0.");
            }
            if (string.IsNullOrWhiteSpace(settings.OutputFolderPath)) {
                result.AddError("Animation output folder is empty.");
            } else {
                if (!settings.OutputFolderPath.StartsWith("Assets/", StringComparison.Ordinal) && !string.Equals(settings.OutputFolderPath, "Assets", StringComparison.Ordinal)) {
                    result.AddError("Animation output folder must start with \"Assets/\".");
                }
                if (Path.HasExtension(settings.OutputFolderPath)) {
                    result.AddError("Animation output path must be a folder, not a file.");
                }
                if (Directory.Exists(settings.OutputFolderPath) && !settings.OverwriteExisting) {
                    string[] existingFiles = Directory.GetFiles(settings.OutputFolderPath, "*.png", SearchOption.TopDirectoryOnly);
                    if (existingFiles.Length > 0) {
                        result.AddError("Animation output folder already contains PNG files and overwrite is disabled.");
                    }
                }
            }
            return result;
        }
    }
}
#endif
