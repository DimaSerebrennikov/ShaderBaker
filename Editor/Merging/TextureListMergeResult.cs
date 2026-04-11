using System;
using System.Collections.Generic;
using UnityEngine;
namespace Serebrennikov {
    public sealed class TextureListMergeResult {
        public bool Success;
        public string OutputFolderPath;
        public IReadOnlyList<Texture2D> CreatedAssets;
        public string ErrorMessage;

        public static TextureListMergeResult Failed(string errorMessage) {
            return new TextureListMergeResult {
                Success = false,
                ErrorMessage = errorMessage,
                CreatedAssets = Array.Empty<Texture2D>()
            };
        }

        public static TextureListMergeResult Completed(string outputFolderPath, IReadOnlyList<Texture2D> createdAssets) {
            return new TextureListMergeResult {
                Success = true,
                OutputFolderPath = outputFolderPath,
                CreatedAssets = createdAssets ?? Array.Empty<Texture2D>()
            };
        }
    }
}
