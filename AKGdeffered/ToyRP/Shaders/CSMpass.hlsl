        #ifndef CSM_PASS_HLSL
        #define CSM_PASS_HLSL
struct appdata{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
};
struct v2f{
    float4 pos : SV_POSITION;
};
//不需要以下：
//判断属于哪一个视锥体
//变换到相应的阴影相机空间
//因为已经通过摄像机设置了
    v2f vert(appdata_base v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        return o;
    }
    void frag(v2f i)
    {
        // 不需要做任何事情，深度值会自动写入
    }
          
       #endif