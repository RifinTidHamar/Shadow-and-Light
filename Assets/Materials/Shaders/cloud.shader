Shader "Unlit/cloud"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/CSShaders/noiseSimplex.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                //float3 worldVertex = mul(unity_ObjectToWorld, v.vertex).xyz;
                //v.vertex.yz += snoise(v.vertex.xz *  2 * (worldVertex.y * 10000) + _Time.z)/15;
                //v.vertex = mul(unity_ObjectToWorld, v.vertex);
                float time = _Time.x * 0.1;
                v.vertex.y += snoise(float3(v.vertex.xz * 2 - time, time)) * 0.05;
                v.vertex.z += snoise(v.vertex.xz * 10 - time * 0.75) * 0.02;
                o.vertex = UnityObjectToClipPos(v.vertex);
                //o.vertex = mul(UNITY_MATRIX_VP, v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv).r;
                return col;
            }
            ENDCG
        }
    }
}
