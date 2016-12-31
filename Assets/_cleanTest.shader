Shader "Unlit/_cleanTest"
{
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0
			
			#include "UnityCG.cginc"
			
			struct buffer
			{
				int x, y, z, w;
			};

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				//float i : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			uniform RWStructuredBuffer<buffer> buff : register(u1);
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				//o.i = 0;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				buffer b;
				b.x = 1;
				b.y = 0;
				b.z = 0;
				b.w = 1;
				buff[0] = b;
				return fixed4(1, 0, 0, 1);
			}
			ENDCG
		}
	}
}
