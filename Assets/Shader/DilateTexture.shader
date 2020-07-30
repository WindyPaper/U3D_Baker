Shader "Unlit/DilateTexture"
{
    Properties
    {
        _ColorTex ("ColorTex", 2D) = "white" {}
		_NormalTex ("NormalTex", 2D) = "black" {}
		_PosTex ("PosTex", 2D) = "white" {}
		_PixelOffset("PixelOffset", Vector) = (0.0, 0.0, 0.0, 0.0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
		Cull Off
		ZTest Off

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
                float4 vertex : SV_POSITION;
            };

            sampler2D _ColorTex;
            float4 _Color_ST;

			sampler2D _NormalTex;
            float4 _NormalTex_ST;

			sampler2D _PosTex;
            float4 _Pos_ST;

			float4 _PixelOffset;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = (v.vertex);
                o.uv = v.uv;
                return o;
            }

			float4 dilate_tex(sampler2D tex, float2 vUv0)
			{
				float4 c = tex2D(tex, vUv0);
                				
				c = c.a < 1.0 ? tex2D(tex, vUv0 - _PixelOffset.xy) : c;
				c = c.a < 1.0 ? tex2D(tex, vUv0 + float2(0, -_PixelOffset.y)) : c;
				c = c.a < 1.0 ? tex2D(tex, vUv0 + float2(_PixelOffset.x, -_PixelOffset.y)) : c;
				c = c.a < 1.0 ? tex2D(tex, vUv0 + float2(-_PixelOffset.x, 0)) : c;
				c = c.a < 1.0 ? tex2D(tex, vUv0 + float2(_PixelOffset.x, 0)) : c;
				c = c.a < 1.0 ? tex2D(tex, vUv0 + float2(-_PixelOffset.x, _PixelOffset.y)) : c;
				c = c.a < 1.0 ? tex2D(tex, vUv0 + float2(0, _PixelOffset.y)) : c;
				c = c.a < 1.0 ? tex2D(tex, vUv0 + _PixelOffset.xy) : c;

				return c;
			}

            void frag (v2f i,
				out float4 GRT0 : SV_Target0,
				out float4 GRT1 : SV_Target1,
				out float4 GRT2 : SV_Target2)
            {
                // sample the texture
				GRT0 = dilate_tex(_ColorTex, i.uv);
				GRT1 = dilate_tex(_NormalTex, i.uv);
                GRT2 = dilate_tex(_PosTex, i.uv);

            }
            ENDCG
        }
    }
}
