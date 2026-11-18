Shader "UI/PanelTransitionDoors"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _DoorColor ("Door Color", Color) = (0.17, 0.19, 0.24, 1)
        _DoorLineColor ("Door Line Color", Color) = (0.25, 0.28, 0.34, 1)
        _StripeColor ("Stripe Color", Color) = (0.91, 0.7, 0.23, 1)
        _StripeDarkColor ("Stripe Dark Color", Color) = (0.1, 0.11, 0.14, 1)
        _StripeWidth ("Stripe Width (px)", Float) = 32
        _StripePeriod ("Stripe Period (px)", Float) = 38
        _PanelSpacing ("Panel Spacing (px)", Float) = 120
        _LineThickness ("Line Thickness (px)", Float) = 3
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

            float4 _Color;
            float4 _DoorColor;
            float4 _DoorLineColor;
            float4 _StripeColor;
            float4 _StripeDarkColor;
            float _StripeWidth;
            float _StripePeriod;
            float _PanelSpacing;
            float _LineThickness;
            float4 _PanelTransitionDoors;

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
                float2 size = max(_PanelTransitionDoors.zw, float2(1.0, 1.0));
                float2 px = i.uv * size;
                px.x -= _PanelTransitionDoors.y;

                float doorWidth = size.x * 0.5 * _PanelTransitionDoors.x;
                float leftDepth = doorWidth - px.x;
                float rightDepth = px.x - (size.x - doorWidth);
                clip(max(leftDepth, rightDepth));

                bool isLeft = leftDepth > 0.0;
                float edgeDistance = isLeft ? leftDepth : rightDepth;

                float lineMask = step(frac(px.y / _PanelSpacing) * _PanelSpacing, _LineThickness);
                float4 color = lerp(_DoorColor, _DoorLineColor, lineMask);

                float diagonal = isLeft ? px.x + px.y : px.y - px.x;
                float4 stripeColor = lerp(_StripeColor, _StripeDarkColor, step(0.5, frac(diagonal / _StripePeriod)));
                color = edgeDistance < _StripeWidth ? stripeColor : color;
                color = edgeDistance < _LineThickness ? _StripeDarkColor : color;

                return float4(color.rgb, 1.0) * i.color;
            }
            ENDCG
        }
    }
}
