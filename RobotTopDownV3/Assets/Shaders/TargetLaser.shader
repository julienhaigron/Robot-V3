Shader "Custom/TargetLaser"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (2.4, 0.09, 0.06, 1)
        _Core ("Core (share of the width)", Range(0, 1)) = 0.22
        _GlowFalloff ("Glow Falloff", Range(0.5, 8)) = 2.2
        _GlowStrength ("Glow Strength", Range(0, 1)) = 0.45
        _FadeIn ("Fade In From The Start (m)", Float) = 0.35
        _PulseSpacing ("Pulse Spacing (m, 0 = none)", Float) = 2.5
        _PulseSpeed ("Pulse Speed (m/s)", Float) = 5
        _PulseLength ("Pulse Length (m)", Float) = 0.6
        _PulseBoost ("Pulse Boost", Range(0, 3)) = 1.2
        _DashLength ("Dash Length (m, 0 = solid)", Float) = 0
        _DashGap ("Dash Gap (m)", Float) = 0.12
        _Opacity ("Opacity", Range(0, 1)) = 1
        [Toggle(_DOT)] _Dot ("Radial Dot", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        ZWrite Off
        ZTest [_ZTest]
        Cull Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma shader_feature_local _DOT
            #include "UnityCG.cginc"

            float4 _Color;
            float _Core;
            float _GlowFalloff;
            float _GlowStrength;
            float _FadeIn;
            float _PulseSpacing;
            float _PulseSpeed;
            float _PulseLength;
            float _PulseBoost;
            float _DashLength;
            float _DashGap;
            float _Opacity;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
            #if _DOT
                float radius = saturate(length(i.uv - 0.5) * 2.0);
                float aa = max(fwidth(radius), 1e-4);
                float core = 1.0 - smoothstep(_Core - aa, _Core + aa, radius);
                float glow = pow(1.0 - radius, _GlowFalloff);
                float boost = 1.0;
            #else
                float across = saturate(abs(i.uv.y - 0.5) * 2.0);
                float aa = max(fwidth(across), 1e-4);
                float core = 1.0 - smoothstep(_Core - aa, _Core + aa, across);
                float glow = pow(1.0 - across, _GlowFalloff);

                float along = i.uv.x;
                float mask = saturate(along / max(_FadeIn, 1e-4));
                if (_DashLength > 0.0)
                {
                    float period = _DashLength + _DashGap;
                    float aaAlong = max(fwidth(along), 1e-4);
                    mask *= 1.0 - smoothstep(_DashLength - aaAlong, _DashLength + aaAlong, fmod(along, period));
                }

                float boost = 1.0;
                if (_PulseSpacing > 0.0)
                {
                    float behindHead = frac((_Time.y * _PulseSpeed - along) / _PulseSpacing) * _PulseSpacing;
                    boost += saturate(1.0 - behindHead / max(_PulseLength, 1e-4)) * _PulseBoost;
                }

                core *= mask;
                glow *= mask;
            #endif
                float weight = _Opacity * i.color.a;
                float alpha = saturate(core + glow * _GlowStrength) * weight;
                float3 rgb = _Color.rgb * i.color.rgb * (core * boost + glow * _GlowStrength) * weight;
                return float4(rgb, alpha);
            }
            ENDCG
        }
    }
}
