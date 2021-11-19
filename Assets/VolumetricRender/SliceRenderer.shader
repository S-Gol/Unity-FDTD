Shader "VolRendering/SliceRender"
{

    SubShader
    {
        // No culling or depth
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        Cull Front

        ZTest LEqual
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off


        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag_mip

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 vertexLocal : TEXCOORD1;
                float3 normal : NORMAL;
                float4 position : POSITION1;

            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.vertexLocal = v.vertex;
                o.normal = v.normal;
                o.position = mul(unity_ObjectToWorld, v.vertex);

                return o;
            }
            float _MinVal;
            float _MaxVal;
            float _VelMult;
            float _OpacityMult;
            int3 size;
            StructuredBuffer<float3> u3Buffer;
            StructuredBuffer<float> weightBuffer;
            StructuredBuffer<int>matGridBuffer;
            StructuredBuffer<float> pressureMagBuffer;

            sampler2D _CMapTex;

            int to1d(int x, int y, int z)
            {
                return (z * size.x * size.y) + (y * size.x) + x;
            }
            const int bWidth = 50;
            float getDensity(float3 pos)
            {
                int3 intPos = pos * size;
                int index = to1d(intPos.x, intPos.y, intPos.z);
                bool inBounds = (intPos.x < size.x) && (intPos.y < size.y) && (intPos.z < size.z) && (intPos.x > 0) && (intPos.y > 0) && (intPos.z > 0);

                return pressureMagBuffer[index]* inBounds/1e12;
            }


            // Direct Volume Rendering
            fixed4 frag_mip(v2f i) : SV_TARGET
            {
                #define NUM_STEPS 1024
                const float stepSize = 1.732f/ NUM_STEPS;

                float3 pos = i.position+float3(0.5,0.5,0.5);
                
                float4 col = getDensity(pos);
                float alpha = !(pos.x < 0 || pos.y < 0 || pos.z < 0 || pos.x > 1 || pos.y > 1 || pos.z > 1);
                col.w = alpha;

                return col;
            }
            ENDCG
        }
    }
}
