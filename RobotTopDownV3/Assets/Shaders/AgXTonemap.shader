Shader "Hidden/RTD/AgXTonemap"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Exposure ("Exposure", Float) = 1
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float _Exposure;

			static const float3x3 AGX_INSET = float3x3(
				0.842479062253094, 0.0423282422610123, 0.0423756549057051,
				0.0784335999999992, 0.878468636469772, 0.0784336,
				0.0792237451477643, 0.0791661274605434, 0.879142973793104);

			static const float3x3 AGX_OUTSET = float3x3(
				1.19687900512017, -0.0528968517574562, -0.0529716355144438,
				-0.0980208811401368, 1.15190312990417, -0.0980434501171241,
				-0.0990297440797205, -0.0989611768448433, 1.15107367264116);

			static const float AGX_MIN_EV = -12.47393;
			static const float AGX_MAX_EV = 4.026069;

			float3 AgxContrast (float3 x)
			{
				float3 x2 = x * x;
				float3 x4 = x2 * x2;
				return 15.5 * x4 * x2 - 40.14 * x4 * x + 31.96 * x4 - 6.868 * x2 * x + 0.4298 * x2 + 0.1191 * x - 0.00232;
			}

			float4 frag (v2f_img i) : SV_Target
			{
				float4 source = tex2D(_MainTex, i.uv);
				float3 color = max(source.rgb * _Exposure, 0.0);

				color = mul(color, AGX_INSET);
				color = clamp(log2(max(color, 1e-10)), AGX_MIN_EV, AGX_MAX_EV);
				color = (color - AGX_MIN_EV) / (AGX_MAX_EV - AGX_MIN_EV);
				color = AgxContrast(color);
				color = saturate(mul(color, AGX_OUTSET));

				return float4(GammaToLinearSpaceExact(color.r), GammaToLinearSpaceExact(color.g), GammaToLinearSpaceExact(color.b), source.a);
			}
			ENDCG
		}
	}
	Fallback Off
}
