Shader "FDTD/Elastic2DRender"
{
    Properties
    {
        _CMapTex ("Color map", 2D) = "white" {}
        _VelMult("Velocity Multiplier", Float) = 1000

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D velTexture;
            sampler2D _CMapTex;
            float _VelMult;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 VelStress = tex2D(velTexture, i.uv);
                float magnitude = pow(pow(VelStress.x, 2) + pow(VelStress.y, 2), 0.5) * _VelMult;

                fixed4 col = tex2D(_CMapTex, float2(magnitude,0.5));

                // apply fog
                return col;
            }
            ENDCG
        }
    }
}
