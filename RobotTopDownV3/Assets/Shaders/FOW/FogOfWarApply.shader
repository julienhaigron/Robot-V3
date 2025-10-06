Shader "Custom/FogOfWar_Apply_Final"
{
    Properties
    {
        _FogMask("Fog Mask", 2D) = "white" {}
        _FogColor("Fog Color", Color) = (0,0,0,0.6)
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
                fixed4 _FogColor;

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float4 pos : SV_POSITION;
                    float2 uv  : TEXCOORD0;
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

                // Inversion : maintenant 0 = visible, 1 = brouillard
                mask = 1.0 - mask;

                fixed4 fog = _FogColor;
                fog.a *= mask;   // alpha contrôlé par le mask inversé

                return fog;
            }
            ENDCG
        }
        }
}
