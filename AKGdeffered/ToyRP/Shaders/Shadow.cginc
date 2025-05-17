#ifndef SHADOW_HLSL
#define SHADOW_HLSL


    float2 RotateVec2(float2 v, float angle)
{
    float s = sin(angle);
    float c = cos(angle);

    return float2(v.x*c+v.y*s, -v.x*s+v.y*c);
}

    float PCF(float3 worldPos,float penumbraSize,float roatationAngle,float bias){
        float4 shadowPos = mul(_ShadowVP, float4(worldPos, 1.0));
        float3 shadowPosNDC = shadowPos.xyz / shadowPos.w;
        float2 uv = shadowPosNDC.xy * 0.5 + 0.5;
        
        // 检查是否在阴影贴图范围内
        if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
            return 1.0;
        if(shadowPosNDC.z<0.0||shadowPosNDC.z>1.0)
            return 1.0;

        float shadow = 0.0;
        float pixelSize = 1.0 / SHADOW_MAP_SIZE;
        
        // 根据半影大小计算采样范围
        float sampleArea = penumbraSize / SHADOW_MAP_SIZE;
        
        for(int i = 0; i < PCF_SAMPLES; i++)
        {
            // 使用泊松分布计算采样偏移
            float2 offset = poissonDisk[i] * sampleArea;
            offset = RotateVec2(offset, roatationAngle);
            float2 sampleUV = uv + offset;
            
            // 计算双线性插值所需的梯度
            float2 grad = frac(sampleUV * SHADOW_MAP_SIZE);
            
            // 获取4个相邻像素的深度值
            float2 texelPos = floor(sampleUV * SHADOW_MAP_SIZE) / SHADOW_MAP_SIZE;
            float depth00 = tex2D(_ShadowMap, texelPos).r;
            float depth10 = tex2D(_ShadowMap, texelPos + float2(pixelSize, 0)).r;
            float depth01 = tex2D(_ShadowMap, texelPos + float2(0, pixelSize)).r;
            float depth11 = tex2D(_ShadowMap, texelPos + float2(pixelSize, pixelSize)).r;
                
            // 对每个深度值进行阴影比较
            #if defined(UNITY_REVERSED_Z)
            float4 shadowVals = float4(
                depth00 > shadowPosNDC.z+0.001 ? SHADOW_INTENSITY : 1.0,
                depth10 > shadowPosNDC.z+0.001 ? SHADOW_INTENSITY : 1.0,
                depth01 > shadowPosNDC.z+0.001 ? SHADOW_INTENSITY : 1.0,
                depth11 > shadowPosNDC.z+0.001 ? SHADOW_INTENSITY : 1.0
            );
            #else
            float4 shadowVals = float4(
                depth00 > shadowPosNDC.z ? SHADOW_INTENSITY : 1.0,
                depth10 > shadowPosNDC.z ? SHADOW_INTENSITY : 1.0,
                depth01 > shadowPosNDC.z ? SHADOW_INTENSITY : 1.0,
                depth11 > shadowPosNDC.z ? SHADOW_INTENSITY : 1.0
            );
            #endif
            
            // 双线性插值
            float bilinearShadow = lerp(
                lerp(shadowVals.x, shadowVals.y, grad.x),
                lerp(shadowVals.z, shadowVals.w, grad.x),
                grad.y
            );
            shadow += bilinearShadow;
        }
        return shadow / PCF_SAMPLES;
    }
        // 计算平均遮挡距离
        float CalculateAvgBlockerDepth(float3 shadowPosNDC)
        {
            float2 uv = shadowPosNDC.xy * 0.5 + 0.5;
            
            // 根据光源大小和正交投影宽度计算搜索范围
            float searchRadius = LIGHT_SIZE / (ORTHO_WIDTH);
            
            float blockerSum = 0.0;
            float blockerCount = 0.0;
            
            for(int i=-3;i<=3;i++)
            {
                for(int j=-3;j<=3;j++)
                {
                    float2 sampleUV=uv+float2(i,j)/3.0f*searchRadius;
                    float sampleDepth=tex2D(_ShadowMap,sampleUV).r;
                    #if defined(UNITY_REVERSED_Z)
                    if(sampleDepth>shadowPosNDC.z)
                    {
                        blockerSum += sampleDepth;
                        blockerCount += 1.0;
                    }
                    #else
                    if(sampleDepth<shadowPosNDC.z)
                    {
                        blockerSum += sampleDepth;
                        blockerCount += 1.0;
                    }
                    #endif
                }
            }
            if(blockerCount<1.0)
                return -1.0;
            return blockerSum / blockerCount;
        }
        
        // 计算半影大小
        float CalculatePenumbraSize(float blockerDistance, float receiverDistance)
        {
            if(blockerDistance < 0.0)
                return 1.0;
                
            float penumbraWidth = (receiverDistance - blockerDistance) * LIGHT_SIZE / blockerDistance;
            penumbraWidth = (penumbraWidth/ORTHO_WIDTH)*SHADOW_MAP_SIZE; // 转换到阴影贴图
            return min(penumbraWidth, MAX_PENUMBRA_SIZE);
        }
        // PCSS主函数
        float PCSS(float3 worldPos,float rotationAngle,float bias)
        {
            // 转换到阴影空间
            float4 shadowPos = mul(_ShadowVP, float4(worldPos, 1.0));
            shadowPos.xyz /= shadowPos.w;
            shadowPos.z += bias;
            float3 shadowPosNDC = shadowPos.xyz / shadowPos.w;
            
            // 1. 计算平均遮挡距离
            float blockerDistance = CalculateAvgBlockerDepth(shadowPosNDC);
            if(blockerDistance < 0.0) return 1.0;
            #if defined(UNITY_REVERSED_Z)
            blockerDistance =(1.0-blockerDistance)*ORTHO_DISTANCE;
            float receiverDistance = (1.0-shadowPosNDC.z)*ORTHO_DISTANCE;
            #else
            blockerDistance =blockerDistance*ORTHO_DISTANCE;
            float receiverDistance =shadowPosNDC.z*ORTHO_DISTANCE;
            #endif
            // 2. 计算半影大小
            float penumbraSize = CalculatePenumbraSize(blockerDistance, receiverDistance);
            
            float shadow = PCF(worldPos,penumbraSize,rotationAngle,bias);
            return shadow;
        }


