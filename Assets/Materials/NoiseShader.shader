// NoiseShader.shader
Shader "Custom/NoiseShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Seed ("Seed", Float) = 0
        _Scale ("Scale", Float) = 1
    }
    SubShader {
        Tags { "Queue"="Background" }
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _Seed;
            float _Scale;

            v2f vert (appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Simple hash function
            float hash(float2 p) {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // 2D Value Noise
            float noise(float2 p) {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f*f*(3.0-2.0*f);
                
                return lerp(
                    lerp(hash(i + float2(0.0,0.0)), 
                         hash(i + float2(1.0,0.0)), u.x),
                    lerp(hash(i + float2(0.0,1.0)), 
                         hash(i + float2(1.0,1.0)), u.x), u.y);
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = i.uv * _Scale + _Seed;
                float n = noise(uv);
                return fixed4(n, n, n, 1);
            }
            ENDCG
        }
    }
}