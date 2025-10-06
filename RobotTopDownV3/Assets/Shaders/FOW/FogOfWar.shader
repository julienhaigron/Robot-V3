Shader "Custom/FogOfWar_Mask"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1) // blanc = fog, noir = rien
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry-10" }
        ZWrite On
        ColorMask RGB

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color; // toujours blanc
            }
            ENDCG
        }
    }
}