// 计算阴影
float CalculateShadow(float3 worldPos)
{
    float4 shadowPos = mul(_ShadowVP, float4(worldPos, 1.0));
    shadowPos.xyz /= shadowPos.w;
    
    float2 uv = shadowPos.xy * 0.5 + 0.5;
    
    if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
        return 1.0;
    if(shadowPos.z<0.0)
        return 1.0;

    float pixelSize = 1.0 / SHADOW_MAP_SIZE;
    float2 grad = frac(uv * SHADOW_MAP_SIZE);
    
    // 获取4个相邻像素的深度值
    float2 texelPos = floor(uv * SHADOW_MAP_SIZE) / SHADOW_MAP_SIZE;
    float depth00 = tex2D(_ShadowMap, texelPos).r;
    float depth10 = tex2D(_ShadowMap, texelPos + float2(pixelSize, 0)).r;
    float depth01 = tex2D(_ShadowMap, texelPos + float2(0, pixelSize)).r;
    float depth11 = tex2D(_ShadowMap, texelPos + float2(pixelSize, pixelSize)).r;
    
    // 对每个深度值进行阴影比较
    #if defined(UNITY_REVERSED_Z)
    float4 shadowVals = float4(
        depth00 > shadowPos.z ? 0.0 : 1.0,
        depth10 > shadowPos.z ? 0.0 : 1.0,
        depth01 > shadowPos.z ? 0.0 : 1.0,
        depth11 > shadowPos.z ? 0.0 : 1.0
    );
    #else
    float4 shadowVals = float4(
        depth00 > shadowPos.z ? 0.0 : 1.0,
        depth10 > shadowPos.z ? 0.0 : 1.0,
        depth01 > shadowPos.z ? 0.0 : 1.0,
        depth11 > shadowPos.z ? 0.0 : 1.0
    );
    #endif
    
    // 双线性插值
    return lerp(
        lerp(shadowVals.x, shadowVals.y, grad.x),
        lerp(shadowVals.z, shadowVals.w, grad.x),
        grad.y
    );
}

#endif

