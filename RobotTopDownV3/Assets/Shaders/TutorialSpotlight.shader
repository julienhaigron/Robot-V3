Shader "UI/TutorialSpotlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _DimColor ("Dim Color", Color) = (0.02, 0.024, 0.055, 0.7)
        _Softness ("Edge Softness (px)", Float) = 14
        _Radius ("Corner Radius (px)", Float) = 8
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
            float4 _DimColor;
            float _Softness;
            float _Radius;
            float4 _TutorialSpotlight;
            float4 _TutorialSpotlightHoles[8];

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

            float RoundedBox (float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 px = i.uv * max(_TutorialSpotlight.zw, float2(1.0, 1.0));
                int count = (int)_TutorialSpotlight.y;
                float distance = 100000.0;

                for (int h = 0; h < 8; h++)
                {
                    if (h >= count)
                        break;

                    float4 hole = _TutorialSpotlightHoles[h];
                    float2 halfSize = (hole.zw - hole.xy) * 0.5;
                    distance = min(distance, RoundedBox(px - (hole.xy + halfSize), halfSize, min(_Radius, min(halfSize.x, halfSize.y))));
                }

                float alpha = _DimColor.a * _TutorialSpotlight.x * smoothstep(0.0, _Softness, distance);
                return float4(_DimColor.rgb * i.color.rgb, alpha * i.color.a);
            }
            ENDCG
        }
    }
}
