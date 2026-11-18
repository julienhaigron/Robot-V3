Shader "UI/PanelTransitionHolo"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _TintFade ("Tint Fade", Range(0, 1)) = 1
        _TintTrail ("Tint Trail (screen fraction)", Range(0.01, 1)) = 0.35
        _TintColor ("Tint Color", Color) = (0.31, 0.71, 1, 0.22)
        _LineColor ("Line Color", Color) = (0.55, 0.85, 1, 1)
        _LineWidth ("Line Width (px)", Float) = 3
        _ScanlinePeriod ("Scanline Period (px)", Float) = 4
        _ScanlineOpacity ("Scanline Opacity", Range(0, 1)) = 0.14
        _GlitchHeight ("Glitch Height (px)", Float) = 28
        _GlitchAmount ("Glitch Amount (px)", Float) = 14
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Color;
            float _TintFade;
            float _TintTrail;
            float4 _TintColor;
            float4 _LineColor;
            float _LineWidth;
            float _ScanlinePeriod;
            float _ScanlineOpacity;
            float _GlitchHeight;
            float _GlitchAmount;
            float4 _PanelTransitionSweep;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 size = max(_PanelTransitionSweep.zw, float2(1.0, 1.0));
                float2 px = i.uv * size;
                float travel = _PanelTransitionSweep.x * size.y;
                bool isDownward = _PanelTransitionSweep.y > 0.0;
                float lineY = isDownward ? size.y - travel : travel;
                float behind = isDownward ? px.y - lineY : lineY - px.y;
                float halfLine = _LineWidth * 0.5;

                float scanline = step(frac(px.y / max(_ScanlinePeriod, 1.0)), 0.3) * _ScanlineOpacity;
                float trail = saturate(1.0 - behind / max(_TintTrail * size.y, 1.0));
                float4 revealed = float4(_TintColor.rgb, saturate(_TintColor.a + scanline) * trail * _TintFade);

                float band = saturate(1.0 + behind / max(_GlitchHeight, 1.0));
                float noise = frac(sin(floor(px.y / 3.0) * 91.7 + floor(_Time.y * 30.0) * 13.3) * 43758.5453);
                float2 captureUV = i.uv;
                captureUV.x += (noise - 0.5) * band * _GlitchAmount / size.x;
                #if UNITY_UV_STARTS_AT_TOP
                captureUV.y = 1.0 - captureUV.y;
                #endif
                float3 captured = lerp(tex2D(_MainTex, captureUV).rgb, _LineColor.rgb, band * 0.3);

                float4 color = float4(captured, 1.0);
                color = behind > -halfLine ? _LineColor : color;
                color = behind > halfLine ? revealed : color;
                return color * float4(1.0, 1.0, 1.0, i.color.a);
            }
            ENDCG
        }
    }
}
