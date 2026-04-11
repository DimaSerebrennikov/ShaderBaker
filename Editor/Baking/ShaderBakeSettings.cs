#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Serebrennikov {
    public sealed class ShaderBakeSettings {
        public Material Material;
        public int Width = 512;
        public int Height = 512;
        public string OutputPath = "Assets/Baked by ShaderBaker.png";
        public bool OverwriteExisting = true;
        public bool SelectCreatedAsset = true;
        public FilterMode FilterMode = FilterMode.Bilinear;
        public TextureWrapMode WrapMode = TextureWrapMode.Clamp;
    }
}
#endif
