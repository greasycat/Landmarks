Shader "Custom/HaloShader"
{
    Properties {
        _Color ("Main Color", Color) = (1, 1, 1, 1)
        _HaloColor ("Halo Color", Color) = (1, 0, 0, 1)
        _HaloRadius ("Halo Radius", Range(0, 1)) = 0.1
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert

        struct Input {
            float2 uv_MainTex;
        };

        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _HaloColor;
        float _HaloRadius;

        void surf (Input IN, inout SurfaceOutput o) {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;

            // Calculate distance from the center of the object
            float dist = distance(IN.uv_MainTex, 0.5);

            // Apply halo effect if within the radius
            if (dist <= _HaloRadius) {
                fixed4 halo = _HaloColor * (1 - dist / _HaloRadius);
                o.Emission = halo.rgb;
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}
