#ifndef RSM_CASTER_PASS_HLSL
#define RSM_CASTER_PASS_HLSL

#include "UnityCG.cginc"

//变换到光源空间，写入深度
//写入法线
//写入位置
//计算并写入辐照度
float4 _BSDF_BaseColor;
struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
};

struct v2f
{
    float4 pos : SV_POSITION;
    float3 worldPos : TEXCOORD1;
    float3 worldNormal : TEXCOORD2;
};

float3 _LightColor; // 主光源颜色
float _RSMIntensity; // 可选：RSM强度
float _LightType; // 光源类型

v2f vert(appdata v)
{
    v2f o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.worldNormal = UnityObjectToWorldNormal(v.normal);
    return o;
}

// MRT输出顺序：SV_Target0=Position, SV_Target1=Normal, SV_Target2=Flux
void frag(
    v2f i,
    out float4 outPosition : SV_Target0,
    out float4 outNormal   : SV_Target1,
    out float4 outFlux     : SV_Target2,
    out float depth:SV_Depth
)
{
    // 世界坐标
    outPosition = float4(i.worldPos, 1);

    // 世界法线，归一化后写入
    outNormal = float4(normalize(i.worldNormal) * 0.5 + 0.5, 1);

    // Flux（辐照度）：这里简单用主光源颜色，可根据材质和BRDF进一步完善
    // 你可以采样albedo贴图等，这里假设全白
    float3 albedo = _BSDF_BaseColor.rgb;
    float3 flux = float3(0,0,0);
    if(_LightType==0){//directional
        flux = albedo/3.1415926f * _LightColor * (_RSMIntensity == 0 ? 1.0 : _RSMIntensity);
    }
    else if(_LightType==1){//point
        flux = albedo * _LightColor * (_RSMIntensity == 0 ? 1.0 : _RSMIntensity);
    }
    else if(_LightType==2){//spot
        flux = albedo * _LightColor * (_RSMIntensity == 0 ? 1.0 : _RSMIntensity);
    }
    outFlux = float4(flux, 1);
}

#endif
