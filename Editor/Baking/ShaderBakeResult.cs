using System;
using System.Collections.Generic;
using UnityEngine;
namespace Serebrennikov {
    public sealed class ShaderBakeResult {
        public bool Success;
        public string OutputPath;
        public Texture2D CreatedAsset;
        public string ErrorMessage;

        public static ShaderBakeResult Failed(string errorMessage) {
            return new ShaderBakeResult {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        public static ShaderBakeResult Completed(string outputPath, Texture2D createdAsset) {
            return new ShaderBakeResult {
                Success = true,
                OutputPath = outputPath,
                CreatedAsset = createdAsset
            };
        }
    }
}