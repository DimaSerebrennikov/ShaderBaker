using System;
using System.Collections.Generic;
using UnityEngine;
namespace Serebrennikov {
    public readonly struct ShaderBakeValidationMessage {
        public readonly ShaderBakeValidationSeverity Severity;
        public readonly string Text;
        public ShaderBakeValidationMessage(ShaderBakeValidationSeverity severity, string text) {
            Severity = severity;
            Text = text;
        }
    }
}