﻿Shader "Unlit/WriteToBuffer"
{
		SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 100

		Pass
	{
		//Blend Zero One
		ZTest Always
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 5.0

#include "UnityCG.cginc"

	struct oriBuffer {
		float x, y, z, w;
	};

	struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	};

	struct v2f
	{
		float4 vertex : SV_POSITION;
		int i : TEXCOORD1;
		float4 dat : TEXCOORD2;
	};

	uniform RWStructuredBuffer<oriBuffer> ssBuffer : register(u1);

	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
		//o.vertex.x = 0;
		//o.vertex.y = 0;
		o.i = round(v.uv.x * 100 + v.uv.y*10000);
		o.dat = mul(unity_ObjectToWorld, v.vertex);
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		oriBuffer b;
		b.x = i.dat.x;
		b.y = i.dat.y;
		b.z = i.dat.z;
		b.w = i.i;
		ssBuffer[i.i] = b;
		return fixed4(1, 0, 0, 1);
	}
		ENDCG
	}
	}
}
