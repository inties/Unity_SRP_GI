#ifndef GBUFFER_PASS_HLSL
#define GBUFFER_PASS_HLSL 

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
    float3 normal : NORMAL;
};

float4 _MainTex_ST;
sampler2D _MainTex;
sampler2D _MetallicGlossMap;
sampler2D _EmissionMap;
sampler2D _OcclusionMap;
sampler2D _BumpMap;

float _Use_Metal_Map;
float _Use_Normal_Map;
float _Metallic_global;
float _Roughness_global;
float4 _BSDF_BaseColor;
float4 _BSDF_Packed1;

v2f vert (appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.normal = UnityObjectToWorldNormal(v.normal);
    return o;
}

void frag (
    v2f i,
    out float4 GT0 : SV_Target0,
    out float4 GT1 : SV_Target1,
    out float4 GT2 : SV_Target2,
    out float4 GT3 : SV_Target3,
    out float4 GT4 : SV_Target4,
    out float4 GT5 : SV_Target5
)
{
    float4 color = tex2D(_MainTex, i.uv);
    float3 emission = tex2D(_EmissionMap, i.uv).rgb;
    float3 normal = i.normal;
    float metallic = _Metallic_global;
    float roughness = _Roughness_global;
    float ao = tex2D(_OcclusionMap, i.uv).g;

    if(_Use_Metal_Map)
    {
        float4 metal = tex2D(_MetallicGlossMap, i.uv);
        metallic = metal.r;
        roughness = 1.0 - metal.a;
    }
    //if(_Use_Normal_Map) normal = UnpackNormal(tex2D(_BumpMap, i.uv));

    GT0 = color;
    GT1 = float4(normal*0.5+0.5, 0);
    GT2 = float4(0, 0, roughness,metallic);
    GT3 = float4(emission, ao);
    GT4 = float4(_BSDF_BaseColor.rgb, 1);
    GT5 = _BSDF_Packed1;
}

#endif
    