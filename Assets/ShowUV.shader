Shader "Unlit/ShowUV"
{
	Properties
	{

	}
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 100

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
		float2 uv : TEXCOORD1;
	};

	v2f vert(appdata v)
	{
		v2f o;
		o.uv = v.uv;
		o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		return fixed4(i.uv.x, i.uv.y, 0, 1);
	}
		ENDCG
	}
	}
}
