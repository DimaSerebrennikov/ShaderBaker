using System;
using System.Collections.Generic;
using UnityEngine;
namespace Serebrennikov {
    public sealed class ShaderAnimationBakeResult {
        public bool Success;
        public string OutputFolderPath;
        public IReadOnlyList<Texture2D> CreatedAssets;
        public string ErrorMessage;
        public static ShaderAnimationBakeResult Failed(string errorMessage) {
            return new ShaderAnimationBakeResult {
                Success = false,
                ErrorMessage = errorMessage,
                CreatedAssets = Array.Empty<Texture2D>()
            };
        }
        public static ShaderAnimationBakeResult Completed(string outputFolderPath, IReadOnlyList<Texture2D> createdAssets) {
            return new ShaderAnimationBakeResult {
                Success = true,
                OutputFolderPath = outputFolderPath,
                CreatedAssets = createdAssets ?? Array.Empty<Texture2D>()
            };
        }
    }
}
