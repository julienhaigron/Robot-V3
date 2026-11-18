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
        [HideInInspector] _VisionType ("Vision Type", Float) = 0
        _OpticColor ("Optic Vision Color", Color) = (0.945, 0.937, 0.91, 1)
        _ThermicColor ("Thermic Vision Color", Color) = (0.847, 0.353, 0.188, 1)
        _RadarColor ("Radar Vision Color", Color) = (0.498, 0.467, 0.867, 1)
        _OpticFillOpacity ("Optic Fill Opacity", Range(0, 1)) = 0.12
        _ThermicFillOpacity ("Thermic Fill Opacity", Range(0, 1)) = 0.16
        _RadarFillOpacity ("Radar Fill Opacity", Range(0, 1)) = 0
        _VisionLineWidth ("Vision Line Width", Range(0.01, 0.4)) = 0.08
        _VisionDashLength ("Vision Dash Length", Range(0.05, 2)) = 0.35
        _VisionDashRatio ("Vision Dash Ratio", Range(0.05, 1)) = 0.6
        _VisionOutlineOpacity ("Vision Outline Opacity", Range(0, 1)) = 0.35
        [HideInInspector] _VisionCenter ("Vision Center", Vector) = (0, 0, 1, 0)
        _RadarWaveRate ("Radar Wave Rate", Range(0.05, 3)) = 0.45
        [IntRange] _RadarWaveCount ("Radar Wave Count", Range(1, 4)) = 2
        _RadarWaveWidth ("Radar Wave Width", Range(0.05, 3)) = 0.35
        _RadarWaveOpacity ("Radar Wave Opacity", Range(0, 1)) = 0.75
        _ThermicGradientOpacity ("Thermic Gradient Opacity", Range(0, 1)) = 0.5
        _ThermicShimmer ("Thermic Shimmer", Range(0, 0.5)) = 0.14
        _ThermicShimmerScale ("Thermic Shimmer Scale", Range(0.05, 4)) = 1.1
        _ThermicShimmerSpeed ("Thermic Shimmer Speed", Range(0, 3)) = 0.35
        _ThermicCold ("Thermic Cold", Color) = (0.118, 0.031, 0.235, 1)
        _ThermicCool ("Thermic Cool", Color) = (0.471, 0.078, 0.431, 1)
        _ThermicWarm ("Thermic Warm", Color) = (0.863, 0.235, 0.157, 1)
        _ThermicHot ("Thermic Hot", Color) = (1, 0.627, 0.118, 1)
        _ThermicCore ("Thermic Core", Color) = (1, 0.961, 0.784, 1)
        _WallEffectOpacity ("Wall Effect Opacity", Range(0, 1)) = 1
        _WallMinHeight ("Wall Effect Min Height", Range(0, 0.5)) = 0.05
        _WallEdgeTolerance ("Wall Edge Tolerance", Range(0, 0.3)) = 0.06
        [HideInInspector] _OverlayCull ("Overlay Cull", Float) = 0
        [HideInInspector] _OverlayZTest ("Overlay ZTest", Float) = 4
    }

    SubShader
    {
        Tags { "Queue" = "Transparent-10" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        ZWrite Off
        ZTest [_OverlayZTest]
        Cull [_OverlayCull]
        Offset -1, -1
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local __ RANGE_OVERLAY_WALLS
            #include "UnityCG.cginc"

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
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
            float _VisionType;
            float4 _OpticColor;
            float4 _ThermicColor;
            float4 _RadarColor;
            float _OpticFillOpacity;
            float _ThermicFillOpacity;
            float _RadarFillOpacity;
            float _VisionLineWidth;
            float _VisionDashLength;
            float _VisionDashRatio;
            float _VisionOutlineOpacity;
            float4 _VisionCenter;
            float _RadarWaveRate;
            float _RadarWaveCount;
            float _RadarWaveWidth;
            float _RadarWaveOpacity;
            float _WallEffectOpacity;
            float _WallMinHeight;
            float _WallEdgeTolerance;
            float _ThermicGradientOpacity;
            float _ThermicShimmer;
            float _ThermicShimmerScale;
            float _ThermicShimmerSpeed;
            float4 _ThermicCold;
            float4 _ThermicCool;
            float4 _ThermicWarm;
            float4 _ThermicHot;
            float4 _ThermicCore;

            static const float2 s_neighborOffsets[6] =
            {
                float2(1, 0), float2(0, 1), float2(-1, 1), float2(-1, 0), float2(0, -1), float2(1, -1)
            };

            static const float2 s_edgeNormals[6] =
            {
                float2(1, 0), float2(0.5, 0.8660254), float2(-0.5, 0.8660254), float2(-1, 0), float2(-0.5, -0.8660254), float2(0.5, -0.8660254)
            };

            static const float2 s_edgeTangents[6] =
            {
                float2(0, 1), float2(0.8660254, -0.5), float2(0.8660254, 0.5), float2(0, 1), float2(0.8660254, -0.5), float2(0.8660254, 0.5)
            };

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 grid : TEXCOORD0;
            #if defined(RANGE_OVERLAY_WALLS)
                float4 screenPos : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float eyeDepth : TEXCOORD3;
            #endif
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grid = v.vertex.xz;
            #if defined(RANGE_OVERLAY_WALLS)
                o.screenPos = ComputeScreenPos(o.pos);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.eyeDepth = -UnityObjectToViewPos(v.vertex.xyz).z;
            #endif
                return o;
            }

            float4 GetRawLayers (float2 _axial)
            {
                float2 cell = float2(_axial.x + floor(_axial.y * 0.5), _axial.y);
                if (cell.x < 0.0 || cell.y < 0.0 || cell.x >= _RangeGrid.x || cell.y >= _RangeGrid.y)
                    return float4(0.0, 0.0, 0.0, 0.0);

                return tex2Dlod(_RangeData, float4((cell + 0.5) / _RangeGrid.xy, 0.0, 0.0));
            }

            float4 GetLayers (float2 _axial)
            {
                return step(0.5, GetRawLayers(_axial));
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

            float Hash (float2 _p)
            {
                return frac(sin(dot(_p, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise (float2 _p)
            {
                float2 cell = floor(_p);
                float2 f = frac(_p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash(cell);
                float b = Hash(cell + float2(1.0, 0.0));
                float c = Hash(cell + float2(0.0, 1.0));
                float d = Hash(cell + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm (float2 _p)
            {
                return 0.55 * ValueNoise(_p) + 0.3 * ValueNoise(_p * 2.1 + float2(5.2, 1.3)) + 0.15 * ValueNoise(_p * 4.3 + float2(9.1, 3.7));
            }

            float3 ThermicPalette (float _heat)
            {
                float heat = saturate(_heat);
                float3 color = lerp(_ThermicCold.rgb, _ThermicCool.rgb, saturate(heat / 0.3));
                color = lerp(color, _ThermicWarm.rgb, saturate((heat - 0.3) / 0.25));
                color = lerp(color, _ThermicHot.rgb, saturate((heat - 0.55) / 0.23));
                return lerp(color, _ThermicCore.rgb, saturate((heat - 0.78) / 0.22));
            }

            float4 DrawThermicGradient (float4 _below, float2 _offset)
            {
                float normalizedDistance = saturate(length(_offset) / max(_VisionCenter.z, 0.001));
                float2 noisePosition = _offset * _ThermicShimmerScale + float2(_Time.y, -_Time.y * 0.57) * _ThermicShimmerSpeed;
                float heat = 1.0 - normalizedDistance + _ThermicShimmer * (Fbm(noisePosition) - 0.5);
                return Over(_below, ThermicPalette(heat), _ThermicGradientOpacity);
            }

            float4 DrawRadarWaves (float4 _below, float3 _color, float2 _offset)
            {
                float radius = max(_VisionCenter.z, 0.001);
                float distanceToCenter = length(_offset);
                float count = max(round(_RadarWaveCount), 1.0);
                float width = max(_RadarWaveWidth, 0.001);
                float wave = 0.0;

                [unroll]
                for (int w = 0; w < 4; w++)
                {
                    if (w < count)
                    {
                        float progress = frac(_Time.y * _RadarWaveRate + w / count);
                        float gap = (distanceToCenter - progress * radius) / width;
                        wave = max(wave, exp(-gap * gap) * (1.0 - progress));
                    }
                }

                return Over(_below, _color, saturate(wave * _RadarWaveOpacity));
            }

            float3 GetVisionColor ()
            {
                return _VisionType < 0.5 ? _OpticColor.rgb : _VisionType < 1.5 ? _ThermicColor.rgb : _RadarColor.rgb;
            }

            float4 DrawVisionFill (float4 _below, float2 _grid)
            {
                float2 offset = _grid - _VisionCenter.xy;
                float4 color = _below;
                if (_VisionType > 0.5 && _VisionType < 1.5)
                    color = DrawThermicGradient(color, offset);
                else
                {
                    float3 visionColor = GetVisionColor();
                    color = Over(color, visionColor, _VisionType < 0.5 ? _OpticFillOpacity : _RadarFillOpacity);
                    if (_VisionType > 1.5)
                        color = DrawRadarWaves(color, visionColor, offset);
                }

                return color;
            }

            float4 DrawVision (float4 _below, float _isInside, float _edgeDistance, float _along, float2 _grid, float _pixelSize)
            {
                float3 visionColor = GetVisionColor();

                float dashPhase = frac(_along / _VisionDashLength);
                float dashSoftness = _pixelSize / _VisionDashLength;
                float dashMask = smoothstep(0.0, dashSoftness, dashPhase)
                    * (1.0 - smoothstep(_VisionDashRatio - dashSoftness, _VisionDashRatio + dashSoftness, dashPhase));

                float halfLine = _VisionLineWidth * 0.5;
                float lineMask = (1.0 - smoothstep(halfLine - _pixelSize, halfLine + _pixelSize, _edgeDistance)) * dashMask;
                float outlineMask = (1.0 - smoothstep(halfLine + _pixelSize, halfLine + 2.0 * _pixelSize, _edgeDistance)) * dashMask;

                float4 color = _below;
                if (_isInside > 0.5)
                    color = DrawVisionFill(color, _grid);

                color = Over(color, float3(0.0, 0.0, 0.0), outlineMask * _VisionOutlineOpacity);
                return Over(color, visionColor, lineMask);
            }

            float2 GetAxial (float2 _grid, out float2 _local)
            {
                float row = _grid.y / (1.5 * _RangeGrid.w);
                float2 axial = RoundAxial(float2(_grid.x / (2.0 * _RangeGrid.z) - row * 0.5, row));
                _local = _grid - float2((axial.x + axial.y * 0.5) * 2.0 * _RangeGrid.z, axial.y * 1.5 * _RangeGrid.w);
                return axial;
            }

        #if defined(RANGE_OVERLAY_WALLS)
            float GetSceneEyeDepth (float _rawDepth)
            {
                float perspectiveDepth = LinearEyeDepth(_rawDepth);
            #if defined(UNITY_REVERSED_Z)
                float orthographicDepth = lerp(_ProjectionParams.z, _ProjectionParams.y, _rawDepth);
            #else
                float orthographicDepth = lerp(_ProjectionParams.y, _ProjectionParams.z, _rawDepth);
            #endif
                return lerp(perspectiveDepth, orthographicDepth, unity_OrthoParams.w);
            }

            float3 GetSceneWorldPosition (v2f i)
            {
                float rawDepth = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(i.screenPos.xy / i.screenPos.w, 0.0, 0.0));
                float sceneDepth = GetSceneEyeDepth(rawDepth);
                float3 perspectivePosition = _WorldSpaceCameraPos + (i.worldPos - _WorldSpaceCameraPos) * (sceneDepth / max(i.eyeDepth, 0.0001));
                float3 orthographicPosition = i.worldPos - UNITY_MATRIX_V[2].xyz * (sceneDepth - i.eyeDepth);
                return lerp(perspectivePosition, orthographicPosition, unity_OrthoParams.w);
            }

            float2 GetWallVision (float2 _axial)
            {
                float code = floor(GetRawLayers(_axial).a * 255.0 + 0.5);
                return float2(step(127.5, code), step(31.5, fmod(code, 64.0)));
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 gridPosition = mul(unity_WorldToObject, float4(GetSceneWorldPosition(i), 1.0)).xyz;
                if (gridPosition.y < _WallMinHeight)
                    discard;

                float2 local;
                float2 axial = GetAxial(gridPosition.xz, local);
                float2 wallVision = GetWallVision(axial);

                if (wallVision.y < 0.5)
                {
                    float nearestEdge = 1e4;
                    float2 nearestNeighbor = float2(0.0, 0.0);
                    [unroll]
                    for (int k = 0; k < 6; k++)
                    {
                        float edgeDistance = _RangeGrid.z - dot(local, s_edgeNormals[k]);
                        if (edgeDistance < nearestEdge)
                        {
                            nearestEdge = edgeDistance;
                            nearestNeighbor = s_neighborOffsets[k];
                        }
                    }

                    if (nearestEdge < _WallEdgeTolerance)
                        wallVision = GetWallVision(axial + nearestNeighbor);
                }

                if (wallVision.x < 0.5 || wallVision.y < 0.5)
                    discard;

                float4 color = DrawVisionFill(float4(0.0, 0.0, 0.0, 0.0), gridPosition.xz);
                color.a *= _WallEffectOpacity;
                return color;
            }
        #else
            float4 frag (v2f i) : SV_Target
            {
                float innerRadius = _RangeGrid.z;
                float pixelSize = max(fwidth(i.grid.x), fwidth(i.grid.y));

                float2 local;
                float2 axial = GetAxial(i.grid, local);
                float4 layers = GetLayers(axial);

                float4 edgeDistances = float4(1e4, 1e4, 1e4, 1e4);
                float visionAlong = 0.0;
                [unroll]
                for (int k = 0; k < 6; k++)
                {
                    float4 isBoundary = abs(GetLayers(axial + s_neighborOffsets[k]) - layers);
                    float edgeDistance = innerRadius - dot(local, s_edgeNormals[k]);

                    if (isBoundary.a > 0.5 && edgeDistance < edgeDistances.a)
                        visionAlong = dot(i.grid, s_edgeTangents[k]);

                    edgeDistances = lerp(edgeDistances, min(edgeDistances, edgeDistance), isBoundary);
                }

                float nearestEdge = min(min(edgeDistances.x, edgeDistances.y), edgeDistances.z);
                bool isNearLine = nearestEdge <= _LineWidth * 0.5 + pixelSize || edgeDistances.a <= _VisionLineWidth * 0.5 + 2.0 * pixelSize;
                if (dot(layers, float4(1.0, 1.0, 1.0, 1.0)) < 0.5 && !isNearLine)
                    discard;

                float pulse = 1.0 - _PulseAmount * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed));
                float4 color = float4(_RangeColor.rgb, 0.0);
                color = DrawLayer(color, layers.r, edgeDistances.r, _RangeColor.rgb, _FillOpacity, pulse, pixelSize);
                color = DrawVision(color, layers.a, edgeDistances.a, visionAlong, i.grid, pixelSize);
                color = DrawLayer(color, layers.g, edgeDistances.g, _AoEColor.rgb, _AoEFillOpacity, pulse, pixelSize);
                color = DrawLayer(color, layers.b, edgeDistances.b, _TargetColor.rgb, _TargetFillOpacity, pulse, pixelSize);
                return color;
            }
        #endif
            ENDCG
        }
    }
}
