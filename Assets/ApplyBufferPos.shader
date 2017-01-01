Shader "Unlit/ApplyBuffer"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
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
				float2 uv4 : TEXCOORD3;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float4 dat : TEXCOORD2;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			StructuredBuffer<oriBuffer> ssBuffer;
			
			//adjust vertex to value from buffer
			v2f vert (appdata v)
			{
				v2f o;
				int i = round(v.uv4.x * 100 + v.uv4.y * 10000);
				float4 c = float4(ssBuffer[i].x, ssBuffer[i].y, ssBuffer[i].z, 1);
				o.vertex = mul(UNITY_MATRIX_MVP, c);
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
