Shader "Custom/FogOfWar_Apply_Stylized"
{
    Properties
    {
        _FogMask("Fog Mask", 2D) = "white" {}
        _VisibleMask("Visible Mask", 2D) = "black" {}
        _FogColor("Fog Color", Color) = (0,0,0,0.6)
        _EdgeSoftness("Edge Softness", Range(0,1)) = 0.2
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _NoiseScale("Noise Scale", Float) = 5.0
        _MainTex("Main Texture", 2D) = "white" {}
    }
        SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _FogMask;
            sampler2D _VisibleMask;
            sampler2D _MainTex;
            sampler2D _NoiseTex;
            sampler2D _CameraDepthTexture;
            fixed4 _FogColor;
            float _EdgeSoftness;
            float _NoiseScale;
            float2 _FogGridOrigin;
            float _FogGridSize;
            float4x4 _FogMainCamInvVP;
            float3 _FogMainCamPos;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 WorldPosFromDepth(float2 uv, out float rawDepth)
            {
                rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);

                float4 clipPos = float4(uv * 2.0 - 1.0, rawDepth, 1.0);

                float4 worldPos = mul(_FogMainCamInvVP, clipPos);
                worldPos /= worldPos.w;
                return worldPos.xyz;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float rawDepth;
                float3 worldPos = WorldPosFromDepth(i.uv, rawDepth);

                if (Linear01Depth(rawDepth) > 0.9999)
                    return fixed4(0,0,0,0);

                float3 rayDir = normalize(worldPos - _FogMainCamPos);
                float t = -_FogMainCamPos.y / rayDir.y;
                float3 groundPos = _FogMainCamPos + rayDir * t;

                float2 fogUV = (groundPos.xz - _FogGridOrigin) / _FogGridSize;

                fixed mask = tex2D(_FogMask, fogUV).r;

                fixed noise = tex2D(_NoiseTex, fogUV * _NoiseScale).r;
                mask = smoothstep(0.0, _EdgeSoftness, mask - noise * 0.1);

                fixed4 fog = _FogColor;
                fog.a *= mask;
                return fog;
            }
            ENDCG
        }
    }
}