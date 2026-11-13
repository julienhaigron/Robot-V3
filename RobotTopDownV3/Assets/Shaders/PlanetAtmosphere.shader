Shader "RTD/PlanetAtmosphere"
{
	Properties
	{
		_ScatterColor ("Scatter Color", Color) = (0.7064634, 0.882387, 0.9063433, 1)
		_AbsorptionColor ("Absorption Color", Color) = (0.9063396, 0.7533226, 0.6785359, 1)
		_Density ("Density", Float) = 0.03
		_DensityFromMax ("Density From Max", Float) = 1.307
		_DensityPower ("Density Power", Float) = 6
		_Anisotropy ("Anisotropy", Range(-0.99, 0.99)) = 0
		_Displacement ("Displacement", Float) = 0.307
		_PlanetRadius ("Planet Radius", Float) = 106.497
		_SunAngularRadius ("Sun Angular Radius", Float) = 0.1544617
	}
	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
		ZWrite Off
		Cull Back

		CGINCLUDE
		#include "UnityCG.cginc"
		#include "UnityLightingCommon.cginc"

		#define ATMOSPHERE_STEPS 24

		float4 _ScatterColor;
		float4 _AbsorptionColor;
		float _Density;
		float _DensityFromMax;
		float _DensityPower;
		float _Anisotropy;
		float _Displacement;
		float _PlanetRadius;
		float _SunAngularRadius;

		UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

		struct appdata
		{
			float4 vertex : POSITION;
		};

		struct v2f
		{
			float4 pos : SV_POSITION;
			float3 objectPos : TEXCOORD0;
			float3 worldPos : TEXCOORD1;
			float4 screenPos : TEXCOORD2;
		};

		v2f vert (appdata v)
		{
			v2f o;
			float3 objectPos = v.vertex.xyz + normalize(v.vertex.xyz) * _Displacement;
			o.pos = UnityObjectToClipPos(float4(objectPos, 1.0));
			o.objectPos = objectPos;
			o.worldPos = mul(unity_ObjectToWorld, float4(objectPos, 1.0)).xyz;
			o.screenPos = ComputeScreenPos(o.pos);
			return o;
		}

		float DensityAt (float r)
		{
			return _Density * pow(saturate(r / _DensityFromMax), _DensityPower);
		}

		void Integrate (v2f i, out float3 inscatter, out float3 transmittance)
		{
			inscatter = 0.0;
			transmittance = 1.0;

			float3 cameraObject = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
			float3 entry = i.objectPos;
			float3 dir = normalize(entry - cameraObject);
			float tEnd = -2.0 * dot(entry, dir);

			float sceneEyeDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
			float3 sceneWorld = _WorldSpaceCameraPos + (i.worldPos - _WorldSpaceCameraPos) * (sceneEyeDepth / i.screenPos.w);
			float3 sceneObject = mul(unity_WorldToObject, float4(sceneWorld, 1.0)).xyz;
			tEnd = min(tEnd, dot(sceneObject - entry, dir));

			if (tEnd <= 0.0)
				return;

			float3 lightDir = normalize(mul((float3x3)unity_WorldToObject, _WorldSpaceLightPos0.xyz));
			float entryRadius = length(entry);
			float g = _Anisotropy;
			float phase = (1.0 - g * g) / (4.0 * UNITY_PI * pow(max(1.0 + g * g - 2.0 * g * dot(dir, lightDir), 1e-4), 1.5));
			float3 extinctionColor = _ScatterColor.rgb + (1.0 - _AbsorptionColor.rgb);
			float stepLength = tEnd / ATMOSPHERE_STEPS;

			[loop]
			for (int s = 0; s < ATMOSPHERE_STEPS; s++)
			{
				float3 p = entry + dir * ((s + 0.5) * stepLength);
				float r = length(p);
				float density = DensityAt(r);
				float3 extinction = max(density * extinctionColor, 1e-6);

				float pl = dot(p, lightDir);
				float sunPath = -pl + sqrt(max(pl * pl - r * r + entryRadius * entryRadius, 0.0));
				float3 sunTransmittance = exp(-extinction * sunPath);

				float planetAngle = asin(saturate(_PlanetRadius / r));
				float sunToCentre = acos(clamp(-pl / r, -1.0, 1.0));
				float visibility = smoothstep(planetAngle - _SunAngularRadius, planetAngle + _SunAngularRadius, sunToCentre);

				float3 stepTransmittance = exp(-extinction * stepLength);
				inscatter += transmittance * density * _ScatterColor.rgb * sunTransmittance * visibility * (1.0 - stepTransmittance) / extinction;
				transmittance *= stepTransmittance;
			}

			inscatter *= _LightColor0.rgb * UNITY_PI * phase;
		}
		ENDCG

		Pass
		{
			Tags { "LightMode" = "ForwardBase" }
			Blend DstColor Zero

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragTransmittance
			#pragma target 3.0

			float4 fragTransmittance (v2f i) : SV_Target
			{
				float3 inscatter, transmittance;
				Integrate(i, inscatter, transmittance);
				return float4(transmittance, 1.0);
			}
			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "ForwardBase" }
			Blend One One

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragInscatter
			#pragma target 3.0

			float4 fragInscatter (v2f i) : SV_Target
			{
				float3 inscatter, transmittance;
				Integrate(i, inscatter, transmittance);
				return float4(inscatter, 0.0);
			}
			ENDCG
		}
	}
	Fallback Off
}
