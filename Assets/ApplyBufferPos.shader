﻿Shader "Unlit/ApplyBuffer"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_l ("lerp", Range(0.0, 1.0)) = 1
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			//Cull Front

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			#pragma target 5.0

			#include "UnityCG.cginc"
			
			struct oriBuffer {
				float x, y, z, w;
			};

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
			};

			struct v2f
			{
				float2 uv : TEXCOORD2;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float4 dat : TEXCOORD3;
			};

			fixed _l;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			StructuredBuffer<oriBuffer> ssBuffer;
			
			//adjust vertex to value from buffer
			v2f vert (appdata v)
			{
				v2f o;
				int i = round(v.uv2.x * 100 + v.uv2.y * 10000);
				float4 c = float4(ssBuffer[i].x, ssBuffer[i].y, ssBuffer[i].z, 1);
				o.vertex = mul(UNITY_MATRIX_MVP, lerp(v.vertex, c, _l));
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o, o.vertex);
				o.dat = c;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
