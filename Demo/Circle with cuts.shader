Shader "Serebrennikov/Circle with cuts" {
	Properties {
		[MainColor] _Color ("Color", Color) = (1,1,1,1)
		_Radius ("Radius", Range(0, 0.5)) = 0.35
		_Thickness ("Thickness", Range(0.001, 0.5)) = 0.05

		_Cut1Angle ("Cut 1 Angle", Range(0, 360)) = 0
		_Cut1Size ("Cut 1 Size", Range(0, 180)) = 20

		_Cut2Angle ("Cut 2 Angle", Range(0, 360)) = 90
		_Cut2Size ("Cut 2 Size", Range(0, 180)) = 0

		_Cut3Angle ("Cut 3 Angle", Range(0, 360)) = 180
		_Cut3Size ("Cut 3 Size", Range(0, 180)) = 0

		_Cut4Angle ("Cut 4 Angle", Range(0, 360)) = 270
		_Cut4Size ("Cut 4 Size", Range(0, 180)) = 0
	}

	SubShader {
		Tags {
			"RenderType"="TransparentCutout"
			"Queue"="AlphaTest"
			"RenderPipeline"="UniversalPipeline"
		}

		Pass {
			Name "Forward"
			Tags {
				"LightMode"="UniversalForward"
			}

			Cull Off
			ZWrite On
			Blend Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			struct Attributes {
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};
			struct Varyings {
				float4 positionHCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};
			CBUFFER_START(UnityPerMaterial)
				half4 _Color;
				half _Radius;
				half _Thickness;
				half _Cut1Angle;
				half _Cut1Size;
				half _Cut2Angle;
				half _Cut2Size;
				half _Cut3Angle;
				half _Cut3Size;
				half _Cut4Angle;
				half _Cut4Size;
			CBUFFER_END
			Varyings vert(Attributes v) {
				Varyings o;
				o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
				o.uv = v.uv;
				return o;
			}
			float DeltaAngleDeg(float a, float b) {
				float d = abs(a - b);
				return min(d, 360.0 - d);
			}
			bool IsInsideCut(float angleDeg, float cutAngle, float cutSize) {
				if(cutSize <= 0.001)
				{
					return false;
				}
				float halfSize = cutSize * 0.5;
				return DeltaAngleDeg(angleDeg, cutAngle) <= halfSize;
			}
			half4 frag(Varyings i) : SV_Target {
				float2 p = i.uv - 0.5;
				float d = length(p);
				clip(d - (_Radius - _Thickness));
				clip((_Radius + _Thickness) - d);
				float angle = degrees(atan2(p.y, p.x));
				angle = (angle < 0.0) ? angle + 360.0 : angle;
				if(IsInsideCut(angle, _Cut1Angle, _Cut1Size) ||
					IsInsideCut(angle, _Cut2Angle, _Cut2Size) ||
					IsInsideCut(angle, _Cut3Angle, _Cut3Size) ||
					IsInsideCut(angle, _Cut4Angle, _Cut4Size))
				{
					clip(-1);
				}
				return _Color;
			}
			ENDHLSL
		}
	}
}