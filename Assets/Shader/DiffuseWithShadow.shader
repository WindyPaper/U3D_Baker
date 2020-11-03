// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Lit/DiffuseWithShadow"
{
	Properties
	{
		[NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
	}

	SubShader
	{
		Pass
		{			
			Tags {"LightMode" = "ForwardBase"}
			CGPROGRAM
			#pragma multi_compile_fwdbase	
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			// compile shader into multiple variants, with and without shadows
			// (we don't care about any lightmaps yet, so skip these variants)
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap
			// shadow helper functions and macros
			#include "AutoLight.cginc"

			struct v2f
			{
				float2 uv : TEXCOORD0;
				SHADOW_COORDS(1) // put shadows data into TEXCOORD1
				fixed3 diff : COLOR0;
				fixed3 ambient : COLOR1;
				float4 pos : SV_POSITION;				
				float3 normalDir : TEXCOORD2;
				float3 lightDir : TEXCOORD3;
				float3 viewDir : TEXCOORD4;
				float4 posWorld : TEXCOORD5;
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord;
				

				o.posWorld = mul(unity_ObjectToWorld, v.vertex);
				o.normalDir = UnityObjectToWorldNormal(v.normal);
				o.lightDir = WorldSpaceLightDir(v.vertex);
				o.viewDir = WorldSpaceViewDir(v.vertex);
				o.diff = float3(0.0f, 0.0f, 0.0f);


//#ifdef VERTEXLIGHT_ON
				
//#endif // VERTEXLIGHT_ON

				// pass lighting information to pixel shader
				TRANSFER_VERTEX_TO_FRAGMENT(o);


				// compute shadows data
				TRANSFER_SHADOW(o)
				return o;
			}

			sampler2D _MainTex;

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				// compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
				fixed shadow = SHADOW_ATTENUATION(i);

				float3 normalDirection = normalize(i.normalDir);
				//float3 viewDirection = normalize(_WorldSpaceCameraPos - i.posWorld.xyz);
				float3 lightDirection;
				float attenuation;

				if (0.0 == _WorldSpaceLightPos0.w) // directional light?
				{
					attenuation = 1.0; // no attenuation
					lightDirection = normalize(_WorldSpaceLightPos0.xyz);
				}
				else // point or spot light
				{
					float3 vertexToLightSource = _WorldSpaceLightPos0.xyz - i.posWorld.xyz;
					float distance = length(vertexToLightSource);
					attenuation = 1.0 / distance; // linear attenuation 
					lightDirection = normalize(vertexToLightSource);
				}

				float3 vertexLight = Shade4PointLights(
					unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
					unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
					unity_4LightAtten0, i.posWorld, i.normalDir);
				i.diff += vertexLight;

				// LIGHT_ATTENUATION not only compute attenuation, but also shadow infos
//                attenuation = LIGHT_ATTENUATION(input);
				// Compare to directions computed from vertex
//				viewDirection = normalize(input.viewDir);
//				lightDirection = normalize(input.lightDir);

				// Because SH lights contain ambient, we don't need to add it to the final result
				//float3 ambientLighting = UNITY_LIGHTMODEL_AMBIENT.xyz;

				float3 diffuseReflection = attenuation * _LightColor0.rgb * col.rgb * saturate(dot(normalDirection, lightDirection));


				// darken light's illumination with shadow, keep ambient intact
				fixed3 lighting = ((i.diff) * col.rgb + shadow * diffuseReflection) / 3.141592f;
				//col.rgb *= lighting;
				return float4(lighting, 1.0f);
			}
			ENDCG
		}

		// shadow casting support
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}