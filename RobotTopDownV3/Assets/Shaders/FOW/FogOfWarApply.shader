Shader "Custom/FogOfWar_Apply"
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
                    fixed mask = tex2D(_FogMask, i.uv).r;
                    fixed4 fog = _FogColor;
                    fog.a *= mask;   // masque contrôle la densité
                    return fog;
                }
                ENDCG
            }
        }
}
