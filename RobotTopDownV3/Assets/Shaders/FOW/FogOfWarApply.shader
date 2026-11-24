Shader "Custom/FogOfWar_Apply"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _NoiseTex ("Noise", 2D) = "gray" {}
        _NoiseScale ("Noise Scale (per m)", Float) = 0.08
        _NoiseDrift ("Noise Drift (per s)", Vector) = (0.012, 0.007, 0, 0)
        _EdgeNoise ("Edge Noise", Range(0, 1)) = 0.25
        _Desaturation ("Desaturation", Range(0, 1)) = 0.75
        _Darken ("Darken", Range(0, 1)) = 0.5
        _Tint ("Tint", Color) = (0.62, 0.7, 0.88, 1)
        _ThermalTint ("Thermal Layer Tint", Color) = (0.92, 0.66, 0.48, 1)
        _RadarTint ("Radar Layer Tint", Color) = (0.5, 0.82, 0.62, 1)
        _BlindDarken ("Blind Layer Extra Darken", Range(0, 1)) = 0.25
        _RadarScan ("Radar Layer Scan", Range(0, 1)) = 0.08
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        ZWrite Off
        ZTest Always
        Cull Off

        GrabPass { "_FogGrab" }

        Pass
        {
            Blend One Zero

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _FogGrab;
            sampler2D _CameraDepthTexture;
            sampler2D _NoiseTex;
            sampler2D _FogSoftMask;
            sampler2D _FogSoftMaskPrevious;
            float _FogMaskBlend;
            float _FogVisionLayers;
            float2 _FogGridOrigin;
            float _FogGridSize;
            float4x4 _FogMainCamInvVP;

            float _NoiseScale;
            float4 _NoiseDrift;
            float _EdgeNoise;
            float _Desaturation;
            float _Darken;
            float4 _Tint;
            float4 _ThermalTint;
            float4 _RadarTint;
            float _BlindDarken;
            float _RadarScan;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 grabPos : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.grabPos = ComputeGrabScreenPos(o.pos);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 scene = tex2Dproj(_FogGrab, i.grabPos).rgb;
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                if (Linear01Depth(rawDepth) > 0.9999)
                    return float4(scene, 1);

                float4 world = mul(_FogMainCamInvVP, float4(i.uv * 2.0 - 1.0, rawDepth, 1.0));
                float3 worldPos = world.xyz / world.w;

                float2 maskUV = (worldPos.xz - _FogGridOrigin) / _FogGridSize;
                float3 mask = lerp(tex2D(_FogSoftMaskPrevious, maskUV).rgb, tex2D(_FogSoftMask, maskUV).rgb, _FogMaskBlend);

                float t = _Time.y;
                float noise = tex2D(_NoiseTex, worldPos.xz * _NoiseScale + _NoiseDrift.xy * t).r;
                float fog = smoothstep(0.08, 0.92, mask.r + (noise - 0.5) * _EdgeNoise);

                float coverage = 1.0 / max(mask.r, 0.001);
                float thermal = saturate(mask.g * coverage);
                float radar = (1.0 - thermal) * saturate(mask.b * coverage);
                float blind = 1.0 - thermal - radar;
                float layers = _FogVisionLayers;

                float3 tint = lerp(_Tint.rgb, _ThermalTint.rgb * thermal + _RadarTint.rgb * radar + _Tint.rgb * blind, layers);
                float darken = saturate(_Darken + _BlindDarken * blind * layers);
                float lum = dot(scene, float3(0.299, 0.587, 0.114));
                float3 veil = lerp(scene, lum.xxx, _Desaturation) * (1.0 - darken) * tint * (0.85 + noise * 0.3);

                float scan = pow(saturate(0.5 + 0.5 * sin(worldPos.z * 5.0 - t * 2.5)), 12.0);
                veil += _RadarTint.rgb * scan * _RadarScan * radar * layers;

                return float4(lerp(scene, veil, fog), 1);
            }
            ENDCG
        }
    }
}
