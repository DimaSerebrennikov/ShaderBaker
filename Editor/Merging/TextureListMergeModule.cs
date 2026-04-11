#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Serebrennikov {
    public sealed class TextureListMergeModule {
        readonly TextureListMergeSettings settings;
        readonly TextureListMergeValidator validator;
        readonly TextureListMerger merger;
        readonly VisualElement root;
        readonly IntegerField listCountField;
        readonly TextField outputFolderField;
        readonly TextField outputPrefixField;
        readonly EnumField filterModeField;
        readonly EnumField wrapModeField;
        readonly Toggle overwriteExistingToggle;
        readonly Toggle selectCreatedAssetToggle;
        readonly ScrollView layersScrollView;
        readonly HelpBox infoBox;
        readonly HelpBox validationBox;
        readonly Button mergeButton;
        public TextureListMergeModule() {
            settings = new TextureListMergeSettings();
            settings.EnsureLayerCount();
            validator = new TextureListMergeValidator();
            merger = new TextureListMerger();
            root = new VisualElement();
            root.style.marginTop = 12.0f;
            root.style.paddingLeft = 4.0f;
            root.style.paddingRight = 4.0f;
            root.style.flexGrow = 1.0f;
            Label titleLabel = new Label("Texture List Merge");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 13;
            titleLabel.style.marginBottom = 6.0f;
            root.Add(titleLabel);
            infoBox = new HelpBox("Each list is a blend layer. Lists are blended from top to bottom in the order shown here: List 1 is the base, the next lists are composited over it.", HelpBoxMessageType.Info);
            infoBox.style.marginBottom = 8.0f;
            root.Add(infoBox);
            listCountField = new IntegerField("List Count");
            listCountField.value = settings.ListCount;
            listCountField.RegisterValueChangedCallback(OnListCountChanged);
            root.Add(listCountField);
            outputFolderField = new TextField("Output Folder");
            outputFolderField.value = settings.OutputFolderPath ?? string.Empty;
            outputFolderField.RegisterValueChangedCallback(evt => {
                settings.OutputFolderPath = (evt.newValue ?? string.Empty).Replace('\\', '/');
                RefreshValidation();
            });
            root.Add(outputFolderField);
            outputPrefixField = new TextField("Output Prefix");
            outputPrefixField.value = settings.OutputNamePrefix ?? string.Empty;
            outputPrefixField.RegisterValueChangedCallback(evt => {
                settings.OutputNamePrefix = evt.newValue ?? string.Empty;
                RefreshValidation();
            });
            root.Add(outputPrefixField);
            filterModeField = new EnumField("Filter Mode", settings.FilterMode);
            filterModeField.Init(settings.FilterMode);
            filterModeField.RegisterValueChangedCallback(evt => {
                settings.FilterMode = (FilterMode)evt.newValue;
                RefreshValidation();
            });
            root.Add(filterModeField);
            wrapModeField = new EnumField("Wrap Mode", settings.WrapMode);
            wrapModeField.Init(settings.WrapMode);
            wrapModeField.RegisterValueChangedCallback(evt => {
                settings.WrapMode = (TextureWrapMode)evt.newValue;
                RefreshValidation();
            });
            root.Add(wrapModeField);
            overwriteExistingToggle = new Toggle("Overwrite Existing");
            overwriteExistingToggle.value = settings.OverwriteExisting;
            overwriteExistingToggle.RegisterValueChangedCallback(evt => {
                settings.OverwriteExisting = evt.newValue;
                RefreshValidation();
            });
            root.Add(overwriteExistingToggle);
            selectCreatedAssetToggle = new Toggle("Select Created Asset");
            selectCreatedAssetToggle.value = settings.SelectCreatedAsset;
            selectCreatedAssetToggle.RegisterValueChangedCallback(evt => {
                settings.SelectCreatedAsset = evt.newValue;
                RefreshValidation();
            });
            root.Add(selectCreatedAssetToggle);
            layersScrollView = new ScrollView(ScrollViewMode.Vertical);
            layersScrollView.style.minHeight = 220.0f;
            layersScrollView.style.marginTop = 8.0f;
            layersScrollView.style.marginBottom = 8.0f;
            layersScrollView.style.flexGrow = 1.0f;
            root.Add(layersScrollView);
            validationBox = new HelpBox(string.Empty, HelpBoxMessageType.None);
            validationBox.style.display = DisplayStyle.None;
            validationBox.style.marginBottom = 8.0f;
            root.Add(validationBox);
            mergeButton = new Button(OnMergeClicked) {
                text = "Merge"
            };
            mergeButton.style.height = 28.0f;
            root.Add(mergeButton);
            RebuildLayersUi();
            RefreshValidation();
        }
        public VisualElement Root => root;
        void OnListCountChanged(ChangeEvent<int> evt) {
            settings.ListCount = Mathf.Max(1, evt.newValue);
            settings.EnsureLayerCount();
            if (listCountField.value != settings.ListCount) {
                listCountField.SetValueWithoutNotify(settings.ListCount);
            }
            RebuildLayersUi();
            RefreshValidation();
        }
        void RebuildLayersUi() {
            layersScrollView.Clear();
            for (int layerIndex = 0; layerIndex < settings.Layers.Count; layerIndex++) {
                TextureListMergeLayer layer = settings.Layers[layerIndex];
                VisualElement layerElement = CreateLayerElement(layerIndex, layer);
                layersScrollView.Add(layerElement);
            }
        }
        VisualElement CreateLayerElement(int layerIndex, TextureListMergeLayer layer) {
            VisualElement container = new VisualElement();
            container.style.marginBottom = 8.0f;
            container.style.paddingTop = 8.0f;
            container.style.paddingBottom = 8.0f;
            container.style.paddingLeft = 8.0f;
            container.style.paddingRight = 8.0f;
            container.style.borderTopWidth = 1.0f;
            container.style.borderBottomWidth = 1.0f;
            container.style.borderLeftWidth = 1.0f;
            container.style.borderRightWidth = 1.0f;
            container.style.borderTopColor = new Color(0.22f, 0.22f, 0.22f);
            container.style.borderBottomColor = new Color(0.22f, 0.22f, 0.22f);
            container.style.borderLeftColor = new Color(0.22f, 0.22f, 0.22f);
            container.style.borderRightColor = new Color(0.22f, 0.22f, 0.22f);
            container.style.borderTopLeftRadius = 3.0f;
            container.style.borderTopRightRadius = 3.0f;
            container.style.borderBottomLeftRadius = 3.0f;
            container.style.borderBottomRightRadius = 3.0f;
            TextField nameField = new TextField("List Name");
            nameField.value = string.IsNullOrWhiteSpace(layer.Name) ? $"List {layerIndex + 1}" : layer.Name;
            nameField.RegisterValueChangedCallback(evt => {
                layer.Name = evt.newValue;
                RefreshValidation();
            });
            container.Add(nameField);
            VisualElement buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.marginTop = 4.0f;
            buttonRow.style.marginBottom = 6.0f;
            container.Add(buttonRow);
            Button addTextureButton = new Button(() => {
                layer.Textures.Add(null);
                RebuildLayersUi();
                RefreshValidation();
            }) {
                text = "Add Empty Slot"
            };
            addTextureButton.style.marginRight = 4.0f;
            buttonRow.Add(addTextureButton);
            Button addSelectedTexturesButton = new Button(() => {
                AddSelectedTextures(layer);
            }) {
                text = "Add Selected Textures"
            };
            addSelectedTexturesButton.style.marginRight = 4.0f;
            buttonRow.Add(addSelectedTexturesButton);
            Button clearButton = new Button(() => {
                layer.Textures.Clear();
                RebuildLayersUi();
                RefreshValidation();
            }) {
                text = "Clear"
            };
            buttonRow.Add(clearButton);
            Label countLabel = new Label($"Texture Count: {layer.Textures.Count}");
            countLabel.style.marginBottom = 6.0f;
            countLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            container.Add(countLabel);
            for (int textureIndex = 0; textureIndex < layer.Textures.Count; textureIndex++) {
                int currentTextureIndex = textureIndex;
                VisualElement textureRow = new VisualElement();
                textureRow.style.flexDirection = FlexDirection.Row;
                textureRow.style.alignItems = Align.Center;
                textureRow.style.marginBottom = 4.0f;
                ObjectField textureField = new ObjectField($"Element {currentTextureIndex + 1}");
                textureField.objectType = typeof(Texture2D);
                textureField.allowSceneObjects = false;
                textureField.value = layer.Textures[currentTextureIndex];
                textureField.style.flexGrow = 1.0f;
                textureField.RegisterValueChangedCallback(evt => {
                    layer.Textures[currentTextureIndex] = evt.newValue as Texture2D;
                    RefreshValidation();
                });
                textureRow.Add(textureField);
                Button removeButton = new Button(() => {
                    if (currentTextureIndex < 0 || currentTextureIndex >= layer.Textures.Count) {
                        return;
                    }
                    layer.Textures.RemoveAt(currentTextureIndex);
                    RebuildLayersUi();
                    RefreshValidation();
                }) {
                    text = "X"
                };
                removeButton.style.width = 24.0f;
                removeButton.style.marginLeft = 4.0f;
                textureRow.Add(removeButton);
                container.Add(textureRow);
            }
            AddTextureDropArea(container, layer);
            return container;
        }
        void AddTextureDropArea(VisualElement parent, TextureListMergeLayer layer) {
            VisualElement dropArea = new VisualElement();
            dropArea.style.height = 42.0f;
            dropArea.style.marginTop = 6.0f;
            dropArea.style.justifyContent = Justify.Center;
            dropArea.style.alignItems = Align.Center;
            dropArea.style.borderTopWidth = 1.0f;
            dropArea.style.borderBottomWidth = 1.0f;
            dropArea.style.borderLeftWidth = 1.0f;
            dropArea.style.borderRightWidth = 1.0f;
            dropArea.style.borderTopColor = new Color(0.35f, 0.35f, 0.35f);
            dropArea.style.borderBottomColor = new Color(0.35f, 0.35f, 0.35f);
            dropArea.style.borderLeftColor = new Color(0.35f, 0.35f, 0.35f);
            dropArea.style.borderRightColor = new Color(0.35f, 0.35f, 0.35f);
            dropArea.style.borderTopLeftRadius = 3.0f;
            dropArea.style.borderTopRightRadius = 3.0f;
            dropArea.style.borderBottomLeftRadius = 3.0f;
            dropArea.style.borderBottomRightRadius = 3.0f;
            Label label = new Label("Drag & drop multiple textures here");
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            dropArea.Add(label);
            dropArea.RegisterCallback<DragUpdatedEvent>(_ => {
                bool hasTextures = HasDraggedTextures();
                DragAndDrop.visualMode = hasTextures ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            });
            dropArea.RegisterCallback<DragPerformEvent>(_ => {
                List<Texture2D> draggedTextures = GetDraggedTextures();
                if (draggedTextures.Count == 0) {
                    return;
                }
                DragAndDrop.AcceptDrag();
                for (int i = 0; i < draggedTextures.Count; i++) {
                    layer.Textures.Add(draggedTextures[i]);
                }
                RebuildLayersUi();
                RefreshValidation();
            });
            parent.Add(dropArea);
        }
        void AddSelectedTextures(TextureListMergeLayer layer) {
            Object[] selectedObjects = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
            if (selectedObjects == null || selectedObjects.Length == 0) {
                EditorUtility.DisplayDialog("No textures selected", "Select one or more Texture2D assets in the Project window first.", "OK");
                return;
            }
            int addedCount = 0;
            for (int i = 0; i < selectedObjects.Length; i++) {
                Texture2D texture = selectedObjects[i] as Texture2D;
                if (texture == null) {
                    continue;
                }
                layer.Textures.Add(texture);
                addedCount++;
            }
            if (addedCount == 0) {
                EditorUtility.DisplayDialog("No textures added", "The current selection does not contain any Texture2D assets.", "OK");
                return;
            }
            RebuildLayersUi();
            RefreshValidation();
        }
        static bool HasDraggedTextures() {
            List<Texture2D> textures = GetDraggedTextures();
            return textures.Count > 0;
        }
        static List<Texture2D> GetDraggedTextures() {
            List<Texture2D> textures = new List<Texture2D>();
            Object[] references = DragAndDrop.objectReferences;
            if (references == null || references.Length == 0) {
                return textures;
            }
            for (int i = 0; i < references.Length; i++) {
                Texture2D texture = references[i] as Texture2D;
                if (texture != null) {
                    textures.Add(texture);
                }
            }
            return textures;
        }
        void RefreshValidation() {
            ShaderBakeValidationResult validation = validator.Validate(settings);
            if (validation.Messages.Count > 0) {
                ShaderBakeValidationMessage first = validation.Messages[0];
                validationBox.text = first.Text;
                validationBox.messageType = ConvertMessageType(first.Severity);
                validationBox.style.display = DisplayStyle.Flex;
            } else {
                validationBox.text = string.Empty;
                validationBox.style.display = DisplayStyle.None;
            }
            mergeButton.SetEnabled(!validation.HasErrors);
        }
        void OnMergeClicked() {
            TextureListMergeResult result = merger.Merge(settings);
            if (!result.Success) {
                EditorUtility.DisplayDialog("Merge failed", result.ErrorMessage, "OK");
                return;
            }
            if (settings.SelectCreatedAsset && result.CreatedAssets.Count > 0 && result.CreatedAssets[0] != null) {
                Selection.activeObject = result.CreatedAssets[0];
                EditorGUIUtility.PingObject(result.CreatedAssets[0]);
            }
        }
        static HelpBoxMessageType ConvertMessageType(ShaderBakeValidationSeverity severity) {
            return severity switch {
                ShaderBakeValidationSeverity.Info => HelpBoxMessageType.Info,
                ShaderBakeValidationSeverity.Warning => HelpBoxMessageType.Warning,
                ShaderBakeValidationSeverity.Error => HelpBoxMessageType.Error,
                _ => HelpBoxMessageType.None
            };
        }
    }
}
#endif
