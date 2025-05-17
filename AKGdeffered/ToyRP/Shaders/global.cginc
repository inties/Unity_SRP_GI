#ifndef GLOBAL_HLSL
#define GLOBAL_HLSL
struct Material
{
    float3 baseColor;
    float opacity;
    int alphaMode;
    float alphaCutoff;
    float3 emission;
    float anisotropic;
    float metallic;
    float roughness;
    float subsurface;
    float specularTint;
    float sheen;
    float sheenTint;
    float clearcoat;
    float clearcoatRoughness;
    float specTrans;
    float ior;
    float ax;
    float ay;
  
};
//#define PI 3.14159265358979323846
#define INV_PI 0.31830988618379067154
// PCSS相关参数
#define LIGHT_SIZE 20.0
#define SHADOW_MAP_SIZE 1024.0
#define PCF_SAMPLES 16
#define MAX_PENUMBRA_SIZE 10.0
#define ORTHO_WIDTH 1000.0
#define ORTHO_DISTANCE 10.0
#define SHADOW_INTENSITY 0.1

static float2 poissonDisk[16] = {
    float2( -0.94201624, -0.39906216 ),
    float2( 0.94558609, -0.76890725 ),
    float2( -0.094184101, -0.92938870 ),
    float2( 0.34495938, 0.29387760 ),
    float2( -0.91588581, 0.45771432 ),
    float2( -0.81544232, -0.87912464 ),
    float2( -0.38277543, 0.27676845 ),
    float2( 0.97484398, 0.75648379 ),
    float2( 0.44323325, -0.97511554 ),
    float2( 0.53742981, -0.47373420 ),
    float2( -0.26496911, -0.41893023 ),
    float2( 0.79197514, 0.19090188 ),
    float2( -0.24188840, 0.99706507 ),
    float2( -0.81409955, 0.91437590 ),
    float2( 0.19984126, 0.78641367 ),
    float2( 0.14383161, -0.14100790 )
};
#endif
