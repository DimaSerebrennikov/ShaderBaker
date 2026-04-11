#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Serebrennikov {
    [Serializable]
    public sealed class ShaderAnimationBakeSettings {
        public Material Material;
        public int Width = 512;
        public int Height = 512;
        public string OutputFolderPath = "Assets/Baked by ShaderBaker";
        public bool OverwriteExisting = true;
        public bool SelectCreatedAsset = true;
        public FilterMode FilterMode = FilterMode.Bilinear;
        public TextureWrapMode WrapMode = TextureWrapMode.Clamp;
        public int FramesPerSecond = 30;
        public void CopySharedValuesFrom(ShaderBakeSettings source) {
            if (source == null) {
                return;
            }
            Material = source.Material;
            Width = source.Width;
            Height = source.Height;
            OverwriteExisting = source.OverwriteExisting;
            SelectCreatedAsset = source.SelectCreatedAsset;
            FilterMode = source.FilterMode;
            WrapMode = source.WrapMode;
            if (!string.IsNullOrWhiteSpace(source.OutputPath)) {
                string directory = Path.GetDirectoryName(source.OutputPath);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(source.OutputPath);
                if (string.IsNullOrWhiteSpace(directory)) {
                    directory = "Assets";
                }
                if (string.IsNullOrWhiteSpace(fileNameWithoutExtension)) {
                    fileNameWithoutExtension = "Baked Animation";
                }
                OutputFolderPath = Path.Combine(directory, fileNameWithoutExtension).Replace('\\', '/');
            }
        }
    }
}
#endif
