// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "GI/StandardGIShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
		_GITexR("GITexR", 3D) = "white" {}
		_GITexG("GITexG", 3D) = "white" {}
		_GITexB("GITexB", 3D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
				float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
				float3 sh_uv : TEXCOORD1;
				float3 normal : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

			sampler3D _GITexR;
            float4 _GITexR_ST;

			sampler3D _GITexG;
            float4 _GITexG_ST;

			sampler3D _GITexB;
            float4 _GITexB_ST;

			float4x4 _InvGIVolumeWMatrix;
			float _lenx;
			float _leny;
			float _lenz;

			void Unity_RotateAboutAxis_Degrees_float(float3 In, float3 Axis, float Rotation, out float3 Out)
			{
				Rotation = radians(Rotation);
				float s = sin(Rotation);
				float c = cos(Rotation);
				float one_minus_c = 1.0 - c;

				Axis = normalize(Axis);
				float3x3 rot_mat =
				{ one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s, one_minus_c * Axis.z * Axis.x + Axis.y * s,
					one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c, one_minus_c * Axis.y * Axis.z - Axis.x * s,
					one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s, one_minus_c * Axis.z * Axis.z + c
				};
				Out = mul(rot_mat, In);
			}

			float3 ToUE4(float3 In)
			{
				float3 ret;
				Unity_RotateAboutAxis_Degrees_float(In.xyz, float3(1, 0, 0), 90, ret.xyz);
				return ret;
			}

			float4 SHBasisFunction2(half3 InputVector)
			{
				float4 Result;
				// These are derived from simplifying SHBasisFunction in C++
				Result.x = 0.282095f;
				Result.y = -0.488603f * InputVector.y;
				Result.z = 0.488603f * InputVector.z;
				Result.w = -0.488603f * InputVector.x;

				return Result;
			}

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                //UNITY_TRANSFER_FOG(o,o.vertex);

				o.normal = UnityObjectToWorldNormal(v.normal);
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.sh_uv = mul(_InvGIVolumeWMatrix, worldPos).xyz;
				o.sh_uv = float3((o.sh_uv.x + 0.25f) / _lenx, (o.sh_uv.y + 0.25f) / _leny, (o.sh_uv.z + 0.25f) / _lenz) * 0.5f + float3(0.5f, 0.5f, 0.5f);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				//UNITY_APPLY_FOG(i.fogCoord, col);
				//return col;

				float3 dir = ToUE4(i.normal); // ue to unity
				float4 basis = SHBasisFunction2(normalize(dir));

				float4 shr = tex3D(_GITexR, i.sh_uv);
				float4 shg = tex3D(_GITexG, i.sh_uv);
				float4 shb = tex3D(_GITexB, i.sh_uv);
				float finalr = dot(basis, shr);
				float finalg = dot(basis, shg);
				float finalb = dot(basis, shb);

				half3 gi = float3(finalr, finalg, finalb) / 3.14159f;		

				return float4(gi, 1.0f);
				//return shb;  
				//return float4(i.sh_uv, 1.0f);
            }
            ENDCG
        }
    }
}
