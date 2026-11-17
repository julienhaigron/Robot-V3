Shader "Custom/RangeOverlay"
{
    Properties
    {
        [HideInInspector][NoScaleOffset] _RangeData ("Range Data", 2D) = "black" {}
        [HideInInspector] _RangeColor ("Range Color", Color) = (0, 0.375, 1, 1)
        [HideInInspector] _AoEColor ("AoE Color", Color) = (1, 0.5, 0, 1)
        [HideInInspector] _TargetColor ("Target Color", Color) = (0.55, 0.78, 1, 1)
        _FillOpacity ("Range Fill Opacity", Range(0, 1)) = 0.22
        _AoEFillOpacity ("AoE Fill Opacity", Range(0, 1)) = 0.3
        _TargetFillOpacity ("Target Fill Opacity", Range(0, 1)) = 0.4
        _LineWidth ("Line Width", Range(0.01, 0.4)) = 0.09
        _LineDarken ("Line Darken", Range(0, 1)) = 0.3
        _GlowWidth ("Glow Width", Range(0.01, 0.8)) = 0.45
        _GlowOpacity ("Glow Opacity", Range(0, 1)) = 0.5
        _GlowLighten ("Glow Lighten", Range(0, 1)) = 0.45
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2.4
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "Queue" = "Transparent-10" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        ZWrite Off
        ZTest LEqual
        Cull Off
        Offset -1, -1
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _RangeData;
            float4 _RangeGrid;
            float4 _RangeColor;
            float4 _AoEColor;
            float4 _TargetColor;
            float _FillOpacity;
            float _AoEFillOpacity;
            float _TargetFillOpacity;
            float _LineWidth;
            float _LineDarken;
            float _GlowWidth;
            float _GlowOpacity;
            float _GlowLighten;
            float _PulseSpeed;
            float _PulseAmount;

            static const float2 s_neighborOffsets[6] =
            {
                float2(1, 0), float2(0, 1), float2(-1, 1), float2(-1, 0), float2(0, -1), float2(1, -1)
            };

            static const float2 s_edgeNormals[6] =
            {
                float2(1, 0), float2(0.5, 0.8660254), float2(-0.5, 0.8660254), float2(-1, 0), float2(-0.5, -0.8660254), float2(0.5, -0.8660254)
            };

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 grid : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grid = v.vertex.xz;
                return o;
            }

            float4 GetLayers (float2 _axial)
            {
                float2 cell = float2(_axial.x + floor(_axial.y * 0.5), _axial.y);
                if (cell.x < 0.0 || cell.y < 0.0 || cell.x >= _RangeGrid.x || cell.y >= _RangeGrid.y)
                    return float4(0.0, 0.0, 0.0, 0.0);

                return step(0.5, tex2Dlod(_RangeData, float4((cell + 0.5) / _RangeGrid.xy, 0.0, 0.0)));
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

            float4 DrawLayer (float4 _below, float _isInside, float _edgeDistance, float3 _color, float _fillOpacity, float _pulse, float _pixelSize)
            {
                float halfLine = _LineWidth * 0.5;
                float lineMask = 1.0 - smoothstep(halfLine - _pixelSize, halfLine + _pixelSize, _edgeDistance);
                float4 color = _below;

                if (_isInside > 0.5)
                {
                    float glowMask = 1.0 - saturate(_edgeDistance / _GlowWidth);
                    glowMask *= glowMask;

                    color = Over(color, _color, _fillOpacity * _pulse);
                    color = Over(color, lerp(_color, float3(1.0, 1.0, 1.0), _GlowLighten), glowMask * _GlowOpacity * _pulse);
                }

                return Over(color, _color * (1.0 - _LineDarken), lineMask);
            }

            float4 frag (v2f i) : SV_Target
            {
                float innerRadius = _RangeGrid.z;
                float outerRadius = _RangeGrid.w;
                float pixelSize = max(fwidth(i.grid.x), fwidth(i.grid.y));

                float row = i.grid.y / (1.5 * outerRadius);
                float2 axial = RoundAxial(float2(i.grid.x / (2.0 * innerRadius) - row * 0.5, row));
                float2 center = float2((axial.x + axial.y * 0.5) * 2.0 * innerRadius, axial.y * 1.5 * outerRadius);
                float2 local = i.grid - center;
                float4 layers = GetLayers(axial);

                float4 edgeDistances = float4(1e4, 1e4, 1e4, 1e4);
                [unroll]
                for (int k = 0; k < 6; k++)
                {
                    float4 isBoundary = abs(GetLayers(axial + s_neighborOffsets[k]) - layers);
                    float edgeDistance = innerRadius - dot(local, s_edgeNormals[k]);
                    edgeDistances = lerp(edgeDistances, min(edgeDistances, edgeDistance), isBoundary);
                }

                float nearestEdge = min(min(edgeDistances.x, edgeDistances.y), edgeDistances.z);
                if (dot(layers.rgb, float3(1.0, 1.0, 1.0)) < 0.5 && nearestEdge > _LineWidth * 0.5 + pixelSize)
                    discard;

                float pulse = 1.0 - _PulseAmount * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed));
                float4 color = float4(_RangeColor.rgb, 0.0);
                color = DrawLayer(color, layers.r, edgeDistances.r, _RangeColor.rgb, _FillOpacity, pulse, pixelSize);
                color = DrawLayer(color, layers.g, edgeDistances.g, _AoEColor.rgb, _AoEFillOpacity, pulse, pixelSize);
                color = DrawLayer(color, layers.b, edgeDistances.b, _TargetColor.rgb, _TargetFillOpacity, pulse, pixelSize);
                return color;
            }
            ENDCG
        }
    }
}
