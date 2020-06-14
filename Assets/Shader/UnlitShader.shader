Shader "Unlit/UnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_FloatVal ("Depth", Range(-1, 1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

		ZTest Off
		ZWrite Off
		Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

			float _FloatVal;

            v2f vert (appdata v)
            {
                v2f o;
                //o.vertex = UnityObjectToClipPos(v.vertex);
				o.vertex = float4((v.uv * 2 - 1) * float2(1, -1), 1.0f, 1.0f);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                //return col;
				return float4(i.uv * 2 - 1, 0.0f, 1.0f);
            }
            ENDCG
        }
    }
}
