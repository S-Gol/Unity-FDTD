Shader "VolRendering/DVRender"
{
    Properties
    {

    }
    SubShader
    {
        // No culling or depth
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        Cull Front

        ZTest LEqual
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha



        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_dvr

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
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.vertexLocal = v.vertex;
                o.normal = v.normal;
                return o;
            }
            float _MinVal;
            float _MaxVal;
            float _VelMult;
            float _OpacityMult;
            int3 size;
            StructuredBuffer<float3> u3Buffer;
            StructuredBuffer<float> weightBuffer;
            sampler2D _CMapTex;

            int to1d(int x, int y, int z)
            {
                return (z * size.x * size.y) + (y * size.x) + x;
            }

            float getDensity(float3 pos)
            {
                int bWidth = 3;
                //return sin(pos.x*20)*sin(pos.y*20)*sin(pos.z*20);
                int3 intPos = pos * size;
                int index = to1d(intPos.x, intPos.y, intPos.z);
                //return weightBuffer[index];
                bool border = ((intPos.x <= bWidth) + (intPos.y <= bWidth) + (intPos.z <= bWidth) + (intPos.x >= size.x - bWidth) + (intPos.y >= size.y - bWidth) + (intPos.z >= size.z - bWidth)) > 1;
                return length(u3Buffer[index])*5+ border*0.5;

            }


            // Direct Volume Rendering
            // Direct Volume Rendering
            fixed4 frag_dvr(v2f i) : SV_TARGET
            {
            #define NUM_STEPS 256
                const float stepSize = 1.732f/*greatest distance in box*/ / NUM_STEPS;

                float3 rayStartPos = i.vertexLocal + float3(0.5f, 0.5f, 0.5f);
                float3 rayDir = normalize(ObjSpaceViewDir(float4(i.vertexLocal, 0.0f)));

                float4 col = float4(0.0f, 0.0f, 0.0f, 0.0f);
                for (uint iStep = 0; iStep < NUM_STEPS; iStep++)
                {
                    const float t = iStep * stepSize;
                    const float3 currPos = rayStartPos + rayDir * t;
                    if (currPos.x < -0.0001f || currPos.x >= 1.0001f || currPos.y < -0.0001f || currPos.y > 1.0001f || currPos.z < -0.0001f || currPos.z > 1.0001f) // TODO: avoid branch?
                        break;

                    const float density = getDensity(currPos);

                    float4 src = float4(density, density, density, density * 0.1f);

                    col.rgb = src.a * src.rgb + (1.0f - src.a) * col.rgb;
                    col.a = src.a + (1.0f - src.a) * col.a;

                    if (col.a > 1.0f)
                        break;
                }
                return col;
            }
            ENDCG
        }
    }
}
