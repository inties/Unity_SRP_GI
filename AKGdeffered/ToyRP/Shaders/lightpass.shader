// #pragma debug
// #pragma disableoptimization
Shader "ToyRP/lightpass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [Toggle(_BRDF)] _BRDF ("Enable BRDF", Float) = 0
        [Toggle(_BSDF)] _BSDF ("Enable BSDF", Float) = 0
    }
    SubShader
    {
        Cull Off ZWrite On ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile_instancing
            #pragma enable_d3d11_debug_symbols
            // #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO
            // #pragma multi_compile _ STEREO_INSTANCING_ON
            // #pragma multi_compile _ STEREO_MULTIVIEW_ON
            // #pragma multi_compile _ STEREO_CUBEMAP_RENDER_ON
            #pragma shader_feature _BRDF _BSDF
            #include "UnityCG.cginc"
            #include "globaluniform.cginc"
            #include "global.cginc"
            #include "BRDF.cginc"
            #include "UnityLightingCommon.cginc"           
            #include "BSDF.cginc"
            #include "CSM.cginc"
            #include "Shadow.cginc"
            #include "RSM.cginc"
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }





            Material decodeMaterial(float4 GT4,float4 GT5)
            {
                Material m;
                m.baseColor = GT4.rgb;
                m.roughness = GT5.r;
                m.sheenTint = GT5.g;
                m.sheen = GT5.b;
                m.subsurface = GT5.a;
                return m;
            }
            fixed4 frag (v2f i, out float depthOut : SV_Depth) : SV_Target
            {
                #ifdef _BRDF
                    float2 uv = i.uv;
                    float4 GT2 = tex2D(_GT2, uv);
                    float4 GT3 = tex2D(_GT3, uv);
                    float4 GT4 = tex2D(_GT4, uv);
                    float4 GT5 = tex2D(_GT5, uv);
                    // 从 Gbuffer 解码数据
                    float3 albedo = tex2D(_GT0, uv).rgb;
                    float3 normal = tex2D(_GT1, uv).rgb * 2 - 1;
                    float2 motionVec = GT2.rg;
                    float roughness = GT2.b;
                    float metallic = GT2.a;
                    float3 emission = GT3.rgb;
                    float occlusion = GT3.a;
                    float3 baseColor = GT4.rgb;
                    float3 packed1 = GT5.rgb;
                    float subsurface = GT5.a;

                    float d = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, uv));
                    float d_lin = Linear01Depth(d);
                    depthOut = d;

                    // 反投影重建世界坐标
                    float4 ndcPos = float4(uv*2-1, d, 1);
                    float4 worldPos = mul(_vpMatrixInv, ndcPos);
                    worldPos /= worldPos.w;

                    // 计算参数
                    float3 N = normalize(normal);
                    float3 L = normalize(_WorldSpaceLightPos0.xyz);
                    float3 V = normalize(_WorldSpaceCameraPos.xyz - worldPos.xyz);
                    float3 radiance = _LightColor0.rgb;

                    float random=tex2D(_NoiseTex,uv*float2(_ScreenWidth,_ScreenHeight)/_noiseTexResolution).a;
                    float rotationAngle=random*2*3.14159;
                    float bias=0.01;
                    // 计算阴影
                    //float shadow = CalculateShadow(worldPos.xyz);
                    float shadow=PCF(worldPos.xyz,3,rotationAngle,bias);
                    //float shadow=PCSS(worldPos.xyz,rotationAngle,bias);
                    // 计算直接光照
                    float3 color = PBR(N, V, L, albedo, radiance, roughness, metallic);
                    //计算间接光照
                    float2 RSM_uv = calculate_uv(worldPos, _RSM_VPMatrix_0);
                    float3 indirectLight = calculateRsmIndirectLight(worldPos, N, RSM_uv,V,albedo,metallic);
                    // 计算环境光照
                    float3 ambient = IBL(
                        N, V,
                        albedo, roughness, metallic,
                        _diffuseIBL, _specularIBL, _brdfLut
                    );
                    
                    color += ambient * occlusion;
                    color += indirectLight;
                    color += emission;
                    color*=shadow;  //有问题
                    
                    
                    
                #endif
                #ifdef _BSDF
                    float2 uv = i.uv;
                    float3 normal = tex2D(_GT1, uv).rgb * 2 - 1;
                    float4 GT4 = tex2D(_GT4, uv);
                    float4 GT5 = tex2D(_GT5, uv);
                    // 从 Gbuffer 解码数据
                
                    float3 baseColor = GT4.rgb;
                    float3 packed1 = GT5.rgb;
                    float subsurface = GT5.a;
                    float d = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, uv));
                    float d_lin = Linear01Depth(d);
                    depthOut = d;
                    // 反投影重建世界坐标
                    float4 ndcPos = float4(uv*2-1, d, 1);
                    float4 worldPos = mul(_vpMatrixInv, ndcPos);
                    worldPos /= worldPos.w;
                


                    Material mat = decodeMaterial(GT4, GT5);
                    // 计算参数
                    float3 N = normalize(normal);
                    float3 L = normalize(_WorldSpaceLightPos0.xyz);
                    float3 V = normalize(_WorldSpaceCameraPos.xyz - worldPos.xyz);
                    float3 radiance = _LightColor0.rgb;

                    float3 H = normalize(L + V);

                    float pdf;
                    float3 color = EvalDisneyDiffuse(mat,N, V, L, H, pdf);
               
                #endif
                return float4(color, 1);
            }
            ENDCG
        }
    }
}
