Shader "Custom/FogOfWar_Apply_Stylized"
{
    Properties
    {
        _FogMask("Fog Mask", 2D) = "white" {}
        _FogColor("Fog Color", Color) = (0,0,0,0.6)
        _EdgeSoftness("Edge Softness", Range(0,1)) = 0.2
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _NoiseScale("Noise Scale", Float) = 5.0
        _MainTex("Main Texture", 2D) = "white" {}
    }
        SubShader
        {
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                sampler2D _FogMask;
                sampler2D _MainTex;
                sampler2D _NoiseTex;
                fixed4 _FogColor;
                float _EdgeSoftness;
                float _NoiseScale;

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float4 pos : SV_POSITION;
                    float2 uv : TEXCOORD0;
                };

                v2f vert(appdata v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    // Lis la valeur du mask
                    fixed mask = tex2D(_FogMask, i.uv).r;

                // Inversion : 0 = visible, 1 = fog
                mask = 1.0 - mask;

                // Lis le bruit pour donner du volume
                fixed noise = tex2D(_NoiseTex, i.uv * _NoiseScale).r;

                // Bords doux
                mask = smoothstep(0.0, _EdgeSoftness, mask - noise * 0.1);

                // Applique la couleur du fog
                fixed4 fog = _FogColor;
                fog.a *= mask;

                return fog;
            }
            ENDCG
        }
        }
}
