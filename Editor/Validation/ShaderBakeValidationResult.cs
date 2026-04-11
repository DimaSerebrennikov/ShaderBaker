using System;
using System.Collections.Generic;
using UnityEngine;
namespace Serebrennikov {
    public sealed class ShaderBakeValidationResult {
        readonly List<ShaderBakeValidationMessage> messages = new();
        public IReadOnlyList<ShaderBakeValidationMessage> Messages => messages;
        public bool HasErrors {
            get {
                for (int i = 0; i < messages.Count; i++) {
                    if (messages[i].Severity == ShaderBakeValidationSeverity.Error) {
                        return true;
                    }
                }
                return false;
            }
        }
        public void AddError(string text) {
            messages.Add(new ShaderBakeValidationMessage(ShaderBakeValidationSeverity.Error, text));
        }
        public void AddWarning(string text) {
            messages.Add(new ShaderBakeValidationMessage(ShaderBakeValidationSeverity.Warning, text));
        }
        public void AddInfo(string text) {
            messages.Add(new ShaderBakeValidationMessage(ShaderBakeValidationSeverity.Info, text));
        }
    }
}