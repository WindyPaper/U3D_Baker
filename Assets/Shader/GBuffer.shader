// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/GBuffer"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100
		Cull Back
		ZTest Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
				float2 lightmap_uv : TEXCOORD1;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 normal : TEXCOORD1;
				float4 world_pos : TEXCOORD2;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

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

			float4 ToUE4(float4 In)
			{
				float4 ret;				
				Unity_RotateAboutAxis_Degrees_float(In.xyz, float3(1, 0, 0), 90, ret.xyz);
				ret.w = In.w;
				return ret;
			}

			v2f vert(appdata v)
			{
				v2f o;				
				//o.vertex = UnityObjectToClipPos(v.vertex);
				//float4 local_v = ToUE4(v.vertex * 100.0);
				//local_v.w = 1.0f;
				o.world_pos = mul(unity_ObjectToWorld, v.vertex);
				o.world_pos = ToUE4(o.world_pos * 100);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.vertex = float4((v.lightmap_uv * 2 - 1) * float2(1, -1), 1.0f, 1.0f);

				float3 worldNormal = UnityObjectToWorldNormal(v.normal);
				o.normal = ToUE4(float4(worldNormal,0));
				return o;
			}

			void frag(v2f i,
				out float4 GRT0 : SV_Target0,
				out float4 GRT1 : SV_Target1,
				out float4 GRT2 : SV_Target2
			)
			{
				// sample the texture
				float4 col = tex2D(_MainTex, i.uv);
				GRT0 = col;
				GRT1 = i.normal;
				GRT2 = i.world_pos;
				//GRTDepth = 0.5;
			}
			ENDCG
		}
	}
}
