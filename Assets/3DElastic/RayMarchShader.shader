Shader "FDTD/Render3D"
{
    Properties
    {
        _MinVal ("Min value", float) = 0
        _MaxVal("Max value", float) = 2

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

                return o;
            }
            float _MinVal;
            float _MaxVal;
            int3 _Size;
            StructuredBuffer<float> testBuffer;


            int to1d(int x, int y, int z)
            {
                return (z * _Size.x * _Size.y) + (y * _Size.x) + x;
            }

            float getDensity(float3 pos)
            {
                //return sin(pos.x*20)*sin(pos.y*20)*sin(pos.z*20);
                int3 index;
                index.x = pos.x * _Size.x;
                index.y = pos.y * _Size.y;
                index.z = pos.z * _Size.z;

                return testBuffer[to1d(index.x, index.y, index.z)];

            }

            sampler2D _MainTex;

            // Direct Volume Rendering
            fixed4 frag_mip(v2f i) : SV_TARGET
            {
                #define NUM_STEPS 512
                const float stepSize = 1.732f/*greatest distance in box*/ / NUM_STEPS;

                float3 rayStartPos = i.vertexLocal + float3(0.5f, 0.5f, 0.5f);
                float3 rayDir = normalize(ObjSpaceViewDir(float4(i.vertexLocal, 0.0f)));

                float maxDensity = 0.0f;
                for (uint iStep = 0; iStep < NUM_STEPS; iStep++)
                {
                    const float t = iStep * stepSize;
                    const float3 currPos = rayStartPos + rayDir * t;
                    // Stop when we are outside the box
                    if (currPos.x < 0.0f || currPos.x >= 1.0f || currPos.y < 0.0f || currPos.y > 1.0f || currPos.z < 0.0f || currPos.z > 1.0f)
                        break;

                    const float density = getDensity(currPos);
                    if (density > _MinVal && density < _MaxVal)
                        maxDensity = max(density, maxDensity);
                }

                // Maximum intensity projection
                float4 col = float4(1, 1, 1, maxDensity);

                return col;
            }
            ENDCG
        }
    }
}
