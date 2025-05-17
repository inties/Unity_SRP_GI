#ifndef RSM_INCLUDED
#define RSM_INCLUDED
float random(float2 seed)
{
    float2 s = seed * float2(12.9898, 78.233);
    float n = dot(s, float2(127.1, 311.7));
    return frac(sin(n) * 43758.5453);
}
float2 calculate_uv(float4 worldPos, float4x4 vpMatrixInv)
{
    float4 ndcPos = mul(vpMatrixInv, worldPos);
    float2 uv = ndcPos.xy / ndcPos.w;
    uv = (uv + 1) / 2;
    return uv;
}
float3 blinn_phong(float3 N,float3 V,float3 L,float3 albedo,float metallic)
{
    float3 F0=lerp(float3(0.04,0.04,0.04),albedo,metallic);
    float3 k_s=F0;
    float3 k_d=(1-k_s)*(1-metallic);
    float3 f_diffuse=albedo/PI;
    
    float3 H=normalize(L+V);
    float NdotH=max(0,dot(N,H));
    float NdotL=max(0,dot(N,L));
    float3 specular=k_s*pow(NdotH,20);
    float3 diffuse=k_d*f_diffuse;
    float3 color=diffuse+specular;
    return color;
}
// RSM 间接光照计算
float3 calculateRsmIndirectLight(float3 worldPos, float3 normal, float2 uv,float3 outdir,float3 albedo,float metallic)
{
    float3 indirectLight = float3(0, 0, 0);
    
    for (int j = 0; j < 400; j++)
    {
        float randnum1 = random(float2(j * 20, j * 40));
        float randnum2 = random(float2(j * 40, j * 20));
        float2 sampleuv = float2(uv.x + 40.0f/1024.0f * randnum1 * sin(2 * 3.14159f * randnum2),
                                uv.y + 40.0f/1024.0f * randnum1 * cos(2 * 3.14159f * randnum2));
        sampleuv = clamp(sampleuv, float2(0, 0), float2(1, 1));
        // 使用标准 Texture2D.Sample 方法采样
        float3 RSMflux = tex2D(_RSM_Flux_0, sampleuv).xyz;
        float3 RSMnormal = tex2D(_RSM_Normal_0, sampleuv).xyz;
        float3 RSMworldpos = tex2D(_RSM_WorldPos_0, sampleuv).xyz;
        
        float3 pq = worldPos - RSMworldpos;
        float3 normalpq = normalize(pq);
        float cos1 = dot(normal, -normalpq);
        float cos2 = dot(RSMnormal, normalpq);
        float dis = length(pq);
        float3 blinn_phong_light = blinn_phong(normal,outdir,-normalpq,albedo,metallic);
        indirectLight += 10*RSMflux*blinn_phong_light * max(0, cos1) * randnum1 * randnum1 * max(0, cos2) / (dis*dis+0.0001);

    }
    
    return indirectLight;
}



#endif

