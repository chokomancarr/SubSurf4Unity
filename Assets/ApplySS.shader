Shader "Unlit/ApplySS"
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
				int n; //XXXX_TYPE_NNNN_4444 _ 3333_2222_1111_0000
				int f00, f01, f10, f11, f20, f21, f30, f31, f40, f41;
				int eid; // XXXX_XXX4_4444_3333 _ 3222_2211_1110_0000 (mask: which face this edge use 43210)
				int e0, e1, e2, e3, e4;
				int v;
				int unused1, unused2;
				/*
				ep uses:
				  int n; //XXXX_TYPE_XXXX_XXXX _ XXXX_XXXX_1111_0000
				  int f00, f01, f20(as f02), f10, f11, f21(as f12);
				  int e0, e1;

				fp uses:
				 int n; //XXXX_TYPE_XXXX_XXXX _ XXXX_XXXX_XXXX_NNNN
				 int f00, f01, f10(as f02), f11(as f03), f20(as f04);
				*/
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
			sampler2D _oriTex;
			int _oriTexSize;
			StructuredBuffer<oriBuffer> ssBuffer;

			inline float4 getP(float a) {
				float4 c = tex2Dlod(_oriTex, float4((fmod(a, _oriTexSize) + 0.5) / _oriTexSize, (floor(a / _oriTexSize) + 0.5) / _oriTexSize, 0, 0)); //new Vector2(((a % oriSize) + 0.5f) / oriSize, ((a / oriSize) + 0.5f) / oriSize);
				float dx = floor(c.a / 4);
				float dy = floor((c.a - dx) / 2);
				float dz = c.a - dx*4 - dy*2;
				return float4(c.x * (dx * 2 - 1), c.y * (dy * 2 - 1), c.z * (dz * 2 - 1), 1);
			}

			inline float4 getFP(float4 v, float4 fp1, float4 fp2, float4 e0, float4 e1, float4 e2, float4 e3, float4 e4, uint mask, uint n) {
				return (v + fp1 + fp2 + e0*(mask & 1) + e1*(mask & 2)*0.5 + e2*(mask & 4)*0.25 + e3*(mask & 8)*0.125 + e4*(mask * 16)*0.0625) / n;
			}

			inline float4 getPos(oriBuffer buff) {
				int type = (buff.n & (15 << 24)) >> 24;
				float4 f00 = getP(buff.f00);
				float4 f01 = getP(buff.f01);
				float4 f10 = getP(buff.f10);
				float4 f11 = getP(buff.f11);
				float4 f20 = getP(buff.f20);
				if (type == 2) { //face
					//float4 r = tex2Dlod(_oriTex, float4((fmod(buff.f00, _oriTexSize) + 0.5) / _oriTexSize, (floor(buff.f00 / _oriTexSize) + 0.5) / _oriTexSize, 0, 0));
					//return float4((fmod(buff.f00, _oriTexSize) + 0.5) / _oriTexSize, (floor(buff.f00 / _oriTexSize) + 0.5) / _oriTexSize, 0, 1);
					//return float4(r.x, r.y, r.z, 1);
					return (f00 + f01 + f10 + f11 + f20) / (buff.n & 15);
				}
				float4 f21 = getP(buff.f21);
				float4 e0 = getP(buff.e0);
				float4 e1 = getP(buff.e1);
				if (type == 1) { //edge
					float4 fp0 = (f00 + f01 + f20 + e0 + e1) / (buff.n & 15);
					float4 fp1 = (f10 + f11 + f21 + e0 + e1) / ((buff.n & (15 << 8)) >> 8);
					return 0;
					//return 0.25*(fp0 + fp1 + e0 + e1);
				}
				return 0;
				float4 f30 = getP(buff.f30);
				float4 f31 = getP(buff.f31);
				float4 f40 = getP(buff.f40);
				float4 f41 = getP(buff.f41); 
				float4 e2 = getP(buff.e2);
				float4 e3 = getP(buff.e3);
				float4 e4 = getP(buff.e4);
				float4 v = getP(buff.v);

				float4 fp0 = getFP(v, f00, f01, e0, e1, e2, e3, e4, buff.eid & 31, buff.n & 15);
				float4 fp1 = getFP(v, f10, f11, e0, e1, e2, e3, e4, (buff.eid & (31 << 5)) >> 5, (buff.n & (15 << 4)) >> 4);
				float4 fp2 = getFP(v, f20, f21, e0, e1, e2, e3, e4, (buff.eid & (31 << 10)) >> 10, (buff.n & (15 << 8)) >> 8);
				float4 fp3 = getFP(v, f30, f31, e0, e1, e2, e3, e4, (buff.eid & (31 << 15)) >> 15, (buff.n & (15 << 12)) >> 12);
				float4 fp4 = getFP(v, f40, f41, e0, e1, e2, e3, e4, (buff.eid & (31 << 20)) >> 20, (buff.n & (15 << 16)) >> 16);
				return ((fp0 + fp1 + fp2 + fp3 + fp4 + e0 + e1 + e2 + e3 + e4)/buff.n + (buff.n-2)*v) / buff.n;
			}
			
			//adjust vertex to value from buffer
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				//o.vertex = mul(UNITY_MATRIX_MVP, mul(unity_WorldToObject, getPos(ssBuffer[round(v.uv4.y * 65536 + v.uv4.x * 256)])));
				//o.dat = getPos(ssBuffer[round(v.uv4.y * 65536 + v.uv4.x * 256)]);

				//uint a = ssBuffer[round(v.uv4.y * 65536 + v.uv4.x * 256)].f00;
				//o.uv = tex2Dlod(_oriTex, float4(0.1, 0.9, 0, 0));
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				float f00 = ssBuffer[round(v.uv4.y * 65536 + v.uv4.x * 256)].f00;
				o.dat = tex2Dlod(_oriTex, float4((fmod(f00, _oriTexSize) + 0.5) / _oriTexSize, (floor(f00 / _oriTexSize) + 0.5) / _oriTexSize, 0, 0));
				UNITY_TRANSFER_FOG(o, o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				//fixed4 col = tex2D(_oriTex, i.uv);
				// apply fog
				//UNITY_APPLY_FOG(i.fogCoord, col);
				//return i.uv.x;
				return i.dat;
			}
			ENDCG
		}
	}
}
