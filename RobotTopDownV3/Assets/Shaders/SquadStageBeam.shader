Shader "Custom/SquadStageBeam"
{
    Properties
    {
        _Color ("Color", Color) = (0.5, 0.83, 1, 1)
        _Base ("Side Base", Range(0, 1)) = 0.035
        _Top ("Side Top Glow", Range(0, 2)) = 0.26
        _Bottom ("Side Bottom Glow", Range(0, 2)) = 0.08
        _CapFill ("Cap Fill", Range(0, 1)) = 0.06
        _CapRing ("Cap Ring", Range(0, 2)) = 0.9
        [PerRendererData] _Intensity ("Intensity", Float) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+50" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        ZWrite Off
        Cull Off
        Blend One One
        ColorMask RGB

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float4 _Color;
            float _Base;
            float _Top;
            float _Bottom;
            float _CapFill;
            float _CapRing;
            float _Intensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 local : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.local = v.vertex.xyz;
                o.normal = v.normal;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                if (i.normal.y < -0.5)
                    discard;

                float height = saturate(i.local.y * 0.5 + 0.5);
                float alpha = _Base + _Top * pow(height, 6.0) + _Bottom * pow(1.0 - height, 3.0);
                if (i.normal.y > 0.5)
                {
                    float radius = length(i.local.xz) / 0.5;
                    float smoothing = max(fwidth(radius), 0.001);
                    alpha = _CapFill + _CapRing * smoothstep(0.88 - smoothing, 0.96, radius) * (1.0 - smoothstep(0.98, 1.0 + smoothing, radius));
                }

                return float4(_Color.rgb * alpha * _Intensity, 0.0);
            }
            ENDCG
        }
    }
}
