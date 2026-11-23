Shader "Custom/SquadStageFloor"
{
    Properties
    {
        _BaseColor ("Floor Color", Color) = (0.04, 0.09, 0.13, 0.6)
        _GridColor ("Grid Color", Color) = (0.36, 0.7, 0.92, 0.3)
        _GridSize ("Grid Size (m)", Float) = 1
        _GridWidth ("Grid Width (m)", Float) = 0.015
        _PadColor ("Pad Color", Color) = (0.5, 0.82, 1, 1)
        _PadRadius ("Pad Radius (m)", Float) = 0.72
        _PadWidth ("Pad Ring Width (m)", Float) = 0.045
        _InnerRadius ("Inner Ring Radius (m)", Float) = 0.48
        _GlowStrength ("Pad Glow", Range(0, 2)) = 0.35
        _FlashStrength ("Flash", Range(0, 4)) = 1.2
        _ShadowRadius ("Contact Shadow Radius (m)", Float) = 0.78
        _ShadowStrength ("Contact Shadow", Range(0, 1)) = 0.75
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        ZWrite Off
        Cull Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float4 _BaseColor;
            float4 _GridColor;
            float _GridSize;
            float _GridWidth;
            float4 _PadColor;
            float _PadRadius;
            float _PadWidth;
            float _InnerRadius;
            float _GlowStrength;
            float _FlashStrength;
            float _ShadowRadius;
            float _ShadowStrength;

            float4 _SquadStageFloor;
            float4 _SquadStageUnits[8];

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float Ring (float distanceToCenter, float radius, float width)
            {
                float edge = abs(distanceToCenter - radius) - width * 0.5;
                float smoothing = max(fwidth(distanceToCenter), 0.0001);
                return 1.0 - smoothstep(0.0, smoothing, edge);
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 position = i.worldPos.xz;
                float2 offset = (position - _SquadStageFloor.xy) / max(_SquadStageFloor.zw, float2(0.01, 0.01));
                float area = saturate(1.0 - dot(offset, offset));
                area *= area;
                float4 color = float4(_BaseColor.rgb * _BaseColor.a, _BaseColor.a) * area;

                float2 cell = abs(frac(position / _GridSize + 0.5) - 0.5) * _GridSize;
                float lineDistance = min(cell.x, cell.y);
                float smoothing = max(fwidth(lineDistance), 0.0001);
                float grid = 1.0 - smoothstep(_GridWidth * 0.5, _GridWidth * 0.5 + smoothing, lineDistance);
                color.rgb += _GridColor.rgb * (_GridColor.a * grid * area);

                [unroll]
                for (int k = 0; k < 8; k++)
                {
                    float4 unit = _SquadStageUnits[k];
                    float distanceToUnit = distance(position, unit.xy);
                    float shadow = (1.0 - smoothstep(0.0, _ShadowRadius, distanceToUnit)) * _ShadowStrength * unit.z;
                    color = color * (1.0 - shadow) + float4(0.0, 0.0, 0.0, shadow);

                    float flash = unit.w * _FlashStrength;
                    float glow = 1.0 - smoothstep(0.0, _PadRadius * 1.15, distanceToUnit);
                    float light = glow * glow * (_GlowStrength + flash)
                        + Ring(distanceToUnit, _PadRadius, _PadWidth) * (0.85 + flash)
                        + Ring(distanceToUnit, _InnerRadius, _PadWidth * 0.4) * (0.45 + flash * 0.5);
                    color.rgb += _PadColor.rgb * light * unit.z;
                }

                return color;
            }
            ENDCG
        }
    }
}
