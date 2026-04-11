#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Serebrennikov {
    [Serializable]
    public sealed class TextureListMergeSettings {
        public int ListCount = 2;
        public string OutputFolderPath = "Assets/Merged Texture Lists";
        public string OutputNamePrefix = "MergedTexture";
        public bool OverwriteExisting = true;
        public bool SelectCreatedAsset = true;
        public FilterMode FilterMode = FilterMode.Bilinear;
        public TextureWrapMode WrapMode = TextureWrapMode.Clamp;
        public List<TextureListMergeLayer> Layers = new();

        public void EnsureLayerCount() {
            ListCount = Mathf.Max(1, ListCount);
            while (Layers.Count < ListCount) {
                Layers.Add(new TextureListMergeLayer {
                    Name = $"List {Layers.Count + 1}"
                });
            }
            while (Layers.Count > ListCount) {
                Layers.RemoveAt(Layers.Count - 1);
            }
            for (int i = 0; i < Layers.Count; i++) {
                Layers[i] ??= new TextureListMergeLayer();
                if (string.IsNullOrWhiteSpace(Layers[i].Name)) {
                    Layers[i].Name = $"List {i + 1}";
                }
            }
        }
    }

    [Serializable]
    public sealed class TextureListMergeLayer {
        public string Name = "List";
        public List<Texture2D> Textures = new();
    }
}
#endif
