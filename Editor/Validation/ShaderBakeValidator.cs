using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Serebrennikov {
    public sealed class ShaderBakeValidator {
        public ShaderBakeValidationResult Validate(ShaderBakeSettings settings) {
            ShaderBakeValidationResult result = new();
            if (settings == null) {
                result.AddError("Settings are null.");
                return result;
            }
            if (settings.Material == null) {
                result.AddError("Material is not selected.");
                return result;
            }
            if (settings.Material.shader == null) {
                result.AddError("Material has no shader.");
            }
            if (settings.Width <= 0) {
                result.AddError("Width must be greater than 0.");
            }
            if (settings.Height <= 0) {
                result.AddError("Height must be greater than 0.");
            }
            if (string.IsNullOrWhiteSpace(settings.OutputPath)) {
                result.AddError("Output path is empty.");
            } else {
                if (!settings.OutputPath.StartsWith("Assets/", StringComparison.Ordinal)) {
                    result.AddError("Output path must start with \"Assets/\".");
                }
                if (!settings.OutputPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) {
                    result.AddError("Output file must be a .png.");
                }
                if (File.Exists(settings.OutputPath) && !settings.OverwriteExisting) {
                    result.AddError("Target file already exists and overwrite is disabled.");
                }
            }
            return result;
        }
    }
}
