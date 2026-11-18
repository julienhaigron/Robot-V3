Shader "UI/BriefingHoloMap"
{
    Properties
    {
        [PerRendererData] _MainTex ("Map Data", 2D) = "black" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Reveal ("Reveal", Range(0, 1)) = 1
        _LineColor ("Line Color", Color) = (0.5, 0.83, 1, 0.9)
        _FloorColor ("Floor Color", Color) = (0.5, 0.83, 1, 0.07)
        _WallColor ("Wall Color", Color) = (0.5, 0.83, 1, 0.34)
        _CoverColor ("Cover Color", Color) = (0.5, 0.83, 1, 0.2)
        _ObjectiveColor ("Objective Color", Color) = (0.91, 0.7, 0.23, 0.5)
        _SpawnColor ("Squad Spawn Color", Color) = (0.36, 0.55, 0.9, 0.6)
        _LineWidth ("Line Width (tile units)", Range(0.01, 0.3)) = 0.05
        _FrontWidth ("Scan Front Width", Range(0.005, 0.2)) = 0.03
        _ScanlineOpacity ("Scanline Opacity", Range(0, 1)) = 0.18
        [HideInInspector] _MapGrid ("Map Grid", Vector) = (1, 1, 0.866, 1)
        [HideInInspector] _MapBounds ("Map Bounds", Vector) = (0, 0, 1, 1)
        [HideInInspector] _RevealCenter ("Reveal Center", Vector) = (0, 0, 1, 0)
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
            float _Reveal;
            float4 _LineColor;
            float4 _FloorColor;
            float4 _WallColor;
            float4 _CoverColor;
            float4 _ObjectiveColor;
            float4 _SpawnColor;
            float _LineWidth;
            float _FrontWidth;
            float _ScanlineOpacity;
            float4 _MapGrid;
            float4 _MapBounds;
            float4 _RevealCenter;

            static const float2 s_edgeNormals[6] =
            {
                float2(1, 0), float2(0.5, 0.8660254), float2(-0.5, 0.8660254), float2(-1, 0), float2(-0.5, -0.8660254), float2(0.5, -0.8660254)
            };

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

            float2 RoundAxial (float2 _axial)
            {
                float3 cube = float3(_axial.x, -_axial.x - _axial.y, _axial.y);
                float3 rounded = round(cube);
                float3 delta = abs(rounded - cube);

                if (delta.x > delta.y && delta.x > delta.z)
                    rounded.x = -rounded.y - rounded.z;
                else if (delta.y > delta.z)
                    rounded.y = -rounded.x - rounded.z;
                else
                    rounded.z = -rounded.x - rounded.y;

                return rounded.xz;
            }

            float4 Over (float4 _below, float3 _color, float _alpha)
            {
                float alpha = _alpha + _below.a * (1.0 - _alpha);
                float3 color = alpha > 0.0001
                    ? (_color * _alpha + _below.rgb * _below.a * (1.0 - _alpha)) / alpha
                    : _color;

                return float4(color, alpha);
            }

            float4 frag (v2f i) : SV_Target
            {
                float innerRadius = _MapGrid.z;
                float outerRadius = _MapGrid.w;
                float2 map = lerp(_MapBounds.xy, _MapBounds.zw, i.uv);
                float pixel = max(fwidth(map.x), fwidth(map.y));

                float row = map.y / (1.5 * outerRadius);
                float2 axial = RoundAxial(float2(map.x / (2.0 * innerRadius) - row * 0.5, row));
                float2 cell = float2(axial.x + floor(axial.y * 0.5), axial.y);
                float isInGrid = step(0.0, cell.x) * step(0.0, cell.y) * step(cell.x, _MapGrid.x - 1.0) * step(cell.y, _MapGrid.y - 1.0);
                float code = floor(tex2Dlod(_MainTex, float4((cell + 0.5) / _MapGrid.xy, 0.0, 0.0)).r * 255.0 + 0.5) * isInGrid;

                float2 center = float2((axial.x + axial.y * 0.5) * 2.0 * innerRadius, axial.y * 1.5 * outerRadius);
                float2 local = map - center;
                float edgeDistance = 1e4;
                [unroll]
                for (int k = 0; k < 6; k++)
                    edgeDistance = min(edgeDistance, innerRadius - dot(local, s_edgeNormals[k]));

                float4 fill = code < 1.5 ? _FloorColor : code < 2.5 ? _WallColor : code < 3.5 ? _CoverColor : code < 4.5 ? _ObjectiveColor : _SpawnColor;
                float3 lineColor = code > 3.5 && code < 4.5 ? _ObjectiveColor.rgb : _LineColor.rgb;
                float outline = 1.0 - smoothstep(_LineWidth - pixel, _LineWidth + pixel, edgeDistance);

                float distanceToCenter = length(center - _RevealCenter.xy) / max(_RevealCenter.z, 0.001);
                float front = _Reveal * 1.08;
                float isTile = step(0.5, code);
                float visible = saturate((front - distanceToCenter) / 0.04) * isTile;
                float gap = (distanceToCenter - front) / max(_FrontWidth, 0.0001);
                float ring = exp(-gap * gap) * isTile * (1.0 - step(0.999, _Reveal));

                float4 color = float4(fill.rgb, fill.a * visible);
                color = Over(color, lineColor, outline * _LineColor.a * visible);
                color = Over(color, _LineColor.rgb, ring * 0.7);

                float flicker = 0.94 + 0.06 * sin(_Time.y * 3.0 + center.y * 0.7);
                float scanline = 1.0 - _ScanlineOpacity * step(frac(i.pos.y * 0.25), 0.5);
                color.a *= flicker * scanline * i.color.a;
                return color;
            }
            ENDCG
        }
    }
}
