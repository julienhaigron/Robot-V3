Shader "Custom/OrderPath"
{
    Properties
    {
        _EdgeColor ("Edge Color", Color) = (0.024, 0.031, 0.071, 0.85)
        _EdgeWidth ("Edge Width (share of the half width)", Range(0, 0.6)) = 0.33
        _ChevronColor ("Chevron Color", Color) = (1, 1, 1, 0.92)
        _ChevronSpacing ("Chevron Spacing (m)", Float) = 0.46
        _ChevronArm ("Chevron Arm (m)", Float) = 0.075
        _ChevronThickness ("Chevron Thickness (m)", Float) = 0.055
        _ScrollSpeed ("Chevron Scroll Speed (m/s)", Float) = 0.45
        _DashLength ("Dash Length (m, 0 = solid)", Float) = 0
        _DashGap ("Dash Gap (m)", Float) = 0.1
        _Opacity ("Opacity", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
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

            float4 _EdgeColor;
            float _EdgeWidth;
            float4 _ChevronColor;
            float _ChevronSpacing;
            float _ChevronArm;
            float _ChevronThickness;
            float _ScrollSpeed;
            float _DashLength;
            float _DashGap;
            float _Opacity;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 strip : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 strip : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                o.strip = v.strip;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float along = i.uv.x;
                float across = abs(i.uv.y);
                float halfWidth = i.strip.x;
                float acrossMetres = across * halfWidth;
                float aaAcross = max(fwidth(across), 1e-4);
                float aaAlong = max(fwidth(along), 1e-4);

                float coverage = 1.0 - smoothstep(1.0 - aaAcross, 1.0, across);
                float edge = smoothstep(1.0 - _EdgeWidth - aaAcross, 1.0 - _EdgeWidth + aaAcross, across);
                float4 color = lerp(i.color, _EdgeColor, edge);

                float local = (frac((along - _Time.y * _ScrollSpeed) / _ChevronSpacing) - 0.5) * _ChevronSpacing;
                float halfThickness = _ChevronThickness * 0.5;
                float chevron = (1.0 - smoothstep(halfThickness - aaAlong, halfThickness + aaAlong, abs(local + acrossMetres)))
                    * (1.0 - smoothstep(_ChevronArm - aaAcross * halfWidth, _ChevronArm + aaAcross * halfWidth, acrossMetres))
                    * i.strip.y * _ChevronColor.a;
                color.rgb = lerp(color.rgb, _ChevronColor.rgb, chevron);

                if (_DashLength > 0.0)
                {
                    float period = _DashLength + _DashGap;
                    float position = frac(along / period) * period;
                    coverage *= smoothstep(0.0, aaAlong, position) * (1.0 - smoothstep(_DashLength - aaAlong, _DashLength, position));
                }

                return float4(color.rgb, color.a * coverage * _Opacity);
            }
            ENDCG
        }
    }
}
