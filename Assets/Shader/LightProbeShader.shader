Shader "Unlit/LightProbeShader"
{
    Properties
    {
        //_MainTex ("Texture", 2D) = "white" {}
		incident_light("incident_light", Color) = (0.0, 0.0, 0.0, 0.0)
		occlusion("occlusion", Vector) = (0.0, 0.0, 0.0, 0.0)
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
                //float2 uv : TEXCOORD0;
                //UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
				float3 normal : TEXCOORD0;
            };

            //sampler2D _MainTex;
            //float4 _MainTex_ST;
			float4 incident_light;
			float4 occlusion;
			float _sh3_r[9];
			float _sh3_g[9];
			float _sh3_b[9];

			/** The SH coefficients for the projection of a function that maps directions to scalar values. */
			struct FThreeBandSHVector
			{
				half4 V0;
				half4 V1;
				half V2;
			};

			struct FThreeBandSHVectorRGB
			{
				FThreeBandSHVector R;
				FThreeBandSHVector G;
				FThreeBandSHVector B;
			};

			FThreeBandSHVector SHBasisFunction3(half3 InputVector)
			{
				FThreeBandSHVector Result;
				// These are derived from simplifying SHBasisFunction in C++
				Result.V0.x = 0.282095f; 
				Result.V0.y = -0.488603f * InputVector.y;
				Result.V0.z = 0.488603f * InputVector.z;
				Result.V0.w = -0.488603f * InputVector.x;

				half3 VectorSquared = InputVector * InputVector;
				Result.V1.x = 1.092548f * InputVector.x * InputVector.y;
				Result.V1.y = -1.092548f * InputVector.y * InputVector.z;
				Result.V1.z = 0.315392f * (3.0f * VectorSquared.z - 1.0f);
				Result.V1.w = -1.092548f * InputVector.x * InputVector.z;
				Result.V2 = 0.546274f * (VectorSquared.x - VectorSquared.y);

				return Result;
			}

			half DotSH3(FThreeBandSHVector A,FThreeBandSHVector B)
			{
				half Result = dot(A.V0, B.V0);
				Result += dot(A.V1, B.V1);
				Result += A.V2 * B.V2;
				return Result;
			}

			half3 DotSH3(FThreeBandSHVectorRGB A, FThreeBandSHVector B)
			{
				half3 Result = 0;
				Result.r = DotSH3(A.R,B);
				Result.g = DotSH3(A.G,B);
				Result.b = DotSH3(A.B,B);
				return Result;
			}

			FThreeBandSHVector InitSH3(float v[9])
			{
				FThreeBandSHVector ret;
				ret.V0 = half4(v[0], v[1], v[2], v[3]);
				ret.V1 = half4(v[4], v[5], v[6], v[7]);
				ret.V2 = v[8];

				return ret;
			}
			
			void Unity_RotateAboutAxis_Degrees_float(float3 In, float3 Axis, float Rotation, out float3 Out)
			{
				Rotation = radians(Rotation);
				float s = sin(Rotation);
				float c = cos(Rotation);
				float one_minus_c = 1.0 - c;

				Axis = normalize(Axis);
				float3x3 rot_mat = 
				{   one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s, one_minus_c * Axis.z * Axis.x + Axis.y * s,
					one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c, one_minus_c * Axis.y * Axis.z - Axis.x * s,
					one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s, one_minus_c * Axis.z * Axis.z + c
				};
				Out = mul(rot_mat,  In);
			}

			float3 ToUE4(float3 In)
			{
				float3 ret;				
				Unity_RotateAboutAxis_Degrees_float(In.xyz, float3(1, 0, 0), 90, ret.xyz);
				return ret;
			}			

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);                
                UNITY_TRANSFER_FOG(o,o.vertex);

				o.normal = v.normal;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                //fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);
                //return col;
				FThreeBandSHVectorRGB rgb_sh;
				rgb_sh.R = InitSH3(_sh3_r);
				rgb_sh.G = InitSH3(_sh3_g);
				rgb_sh.B = InitSH3(_sh3_b);

				//float3 dir = float3(i.normal.z, -i.normal.y, i.normal.x); // ue to unity
				float3 dir = ToUE4(i.normal); // ue to unity
				FThreeBandSHVector basis = SHBasisFunction3(normalize(dir));
				half3 gi = DotSH3(rgb_sh, basis) / 3.1415f;

				return float4(gi, 1.0f);
				//return float4(occlusion.xyz, 1.0f);
            }
            ENDCG
        }
    }
}
