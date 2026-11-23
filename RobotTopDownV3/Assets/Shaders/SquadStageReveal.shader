Shader "UI/SquadStageReveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _LineColor ("Line Color", Color) = (0.55, 0.85, 1, 1)
        _LineWidth ("Line Width (px)", Float) = 2
        _LineSpread ("Line Spread off the unit (unit height)", Range(0, 2)) = 0.45
        _LineOffUnit ("Line Opacity off the unit", Range(0, 1)) = 0.3
        _GlitchHeight ("Glitch Height (px)", Float) = 18
        _GlitchAmount ("Glitch Amount (px)", Float) = 8
        _GlitchGlow ("Glitch Glow", Range(0, 1)) = 0.35
        _TintColor ("Trail Tint", Color) = (0.31, 0.71, 1, 0.3)
        _TintTrail ("Trail Length (unit height)", Range(0.01, 1)) = 0.35
        _ScanlinePeriod ("Scanline Period (px)", Float) = 4
        _ScanlineOpacity ("Scanline Opacity", Range(0, 1)) = 0.14
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Color;
            float4 _LineColor;
            float _LineWidth;
            float _LineSpread;
            float _LineOffUnit;
            float _GlitchHeight;
            float _GlitchAmount;
            float _GlitchGlow;
            float4 _TintColor;
            float _TintTrail;
            float _ScanlinePeriod;
            float _ScanlineOpacity;

            sampler2D _SquadStageHologram;
            sampler2D _SquadStageDecor;
            float4 _SquadStageSlots[8];
            float _SquadStageReveal[8];

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
                float2 size = max(_MainTex_TexelSize.zw, float2(1.0, 1.0));
                float2 uv = i.uv;

                float4 slot = _SquadStageSlots[0];
                float reveal = _SquadStageReveal[0];
                [unroll]
                for (int k = 1; k < 8; k++)
                {
                    bool isInSlot = uv.x >= _SquadStageSlots[k].x;
                    slot = isInSlot ? _SquadStageSlots[k] : slot;
                    reveal = isInSlot ? _SquadStageReveal[k] : reveal;
                }

                float heightPx = max(slot.w, 0.001) * size.y;
                float behind = (slot.z - uv.y) * size.y;
                float isActive = saturate(reveal * 12.0) * saturate((1.0 - reveal) * 12.0);

                float band = saturate(1.0 + behind / max(_GlitchHeight, 1.0)) * step(behind, 0.0) * isActive;
                float noise = frac(sin(floor(uv.y * size.y / 3.0) * 91.7 + floor(_Time.y * 30.0) * 13.3) * 43758.5453);
                float4 hologram = tex2D(_SquadStageHologram, float2(uv.x + (noise - 0.5) * band * _GlitchAmount / size.x, uv.y));
                hologram.a = sqrt(saturate(hologram.a));
                hologram.rgb += _LineColor.rgb * (band * _GlitchGlow * hologram.a);

                float4 real = tex2D(_MainTex, uv);
                float trail = saturate(1.0 - behind / max(_TintTrail * heightPx, 1.0)) * step(0.0, behind) * isActive;
                float scanline = step(frac(uv.y * size.y / max(_ScanlinePeriod, 1.0)), 0.3) * _ScanlineOpacity;
                float tint = saturate(_TintColor.a + scanline) * trail;
                float4 loaded = float4(lerp(real.rgb, _TintColor.rgb * real.a, tint), real.a);

                float coverage = saturate(max(real.a, hologram.a) * 2.0);
                float spread = saturate(1.0 - abs(uv.x - slot.y) * size.x / max(_LineSpread * heightPx, 1.0));
                float lineAlpha = lerp(_LineOffUnit * spread, 1.0, coverage);
                float lineMask = saturate(_LineWidth * 0.5 + 0.5 - abs(behind)) * isActive;

                float4 color = behind > 0.0 ? loaded : hologram;
                color = lerp(color, float4(_LineColor.rgb, 1.0) * lineAlpha, lineMask);
                color += tex2D(_SquadStageDecor, uv) * (1.0 - color.a);
                return color * i.color.a;
            }
            ENDCG
        }
    }
}
