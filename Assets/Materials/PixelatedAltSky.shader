Shader "Custom/PixelatedAltSky"
{
    Properties
    {
        _PixelSize ("Pixel Size", Float) = 2.0
        _CycleSpeed ("Day Night Cycle Speed", Float) = 0.02
        _StarDensity ("Star Density", Float) = 0.05
        _FallingStarSpeed ("Falling Star Speed", Float) = 0.3
        _MagmaSpeed ("Magma Noise Speed", Float) = 0.2
        _MagmaScale ("Magma Noise Scale", Float) = 5.0
        _TimeOffset ("Time Offset", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        LOD 100
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float2 worldPos : TEXCOORD1;
            };

            float _PixelSize;
            float _CycleSpeed;
            float _StarDensity;
            float _FallingStarSpeed;
            float _MagmaSpeed;
            float _MagmaScale;
            float _TimeOffset;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldPos = wpos.xy;
                return o;
            }

            // Simple hash function for noise
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float2 hash22(float2 p)
            {
                float n = hash21(p);
                return frac(float2(n, n * 34.5));
            }

            // Pseudo-random noise
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Snap UV to a pixel grid for pixelation
                float2 pixelUV = floor(i.uv * _ScreenParams.xy / _PixelSize) * _PixelSize / _ScreenParams.xy;

                float altitude = i.worldPos.y; // Y-axis altitude
                float t = (_Time.y + _TimeOffset) * _CycleSpeed; // cycles day/night
                float dayPhase = 0.5 + 0.5 * sin(t * 6.2831); // 0..1

                fixed4 col;

                if (altitude >= 10000.0)
                {
                    // Stratosphere: dark with stars
                    float base = saturate((altitude - 10000.0) / 1000.0);
                    col = lerp(fixed4(0.1, 0.2, 0.5, 1) * dayPhase, fixed4(0, 0, 0.05, 1), base);

                    // Stars
                    float2 starCoord = floor(i.worldPos.xy * _StarDensity);
                    if (hash21(starCoord) > 0.98)
                    {
                        float twinkle = 0.5 + 0.5 * sin((_Time.y + hash21(starCoord)) * 10.0);
                        col.rgb += twinkle;
                    }
                    // Falling stars occasionally
                    if (fmod((_Time.y * _FallingStarSpeed) + hash21(starCoord), 20.0) < 0.05)
                    {
                        col.rgb += fixed3(1.0, 1.0, 0.8);
                    }
                }
                else if (altitude >= 0.0)
                {
                    // Ground level to high sky before stratosphere
                    float factor = saturate(altitude / 10000.0);
                    fixed4 dayColor = lerp(fixed4(0.5, 0.7, 1, 1), fixed4(0.1, 0.2, 0.4, 1), factor);
                    fixed4 nightColor = lerp(fixed4(0.2, 0.2, 0.4, 1), fixed4(0, 0, 0.1, 1), factor);
                    col = lerp(dayColor, nightColor, 1.0 - dayPhase);

                    if (altitude < 100.0)
                        col = fixed4(0.6, 0.6, 0.6, 1); // grey skyline
                }
                else
                {
                    // Underground
                    if (altitude < -2000.0)
                    {
                        // Magma zone
                        float2 ncoord = i.worldPos.xy * _MagmaScale;
                        float heat = noise(ncoord + float2(0, _Time.y * _MagmaSpeed));
                        col = lerp(fixed4(1.0, 0.3, 0.0, 1), fixed4(1.0, 1.0, 0.0, 1), heat);
                    }
                    else if (altitude < -1950.0)
                    {
                        col = fixed4(1.0, 0.8, 0.0, 1); // yellow before magma
                    }
                    else if (altitude < -100.0)
                    {
                        col = fixed4(0.5, 0.25, 0.1, 1); // brown
                    }
                    else
                    {
                        col = fixed4(0.6, 0.6, 0.6, 1); // grey for shallow underground
                    }
                }

                return col;
            }
            ENDCG
        }
    }
}
