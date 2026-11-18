Shader "Custom/EntitySilhouette"
{
    Properties
    {
        _Color ("Color", Color) = (0.92, 0.18, 0.12, 1)
        _Opacity ("Body Opacity", Range(0, 1)) = 1
        _Shading ("Shading", Range(0, 1)) = 0.35
        _RimColor ("Rim Color", Color) = (1, 0.55, 0.25, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.2
        _RimOpacity ("Rim Opacity", Range(0, 1)) = 0.8
        _ShimmerAmount ("Shimmer", Range(0, 1)) = 0.18
        _ShimmerScale ("Shimmer Scale", Range(0.1, 20)) = 3
        _ShimmerSpeed ("Shimmer Speed", Range(0, 5)) = 0.9
        _ScanlineDensity ("Scanline Density", Range(1, 80)) = 22
        _ScanlineSpeed ("Scanline Speed", Range(-5, 5)) = 0.6
        _ScanlineOpacity ("Scanline Opacity", Range(0, 1)) = 0
        _FlickerAmount ("Flicker", Range(0, 1)) = 0
        _FlickerSpeed ("Flicker Speed", Range(0, 40)) = 14
        _OccludedOpacity ("Occluded Opacity", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+100" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        CGINCLUDE
        #include "UnityCG.cginc"

        float4 _Color;
        float _Opacity;
        float _Shading;
        float4 _RimColor;
        float _RimPower;
        float _RimOpacity;
        float _ShimmerAmount;
        float _ShimmerScale;
        float _ShimmerSpeed;
        float _ScanlineDensity;
        float _ScanlineSpeed;
        float _ScanlineOpacity;
        float _FlickerAmount;
        float _FlickerSpeed;
        float _OccludedOpacity;

        struct appdata
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
        };

        struct v2f
        {
            float4 pos : SV_POSITION;
            float3 worldPos : TEXCOORD0;
            float3 worldNormal : TEXCOORD1;
        };

        v2f vert (appdata v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            o.worldNormal = UnityObjectToWorldNormal(v.normal);
            return o;
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

        float GetRim (v2f i)
        {
            float3 normal = normalize(i.worldNormal);
            float3 viewDirection = normalize(UnityWorldSpaceViewDir(i.worldPos));
            return pow(1.0 - saturate(abs(dot(normal, viewDirection))), _RimPower);
        }

        float GetFlicker ()
        {
            return 1.0 - _FlickerAmount * Hash(float2(floor(_Time.y * _FlickerSpeed), 7.0));
        }

        float4 fragDepth (v2f i) : SV_Target
        {
            return float4(0.0, 0.0, 0.0, 0.0);
        }

        float4 fragOccluded (v2f i) : SV_Target
        {
            float rim = GetRim(i);
            float alpha = _OccludedOpacity * (0.35 + 0.65 * rim) * GetFlicker();
            if (alpha < 0.002)
                discard;

            return float4(lerp(_Color.rgb, _RimColor.rgb, rim), alpha);
        }

        float4 fragVisible (v2f i) : SV_Target
        {
            float3 normal = normalize(i.worldNormal);
            float rim = GetRim(i);
            float light = saturate(dot(normal, normalize(float3(0.35, 1.0, -0.25))) * 0.5 + 0.5);

            float2 shimmerPosition = float2(i.worldPos.x + i.worldPos.z, i.worldPos.y) * _ShimmerScale - float2(0.0, _Time.y * _ShimmerSpeed);
            float shimmer = 0.65 * ValueNoise(shimmerPosition) + 0.35 * ValueNoise(shimmerPosition * 2.3 + float2(4.1, 1.7));

            float3 body = _Color.rgb * lerp(1.0, light, _Shading);
            body *= 1.0 + _ShimmerAmount * (shimmer - 0.5) * 2.0;

            float scan = frac(i.worldPos.y * _ScanlineDensity - _Time.y * _ScanlineSpeed);
            float scanline = smoothstep(0.0, 0.2, scan) * (1.0 - smoothstep(0.3, 0.5, scan)) * _ScanlineOpacity;

            float rimMask = saturate(rim * _RimOpacity);
            float3 color = lerp(body, _RimColor.rgb, rimMask) + _RimColor.rgb * scanline;
            float alpha = saturate(_Opacity + (rimMask + scanline) * (1.0 - _Opacity));
            float flicker = GetFlicker();
            return float4(color * flicker, alpha * flicker);
        }
        ENDCG

        Pass
        {
            Name "Occluded"
            ZWrite Off
            ZTest Greater
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragOccluded
            #pragma target 3.0
            ENDCG
        }

        Pass
        {
            Name "Depth"
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragDepth
            #pragma target 3.0
            ENDCG
        }

        Pass
        {
            Name "Visible"
            ZWrite Off
            ZTest LEqual
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragVisible
            #pragma target 3.0
            ENDCG
        }
    }
}
