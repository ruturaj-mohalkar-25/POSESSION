Shader "Possession/Visibility/ObjectiveSilhouette"
{
    Properties
    {
        _Color ("Objective color", Color) = (0.35,1,0.7,0.8)
        _Target ("Target bullseye", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZTest Greater
        ZWrite Off
        Cull Back
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _Target;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata input)
            {
                v2f output; output.position = UnityObjectToClipPos(input.vertex); output.uv = input.uv; return output;
            }
            fixed4 frag(v2f input) : SV_Target
            {
                float radius = length(input.uv - .5);
                float ring = 1 - smoothstep(.025, .045, abs(radius - .28));
                float center = 1 - smoothstep(.06, .09, radius);
                return fixed4(lerp(_Color.rgb, fixed3(1,1,1), saturate(ring + center) * _Target), _Color.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
