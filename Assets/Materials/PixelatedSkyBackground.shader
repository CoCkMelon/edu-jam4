Shader "Custom/PixelatedSkyBackground"
{
    Properties
    {
        [Header(Time Settings)]
        _DayNightSpeed ("Day Night Cycle Speed", Range(0.01, 2.0)) = 0.1
        _TimeOfDay ("Time of Day Override", Range(-1, 1)) = -1

        [Header(Pixelation)]
        _PixelSize ("Pixel Size", float) = 0.04

        [Header(Colors)]
        _SkylineColor ("Skyline Color", Color) = (0.2, 0.3, 0.5, 1)
        _DaySkyBottom ("Day Sky Bottom", Color) = (0.5, 0.7, 0.9, 1)
        _DaySkyTop ("Day Sky Top", Color) = (0.3, 0.5, 0.8, 1)
        _NightSkyBottom ("Night Sky Bottom", Color) = (0.05, 0.05, 0.2, 1)
        _NightSkyTop ("Night Sky Top", Color) = (0.0, 0.0, 0.1, 1)
        _StratosphereColor ("Stratosphere Color", Color) = (0.01, 0.01, 0.05, 1)

        [Header(Underground Colors)]
        _BrownColor ("Brown Layer Color", Color) = (0.4, 0.25, 0.15, 1)
        _YellowColor ("Yellow Layer Color", Color) = (0.8, 0.6, 0.2, 1)
        _GreyColor ("Grey Layer Color", Color) = (0.3, 0.3, 0.3, 1)
        _MagmaColor1 ("Magma Color 1", Color) = (1, 0.3, 0, 1)
        _MagmaColor2 ("Magma Color 2", Color) = (1, 0.8, 0, 1)

        [Header(Stars)]
        _StarDensity ("Star Density", Range(0, 0.5)) = 0.15
        _StarSize ("Star Size", Range(0.01, 1)) = 0.1
        _StarBrightness ("Star Brightness", Range(0, 1)) = 0.8
        _StarTwinkleSpeed ("Star Twinkle Speed", Range(0, 10)) = 2
        _FallingStarSpeed ("Falling Star Speed", Range(1, 20)) = 10

        [Header(Effects)]
        _SkylineNoiseScale ("Skyline Noise Scale", Range(0.1, 10)) = 2
        _SkylineHeight ("Skyline Height", Range(10, 100)) = 50
        _MagmaSpeed ("Magma Animation Speed", Range(0.1, 5)) = 1
        _MagmaNoiseScale ("Magma Noise Scale", Range(10, 10000)) = 3
    }

    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "PreviewType"="Plane" }
        LOD 100

        Pass
        {
            ZWrite Off
            Cull Off

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
                float3 worldPos : TEXCOORD1;
            };

            // Properties
            float _DayNightSpeed;
            float _TimeOfDay;
            float _PixelSize;

            float4 _SkylineColor;
            float4 _DaySkyBottom;
            float4 _DaySkyTop;
            float4 _NightSkyBottom;
            float4 _NightSkyTop;
            float4 _StratosphereColor;
            float4 _BrownColor;
            float4 _YellowColor;
            float4 _GreyColor;
            float4 _MagmaColor1;
            float4 _MagmaColor2;

            float _StarDensity;
            float _StarSize;
            float _StarBrightness;
            float _StarTwinkleSpeed;
            float _FallingStarSpeed;

            float _SkylineNoiseScale;
            float _SkylineHeight;
            float _MagmaSpeed;
            float _MagmaNoiseScale;

            // Fast hash without loops
            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Simple noise without loops
            float simpleNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Unrolled FBM - only 2 octaves for performance
            float fastFBM(float2 p)
            {
                float value = 0.0;

                // Octave 1
                value += 0.5 * simpleNoise(p);

                // Octave 2
                value += 0.25 * simpleNoise(p * 2.0);

                return value;
            }

            // Ultra-simple star field
            float simpleStar(float2 p, float time)
            {
                p *= 50.0;
                float2 cell = floor(p);
                float2 f = frac(p) - 0.5;

                float rnd = hash(cell);
                float hasStar = step(rnd, _StarDensity);

                // Simple twinkle based on cell position
                float twinkle = sin(time * _StarTwinkleSpeed + rnd * 6.283) * 0.3 + 0.7;

                // Distance to star center
                float d = length(f) / (_StarSize * (0.5 + rnd * 0.5));

                return hasStar * saturate(1.0 - d) * twinkle * _StarBrightness;
            }

            // Single falling star check (no loop)
            float singleFallingStar(float2 p, float time)
            {
                // Grid-based falling star
                float2 gridSize = float2(3.0, 2.0);
                float2 cell = floor(p * gridSize);
                float cellHash = hash(cell);

                // Periodic appearance
                float period = 8.0 + cellHash * 8.0;
                float phase = cellHash * period;
                float localTime = fmod(time * _FallingStarSpeed * 0.1 + phase, period);

                // Only show during first part of period
                float shouldShow = step(localTime, 1.0) * step(cellHash, 0.1);

                // Simple trajectory
                float2 startPos = (cell + 0.5) / gridSize;
                float2 offset = float2(localTime, -localTime * 0.5);
                float2 currentPos = startPos + offset;

                // Distance to star
                float d = length(p - currentPos) * 20.0;

                return shouldShow * saturate(1.0 - d) * (1.0 - localTime);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Pixelate coordinates
                float2 pixelatedUV = floor(i.uv * _ScreenParams.xy / _PixelSize) * _PixelSize / _ScreenParams.xy;

                float altitude = i.worldPos.y;
                float timeOfDay = _TimeOfDay >= 0 ? _TimeOfDay : (sin(_Time.y * _DayNightSpeed) * 0.5 + 0.5);

                float4 finalColor = float4(0, 0, 0, 1);

                // Single branch for underground vs sky
                float isUnderground = step(altitude, 0);

                // UNDERGROUND - simplified layers
                float4 undergroundColor;
                {
                    // Depth-based color mixing
                    float depth = -altitude;

                    // Magma (below 2000)
                    float magmaFactor = saturate((depth - 1950) / 50.0);
                    float magmaNoise = simpleNoise(pixelatedUV * _MagmaNoiseScale + float2(_Time.y * _MagmaSpeed, 0));
                    magmaNoise += simpleNoise(pixelatedUV * _MagmaNoiseScale * 2.0 - float2(_Time.y * _MagmaSpeed * 0.5, 0));
                    float4 magmaColor = lerp(_MagmaColor1, _MagmaColor2, saturate(magmaNoise * 0.5));

                    // Yellow transition (1950-2000)
                    float yellowFactor = saturate((depth - 100) / 1850.0) * (1.0 - magmaFactor);

                    // Grey (0-100)
                    float greyFactor = saturate(depth / 100.0) * (1.0 - yellowFactor) * (1.0 - magmaFactor);

                    // Brown (near surface)
                    float brownFactor = (1.0 - greyFactor - yellowFactor - magmaFactor);

                    undergroundColor = magmaColor * magmaFactor +
                                     _YellowColor * yellowFactor +
                                     _GreyColor * greyFactor +
                                     _BrownColor * brownFactor;
                }

                // SKY - simplified gradients
                float4 skyColor;
                {
                    // Height-based factors
                    float skylineFactor = saturate(1.0 - altitude / _SkylineHeight);
                    float stratosphereFactor = saturate((altitude - 10000) / 5000);
                    float normalSkyFactor = (1.0 - skylineFactor) * (1.0 - stratosphereFactor);

                    // Simple skyline noise
                    float skylineNoise = simpleNoise(float2(pixelatedUV.x * _SkylineNoiseScale, 0));
                    skylineFactor *= (1.0 + skylineNoise * 0.2);
                    skylineFactor = saturate(skylineFactor);

                    // Mix sky colors
                    float skyGradient = saturate(altitude / 10000);
                    float4 dayColor = lerp(
                        lerp(_DaySkyBottom, _SkylineColor, skylineFactor),
                        lerp(_DaySkyTop, _StratosphereColor, stratosphereFactor),
                        skyGradient
                    );

                    float4 nightColor = lerp(
                        lerp(_NightSkyBottom, _SkylineColor * 0.5, skylineFactor),
                        _StratosphereColor,
                        skyGradient
                    );

                    // Adjust time of day for stratosphere
                    float adjustedTimeOfDay = timeOfDay * (1.0 - stratosphereFactor * 0.8);

                    skyColor = lerp(nightColor, dayColor, adjustedTimeOfDay);

                    // Add stars only at night or in stratosphere
                    float starVisibility = saturate((1.0 - adjustedTimeOfDay) + stratosphereFactor);

                    // Simple star field
                    float stars = simpleStar(pixelatedUV, _Time.y);
                    float falling = singleFallingStar(pixelatedUV, _Time.y);

                    skyColor.rgb += (stars + falling * 2.0) * starVisibility;
                }

                // Final blend
                finalColor = lerp(skyColor, undergroundColor, isUnderground);

                return finalColor;
            }
            ENDCG
        }
    }
}
