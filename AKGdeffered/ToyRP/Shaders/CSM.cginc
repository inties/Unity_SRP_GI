#ifndef CSM_HLSL
#define CSM_HLSL

float CalculateCSM(float3 worldPos, float d_lin)
{
    // 定义级联分割点（可以根据需要调整）
    float splitPoints[5] = {0.0, 0.1, 0.3, 0.6, 1.0};
    
    // 确定使用哪个级联
    int cascadeIndex = 0;
    for(int i = 1; i < 5; i++)
    {
        if(d_lin < splitPoints[i])//深度计算有问题？？？？？
        {
            cascadeIndex = i - 1;
            break;
        }
    }
    
    // 根据级联索引选择正确的阴影贴图和变换矩阵
    float4x4 shadowVP;
    sampler2D shadowMap;
    
    switch(cascadeIndex)
    {
        case 0:
            shadowVP = _CSMVP_1;
            shadowMap = _CSM_1;
            break;
        case 1:
            shadowVP = _CSMVP_2;
            shadowMap = _CSM_2;
            break;
        case 2:
            shadowVP = _CSMVP_3;
            shadowMap = _CSM_3;
            break;
        case 3:
            shadowVP = _CSMVP_4;
            shadowMap = _CSM_4;
            break;
    }
    
    // 计算阴影
    float4 shadowPos = mul(shadowVP, float4(worldPos, 1.0));
    shadowPos.xyz /= shadowPos.w;
    
    // 转换到UV坐标
    float2 shadowUV = shadowPos.xy * 0.5 + 0.5;
    
    // 检查是否在阴影贴图范围内
    if (shadowUV.x < 0 || shadowUV.x > 1 || shadowUV.y < 0 || shadowUV.y > 1)
        return 1.0;
    if(shadowPos.z<0.0||shadowPos.z>1.0)
        return 1.0;
    // 获取阴影贴图中的深度值
    float shadowDepth = tex2D(shadowMap, shadowUV).r;
    
    // 计算当前片段在阴影空间中的深度
    float currentDepth = shadowPos.z;
    
    // 添加一个小的偏移量，避免自遮挡
    float bias = 0.005;
    #if defined (UNITY_REVERSED_Z)
    return currentDepth + bias > shadowDepth ? 1.0 : 0.0;
    #else
    return currentDepth - bias > shadowDepth ? 0.0 : 1.0;
    #endif
}

#endif

