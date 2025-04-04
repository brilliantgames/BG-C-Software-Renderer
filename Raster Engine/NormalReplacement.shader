Shader "Custom/NormalReplacement"
{
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		Pass
		{
			Name "NORMAL"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 normal : TEXCOORD0;
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.normal = UnityObjectToWorldNormal(v.normal) * 0.5 + 0.5;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return fixed4(i.normal, 1.0);
			}
			ENDCG
		}
	}
}
