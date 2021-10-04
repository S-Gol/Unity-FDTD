Shader "VolRendering/MIPRender"
{
    Properties
    {
        _MinVal ("Min value", float) = 0
        _MaxVal("Max value", float) = 2
        _CMapTex("Color map", 2D) = "white" {}
        _VelMult("Velocity Multiplier", Float) = 1000
        _OpacityMult("Opacity Multiplier", Float) = 1000


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
            StructuredBuffer<int>matGridBuffer;
            sampler2D _CMapTex;

            int to1d(int x, int y, int z)
            {
                return (z * size.x * size.y) + (y * size.x) + x;
            }
            const int bWidth = 50;
            float getDensity(float3 pos)
            {
                //return sin(pos.x*20)*sin(pos.y*20)*sin(pos.z*20);
                int3 intPos = pos * size;
                int index = to1d(intPos.x, intPos.y, intPos.z);
                //return weightBuffer[index];

                return length(u3Buffer[index]) * 5;
            }


            // Direct Volume Rendering
            fixed4 frag_mip(v2f i) : SV_TARGET
            {
                #define NUM_STEPS 1024
                const float stepSize = 1.732f/ NUM_STEPS;

                float3 rayStartPos = i.vertexLocal + float3(0.5f, 0.5f, 0.5f);
                float3 rayDir = normalize(ObjSpaceViewDir(float4(i.vertexLocal, 0.0f)));

                float maxDensity = 0.0f;
                for (uint iStep = 0; iStep < NUM_STEPS; iStep++)
                {
                    const float t = iStep * stepSize;
                    const float3 currPos = rayStartPos + rayDir * t;
                    // Stop when we are outside the box
                    if (currPos.x < -0.0001f || currPos.x >= 1.0001f || currPos.y < -0.0001f || currPos.y > 1.0001f || currPos.z < -0.0001f || currPos.z > 1.0001f) // TODO: avoid branch?
                        break;

                    const float density = getDensity(currPos);
                    if (density > _MinVal && density < _MaxVal)
                        maxDensity = max(density, maxDensity);
                }

                // Maximum intensity projection
                float4 col = tex2D(_CMapTex, float2(maxDensity* _VelMult, 0.5));
                //float4 col = float4(1, 1, 1, maxDensity);
                col.w = maxDensity* _OpacityMult;
                return col;
            }
            ENDCG
        }
    }
}
