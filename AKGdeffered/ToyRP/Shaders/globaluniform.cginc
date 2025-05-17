#ifndef GLOBALUNIFORM_HLSL
#define GLOBALUNIFORM_HLSL
            sampler2D _gdepth;
            sampler2D _GT0;
            sampler2D _GT1;
            sampler2D _GT2;
            sampler2D _GT3;
            sampler2D _GT4;
            sampler2D _GT5;
            sampler2D _ShadowMap; // 阴影贴图
            sampler2D _CSM_1;
            sampler2D _CSM_2;
            sampler2D _CSM_3;
            sampler2D _CSM_4;
            sampler2D _NoiseTex;

            samplerCUBE _diffuseIBL;
            samplerCUBE _specularIBL;
            sampler2D _brdfLut;
            
            float _ScreenWidth;
            float _ScreenHeight;
            float _noiseTexResolution;
            
            float4x4 _vpMatrix;
            float4x4 _vpMatrixInv;
            float4x4 _ShadowVP; // 阴影矩阵
            float4x4 _CSMVP_1;
            float4x4 _CSMVP_2;
            float4x4 _CSMVP_3;
            float4x4 _CSMVP_4;
            float3 _LightDir; // 光源方向


            float _RSMLightsCount;
            float _RsmSampleCount=400;
            float _RsmSampleRadius=30;
            float _RsmResolution=1024;
            float4x4 _RSM_VPMatrix_0;
            // float _RSMResolution;
            
            
            // 声明纹理
            sampler2D _RSM_Flux_0;
            sampler2D _RSM_Normal_0;
            sampler2D _RSM_WorldPos_0;
#endif
