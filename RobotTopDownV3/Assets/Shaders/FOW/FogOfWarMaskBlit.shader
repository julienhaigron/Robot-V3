Shader "Hidden/FogOfWar_MaskBlit"
{
    Properties
    {
        _MainTex ("Mask", 2D) = "black" {}
    }

    SubShader
    {
        ZTest Always
        ZWrite Off
        Cull Off

        CGINCLUDE
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        sampler2D _BlendTarget;
        float4 _BlurStep;
        float _BlendAmount;

        static const float kWeights[7] = { 0.1997, 0.1762, 0.1211, 0.0648, 0.0270, 0.0088, 0.0022 };

        float4 Blur (v2f_img i) : SV_Target
        {
            float4 sum = tex2D(_MainTex, i.uv) * kWeights[0];
            [unroll] for (int k = 1; k < 7; k++)
            {
                float2 offset = _BlurStep.xy * k;
                sum += (tex2D(_MainTex, i.uv + offset) + tex2D(_MainTex, i.uv - offset)) * kWeights[k];
            }
            return sum;
        }

        float4 Mix (v2f_img i) : SV_Target
        {
            return lerp(tex2D(_MainTex, i.uv), tex2D(_BlendTarget, i.uv), _BlendAmount);
        }
        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Blur
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Mix
            ENDCG
        }
    }
}
