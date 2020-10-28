//Shader "Unlit/LightProbeInstShader"
//{
//    Properties
//    {
//        //_MainTex ("Texture", 2D) = "white" {}
//		//incident_light("incident_light", Color) = (0.0, 0.0, 0.0, 0.0)
//		//occlusion("occlusion", Vector) = (0.0, 0.0, 0.0, 0.0)
//    }
//    SubShader
//    {
//        Tags { "RenderType"="Opaque" }
//        LOD 100

//        Pass
//        {
//            CGPROGRAM
//            #pragma vertex vert
//            #pragma fragment frag
//            #pragma multi_compile_instancing
//            #include "UnityCG.cginc"

//            struct appdata
//            {
//                float4 vertex : POSITION;
//				float3 normal : NORMAL;
//				UNITY_VERTEX_INPUT_INSTANCE_ID
//            };

//            struct v2f
//            {
//                //float2 uv : TEXCOORD0;
//                //UNITY_FOG_COORDS(1)
//                float4 vertex : SV_POSITION;
//				float3 normal : TEXCOORD0;
//				UNITY_VERTEX_INPUT_INSTANCE_ID
//            };

//            //sampler2D _MainTex;
//            //float4 _MainTex_ST;
//			UNITY_INSTANCING_BUFFER_START(Props)			
//				UNITY_DEFINE_INSTANCED_PROP(float4, _sh2_r)
//				UNITY_DEFINE_INSTANCED_PROP(float4, _sh2_g)
//				UNITY_DEFINE_INSTANCED_PROP(float4, _sh2_b)
//				UNITY_DEFINE_INSTANCED_PROP(float4, _color)
//			UNITY_INSTANCING_BUFFER_END(Props)

//			/** The SH coefficients for the projection of a function that maps directions to scalar values. */
//			struct FThreeBandSHVector
//			{
//				half4 V0;
//			};

//			struct FThreeBandSHVectorRGB
//			{
//				FThreeBandSHVector R;
//				FThreeBandSHVector G;
//				FThreeBandSHVector B;
//			};

//			float4 SHBasisFunction2(half3 InputVector)
//			{
//				float4 Result;
//				// These are derived from simplifying SHBasisFunction in C++
//				Result.x = 0.282095f; 
//				Result.y = -0.488603f * InputVector.y;
//				Result.z = 0.488603f * InputVector.z;
//				Result.w = -0.488603f * InputVector.x;				

//				return Result;
//			}

//			half DotSH2(FThreeBandSHVector A,FThreeBandSHVector B)
//			{
//				half Result = dot(A.V0, B.V0);				
//				return Result;
//			}

//			half3 DotSH2(FThreeBandSHVectorRGB A, FThreeBandSHVector B)
//			{
//				half3 Result = 0;
//				Result.r = DotSH2(A.R,B);
//				Result.g = DotSH2(A.G,B);
//				Result.b = DotSH2(A.B,B);
//				return Result;
//			}

//			FThreeBandSHVector InitSH2(float v[4])
//			{
//				FThreeBandSHVector ret;
//				ret.V0 = half4(v[0], v[1], v[2], v[3]);				

//				return ret;
//			}

//			//float DotFloat4(float4 a, float b)
//			//{
//			//	return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
//			//}
			
//			void Unity_RotateAboutAxis_Degrees_float(float3 In, float3 Axis, float Rotation, out float3 Out)
//			{
//				Rotation = radians(Rotation);
//				float s = sin(Rotation);
//				float c = cos(Rotation);
//				float one_minus_c = 1.0 - c;

//				Axis = normalize(Axis);
//				float3x3 rot_mat = 
//				{   one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s, one_minus_c * Axis.z * Axis.x + Axis.y * s,
//					one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c, one_minus_c * Axis.y * Axis.z - Axis.x * s,
//					one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s, one_minus_c * Axis.z * Axis.z + c
//				};
//				Out = mul(rot_mat,  In);
//			}

//			float3 ToUE4(float3 In)
//			{
//				float3 ret;				
//				Unity_RotateAboutAxis_Degrees_float(In.xyz, float3(1, 0, 0), 90, ret.xyz);
//				return ret;
//			}			

//            v2f vert (appdata v)
//            {
//                v2f o;

//                UNITY_SETUP_INSTANCE_ID(v);
//                UNITY_TRANSFER_INSTANCE_ID(v, o);

//                o.vertex = UnityObjectToClipPos(v.vertex);                
//				o.normal = v.normal;

//                return o;
//            }

//            fixed4 frag (v2f i) : SV_Target
//            {
//                // sample the texture
//                //fixed4 col = tex2D(_MainTex, i.uv);
//                // apply fog
//                //UNITY_APPLY_FOG(i.fogCoord, col);
//                //return col;
//				//FThreeBandSHVectorRGB rgb_sh;
//				float4 shr = UNITY_ACCESS_INSTANCED_PROP(Props, _sh2_r);
//				float4 shg = UNITY_ACCESS_INSTANCED_PROP(Props, _sh2_g);
//				float4 shb = UNITY_ACCESS_INSTANCED_PROP(Props, _sh2_b);				

//				//float3 dir = float3(i.normal.z, -i.normal.y, i.normal.x); // ue to unity
//				float3 dir = ToUE4(i.normal); // ue to unity
//				float4 basis = SHBasisFunction2(normalize(dir));

//				float finalr = dot(basis, shr);
//				float finalg = dot(basis, shg);
//				float finalb = dot(basis, shb);

//				half3 gi = float3(finalr, finalg, finalb) / 3.1415f;

//				//return float4(gi * 1.0f, 1.0f);
//				half3 test_dir = normalize(shr.yzw);
//				test_dir = abs(test_dir);
//				//return float4(test_dir, 1.0f);
//				return float4(UNITY_ACCESS_INSTANCED_PROP(Props, _color));
//				//return float4(occlusion.xyz, 1.0f);
//            }
//            ENDCG
//        }
//    }
//}


Shader "Unlit/LightProbeInstShader"
{
    Properties
    {
        //_Color ("Color", Color) = (1, 1, 1, 1)
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
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
				float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float3 normal : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // necessary only if you want to access instanced properties in fragment Shader.
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _color)
				UNITY_DEFINE_INSTANCED_PROP(float4, _sh2_r)
				UNITY_DEFINE_INSTANCED_PROP(float4, _sh2_g)
				UNITY_DEFINE_INSTANCED_PROP(float4, _sh2_b)
            UNITY_INSTANCING_BUFFER_END(Props)

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
           
            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o); // necessary only if you want to access instanced properties in the fragment Shader.

                o.vertex = UnityObjectToClipPos(v.vertex);
				o.normal = v.normal;
                return o;
            }
           
            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); // necessary only if any instanced properties are going to be accessed in the fragment Shader.

				float3 dir = ToUE4(i.normal); // ue to unity
				float4 basis = SHBasisFunction2(normalize(dir));

				float4 shr = UNITY_ACCESS_INSTANCED_PROP(Props, _sh2_r);
				float4 shg = UNITY_ACCESS_INSTANCED_PROP(Props, _sh2_g);
				float4 shb = UNITY_ACCESS_INSTANCED_PROP(Props, _sh2_b);
				float finalr = dot(basis, shr);
				float finalg = dot(basis, shg);
				float finalb = dot(basis, shb);

				half3 gi = float3(finalr, finalg, finalb);

                //float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _color);

				return float4(gi, 1.0f);
            }
            ENDCG
        }
    }
}