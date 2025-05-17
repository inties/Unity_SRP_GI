#ifndef SHADOW_CASTER_PASS_HLSL
#define SHADOW_CASTER_PASS_HLSL

    struct v2f
    {
        float4 pos : SV_POSITION;
    };

    v2f vert(appdata_base v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        return o;
    }

    // 对于阴影贴图，我们只需要写入深度值
    // 不需要返回颜色，因为阴影贴图是深度纹理
    void frag(v2f i)
    {
        // 不需要做任何事情，深度值会自动写入
    }

#endif

