Shader "Unlit/GetPos"
{
	Properties
	{
		
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 pos : TEXCOORD1;
			};
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = float4(v.uv.x * 2 - 1,  v.uv.y * 2 - 1, 1, 1);
				o.pos = mul(unity_ObjectToWorld, v.vertex);//mul(UNITY_MATRIX_MVP, v.vertex);
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float x = i.pos.x;
				float y = i.pos.y;
				float z = i.pos.z;
				int dx = ceil(clamp(x, 0, 1));
				int dy = ceil(clamp(y, 0, 1));
				int dz = ceil(clamp(z, 0, 1));
				//return x * (dx * 2 - 1);
				return float4(x * (dx * 2 - 1), y * (dy * 2 - 1), z * (dz * 2 - 1), dx * 4 + dy * 2 + dz);
			}
			ENDCG
		}
	}
}
