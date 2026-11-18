Shader "Custom/RadarBlip"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.16, 0.12, 1)
        _FlashColor ("Flash Color", Color) = (1, 0.8, 0.7, 1)
        _CoreRadius ("Core Radius", Range(0.01, 0.5)) = 0.09
        _CoreOutline ("Core Outline Width", Range(0, 0.1)) = 0.018
        _CoreOutlineOpacity ("Core Outline Opacity", Range(0, 1)) = 0.6
        _GlowRadius ("Glow Radius", Range(0.01, 0.5)) = 0.28
        _GlowOpacity ("Glow Opacity", Range(0, 1)) = 0.45
        _IdlePulse ("Idle Pulse", Range(0, 1)) = 0.12
        _IdlePulseSpeed ("Idle Pulse Speed", Range(0, 10)) = 1.6
        _PingFlash ("Ping Flash", Range(0, 1)) = 0.85
        _PingDecay ("Ping Flash Decay", Range(0.5, 20)) = 3.5
        _PingGrowth ("Ping Core Growth", Range(0, 1)) = 0.35
        _PingRingDuration ("Ping Ring Duration", Range(0.1, 3)) = 0.7
        _PingRingWidth ("Ping Ring Width", Range(0.005, 0.2)) = 0.035
        _PingRingOpacity ("Ping Ring Opacity", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+100" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float4 _Color;
            float4 _FlashColor;
            float _CoreRadius;
            float _CoreOutline;
            float _CoreOutlineOpacity;
            float _GlowRadius;
            float _GlowOpacity;
            float _IdlePulse;
            float _IdlePulseSpeed;
            float _PingFlash;
            float _PingDecay;
            float _PingGrowth;
            float _PingRingDuration;
            float _PingRingWidth;
            float _PingRingOpacity;

            float4 _RadarPingCenter;
            float4 _RadarPingTiming;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 local : TEXCOORD0;
                float3 origin : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.local = v.vertex.xy;
                o.origin = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0)).xyz;
                return o;
            }

            float4 Over (float4 _below, float3 _color, float _alpha)
            {
                float alpha = _alpha + _below.a * (1.0 - _alpha);
                float3 color = alpha > 0.0001
                    ? (_color * _alpha + _below.rgb * _below.a * (1.0 - _alpha)) / alpha
                    : _color;

                return float4(color, alpha);
            }

            float GetPingAge (float3 _origin)
            {
                float age = 1e4;
                if (_RadarPingCenter.w > 0.5)
                {
                    float radius = max(_RadarPingCenter.z, 0.001);
                    float distanceToCenter = length(_origin.xz - _RadarPingCenter.xy);
                    if (distanceToCenter <= radius)
                    {
                        float rate = max(_RadarPingTiming.x, 0.001);
                        float count = max(_RadarPingTiming.y, 1.0);
                        float cyclesSinceFront = frac((_Time.y * rate - distanceToCenter / radius) * count) / count;
                        age = cyclesSinceFront / rate;
                    }
                }

                return age;
            }

            float4 frag (v2f i) : SV_Target
            {
                float distanceToCenter = length(i.local);
                float pixel = max(fwidth(distanceToCenter), 0.0001);

                float age = GetPingAge(i.origin);
                float flash = exp(-age * _PingDecay) * _PingFlash;
                float idle = 1.0 - _IdlePulse * (0.5 + 0.5 * sin(_Time.y * _IdlePulseSpeed));
                float coreRadius = _CoreRadius * (1.0 + _PingGrowth * flash);

                float glow = saturate(1.0 - distanceToCenter / max(_GlowRadius * (1.0 + flash), 0.001));
                float4 color = Over(float4(_Color.rgb, 0.0), _Color.rgb, glow * glow * saturate(_GlowOpacity * idle + flash * 0.5));

                float ringProgress = saturate(age / _PingRingDuration);
                float ringRadius = lerp(coreRadius, 0.48, ringProgress);
                float ringMask = 1.0 - smoothstep(_PingRingWidth * 0.5 - pixel, _PingRingWidth * 0.5 + pixel, abs(distanceToCenter - ringRadius));
                float ringFade = (1.0 - ringProgress) * (1.0 - ringProgress);
                color = Over(color, lerp(_Color.rgb, _FlashColor.rgb, 0.5), ringMask * ringFade * _PingRingOpacity);

                float outlineRadius = coreRadius + _CoreOutline;
                float outlineMask = 1.0 - smoothstep(outlineRadius - pixel, outlineRadius + pixel, distanceToCenter);
                color = Over(color, _Color.rgb * 0.2, outlineMask * _CoreOutlineOpacity);

                float coreMask = 1.0 - smoothstep(coreRadius - pixel, coreRadius + pixel, distanceToCenter);
                color = Over(color, lerp(_Color.rgb * idle, _FlashColor.rgb, saturate(flash)), coreMask);

                if (color.a < 0.002)
                    discard;

                return color;
            }
            ENDCG
        }
    }
}
