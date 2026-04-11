#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Serebrennikov {
    public sealed class ShaderBakerWindow : EditorWindow {
        ShaderBakeSettings settings;
        ShaderBakeValidator validator;
        ShaderTextureBaker baker;
        ShaderAnimationBakeSettings animationSettings;
        ShaderAnimationBakeValidator animationValidator;
        ShaderAnimationTextureBaker animationBaker;
        ObjectField materialField;
        IntegerField widthField;
        IntegerField heightField;
        TextField outputPathField;
        TextField animationOutputFolderField;
        IntegerField animationFpsField;
        EnumField filterModeField;
        EnumField wrapModeField;
        Toggle overwriteExistingToggle;
        Toggle selectCreatedAssetToggle;
        HelpBox validationHelpBox;
        Button bakeButton;
        Button bakeAnimationButton;
        Button openAnimationFolderButton;
        bool animationFolderWasEditedManually;
        TextureListMergeModule textureListMergeModule;
        [MenuItem("Tools/Shader baker by Serebrennikov", false, -10000)]
        static void OpenWindow() {
            ShaderBakerWindow window = GetWindow<ShaderBakerWindow>();
            window.titleContent = new GUIContent("Shader baker");
            window.minSize = new Vector2(420.0f, 420.0f);
            window.Show();
        }
        public void CreateGUI() {
            settings = new ShaderBakeSettings();
            validator = new ShaderBakeValidator();
            baker = new ShaderTextureBaker();
            animationSettings = new ShaderAnimationBakeSettings();
            animationSettings.CopySharedValuesFrom(settings);
            animationValidator = new ShaderAnimationBakeValidator();
            animationBaker = new ShaderAnimationTextureBaker();
            textureListMergeModule = new TextureListMergeModule();
            VisualElement root = rootVisualElement;
            root.Clear();
            materialField = new ObjectField("Material") {
                objectType = typeof(Material),
                allowSceneObjects = false,
                value = settings.Material
            };
            materialField.RegisterValueChangedCallback(evt => {
                settings.Material = evt.newValue as Material;
                SyncAnimationSettingsFromBase();
                RefreshUiState();
            });
            root.Add(materialField);
            widthField = new IntegerField("Width") {
                value = settings.Width
            };
            widthField.RegisterValueChangedCallback(evt => {
                settings.Width = Mathf.Max(1, evt.newValue);
                widthField.SetValueWithoutNotify(settings.Width);
                SyncAnimationSettingsFromBase();
                RefreshUiState();
            });
            root.Add(widthField);
            heightField = new IntegerField("Height") {
                value = settings.Height
            };
            heightField.RegisterValueChangedCallback(evt => {
                settings.Height = Mathf.Max(1, evt.newValue);
                heightField.SetValueWithoutNotify(settings.Height);
                SyncAnimationSettingsFromBase();
                RefreshUiState();
            });
            root.Add(heightField);
            outputPathField = new TextField("PNG Path") {
                value = settings.OutputPath
            };
            outputPathField.RegisterValueChangedCallback(evt => {
                settings.OutputPath = evt.newValue ?? string.Empty;
                SyncAnimationSettingsFromBase();
                animationOutputFolderField.SetValueWithoutNotify(animationSettings.OutputFolderPath);
                RefreshUiState();
            });
            root.Add(outputPathField);
            animationOutputFolderField = new TextField("Animation Folder") {
                value = animationSettings.OutputFolderPath
            };
            animationOutputFolderField.RegisterValueChangedCallback(evt => {
                animationFolderWasEditedManually = true;
                animationSettings.OutputFolderPath = (evt.newValue ?? string.Empty).Replace('\\', '/');
                RefreshUiState();
            });
            root.Add(animationOutputFolderField);
            animationFpsField = new IntegerField("Animation FPS") {
                value = animationSettings.FramesPerSecond
            };
            animationFpsField.RegisterValueChangedCallback(evt => {
                animationSettings.FramesPerSecond = Mathf.Max(1, evt.newValue);
                animationFpsField.SetValueWithoutNotify(animationSettings.FramesPerSecond);
                RefreshUiState();
            });
            root.Add(animationFpsField);
            filterModeField = new EnumField("Filter Mode", settings.FilterMode);
            filterModeField.RegisterValueChangedCallback(evt => {
                settings.FilterMode = (FilterMode)evt.newValue;
                SyncAnimationSettingsFromBase();
                RefreshUiState();
            });
            root.Add(filterModeField);
            wrapModeField = new EnumField("Wrap Mode", settings.WrapMode);
            wrapModeField.RegisterValueChangedCallback(evt => {
                settings.WrapMode = (TextureWrapMode)evt.newValue;
                SyncAnimationSettingsFromBase();
                RefreshUiState();
            });
            root.Add(wrapModeField);
            overwriteExistingToggle = new Toggle("Overwrite Existing") {
                value = settings.OverwriteExisting
            };
            overwriteExistingToggle.RegisterValueChangedCallback(evt => {
                settings.OverwriteExisting = evt.newValue;
                SyncAnimationSettingsFromBase();
                RefreshUiState();
            });
            root.Add(overwriteExistingToggle);
            selectCreatedAssetToggle = new Toggle("Select Created Asset") {
                value = settings.SelectCreatedAsset
            };
            selectCreatedAssetToggle.RegisterValueChangedCallback(evt => {
                settings.SelectCreatedAsset = evt.newValue;
                SyncAnimationSettingsFromBase();
                RefreshUiState();
            });
            root.Add(selectCreatedAssetToggle);
            validationHelpBox = new HelpBox(string.Empty, HelpBoxMessageType.None);
            root.Add(validationHelpBox);
            bakeButton = new Button(OnBakeClicked) {
                text = "Bake Texture"
            };
            root.Add(bakeButton);
            bakeAnimationButton = new Button(OnBakeAnimationClicked) {
                text = "Bake Animation"
            };
            root.Add(bakeAnimationButton);
            openAnimationFolderButton = new Button(OnOpenAnimationFolderClicked) {
                text = "Open Animation Folder"
            };
            root.Add(openAnimationFolderButton);
            root.Add(textureListMergeModule.Root);
            RefreshUiState();
        }
        void SyncAnimationSettingsFromBase() {
            string currentFolderPath = animationSettings?.OutputFolderPath;
            int currentFramesPerSecond = animationSettings != null ? animationSettings.FramesPerSecond : 30;
            animationSettings.CopySharedValuesFrom(settings);
            animationSettings.FramesPerSecond = currentFramesPerSecond;
            if (animationFolderWasEditedManually && !string.IsNullOrWhiteSpace(currentFolderPath)) {
                animationSettings.OutputFolderPath = currentFolderPath;
            }
        }
        void RefreshUiState() {
            ShaderBakeValidationResult textureValidation = validator.Validate(settings);
            ShaderBakeValidationResult animationValidation = animationValidator.Validate(animationSettings);
            if (textureValidation.Messages.Count > 0) {
                ShaderBakeValidationMessage first = textureValidation.Messages[0];
                validationHelpBox.text = $"Texture: {first.Text}";
                validationHelpBox.messageType = ConvertSeverity(first.Severity);
                validationHelpBox.style.display = DisplayStyle.Flex;
            } else if (animationValidation.Messages.Count > 0) {
                ShaderBakeValidationMessage first = animationValidation.Messages[0];
                validationHelpBox.text = $"Animation: {first.Text}";
                validationHelpBox.messageType = ConvertSeverity(first.Severity);
                validationHelpBox.style.display = DisplayStyle.Flex;
            } else {
                validationHelpBox.style.display = DisplayStyle.None;
            }
            bakeButton?.SetEnabled(!textureValidation.HasErrors);
            bakeAnimationButton?.SetEnabled(!animationValidation.HasErrors);
            openAnimationFolderButton?.SetEnabled(!string.IsNullOrWhiteSpace(animationSettings.OutputFolderPath));
        }
        void OnBakeClicked() {
            ShaderBakeValidationResult validation = validator.Validate(settings);
            if (validation.HasErrors) {
                EditorUtility.DisplayDialog("Bake failed", validation.Messages[0].Text, "OK");
                RefreshUiState();
                return;
            }
            ShaderBakeResult result = baker.Bake(settings);
            if (!result.Success) {
                EditorUtility.DisplayDialog("Bake failed", result.ErrorMessage, "OK");
                return;
            }
            if (settings.SelectCreatedAsset && result.CreatedAsset != null) {
                Selection.activeObject = result.CreatedAsset;
                EditorGUIUtility.PingObject(result.CreatedAsset);
            }
        }
        void OnBakeAnimationClicked() {
            ShaderBakeValidationResult validation = animationValidator.Validate(animationSettings);
            if (validation.HasErrors) {
                EditorUtility.DisplayDialog("Animation bake failed", validation.Messages[0].Text, "OK");
                RefreshUiState();
                return;
            }
            ShaderAnimationBakeResult result = animationBaker.Bake(animationSettings);
            if (!result.Success) {
                EditorUtility.DisplayDialog("Animation bake failed", result.ErrorMessage, "OK");
                return;
            }
            if (animationSettings.SelectCreatedAsset && result.CreatedAssets.Count > 0 && result.CreatedAssets[0] != null) {
                Selection.activeObject = result.CreatedAssets[0];
                EditorGUIUtility.PingObject(result.CreatedAssets[0]);
            }
        }
        void OnOpenAnimationFolderClicked() {
            if (string.IsNullOrWhiteSpace(animationSettings.OutputFolderPath)) {
                return;
            }
            if (!AssetDatabase.IsValidFolder(animationSettings.OutputFolderPath)) {
                string fullFolderPath = Path.Combine(Directory.GetCurrentDirectory(), animationSettings.OutputFolderPath);
                if (Directory.Exists(fullFolderPath)) {
                    EditorUtility.RevealInFinder(fullFolderPath);
                }
                return;
            }
            UnityEngine.Object folderAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(animationSettings.OutputFolderPath);
            if (folderAsset != null) {
                Selection.activeObject = folderAsset;
                EditorGUIUtility.PingObject(folderAsset);
            }
        }
        static HelpBoxMessageType ConvertSeverity(ShaderBakeValidationSeverity severity) {
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
