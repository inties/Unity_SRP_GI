#ifndef BSDF_CGINC_INCLUDED
#define BSDF_CGINC_INCLUDED

float SchlickWeight(float u)
{
    float m = clamp(1.0 - u, 0.0, 1.0);
    float m2 = m * m;
    return m2 * m2 * m;
}
float Luminance(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

float3 EvalDisneyDiffuse(Material mat,float3 N, float3 V, float3 L, float3 H, out float pdf)
{   
    float lum = Luminance(mat.baseColor);
    float3 ctint = lum > 0.0 ? mat.baseColor / lum : float3(1.0, 1.0, 1.0);
    float3 Csheen = lerp(float3(1.0, 1.0, 1.0), ctint, mat.sheenTint);


    pdf = 0.0;
    if (dot(N, L) <= 0.0) return float3(0.0, 0.0, 0.0);

    float LDotH = dot(L, H);

    float Rr = 2.0 * mat.roughness * LDotH * LDotH;

    // Diffuse
    float FL = SchlickWeight(dot(N, L));
    float FV = SchlickWeight(dot(N, V));
    float Fretro = Rr * (FL + FV + FL * FV * (Rr - 1.0));
    float Fd = (1.0 - 0.5 * FL) * (1.0 - 0.5 * FV);

    // Fake subsurface
    float Fss90 = 0.5 * Rr;
    float Fss = lerp(1.0, Fss90, FL) * lerp(1.0, Fss90, FV);
    float ss = 1.25 * (Fss * (1.0 / (dot(N, L) + dot(N, V)) - 0.5) + 0.5);

    // Sheen
    float FH = SchlickWeight(dot(H, L));
    float3 Fsheen = FH * mat.sheen * Csheen;

    pdf = dot(N, L) * INV_PI;
    return INV_PI * mat.baseColor * lerp(Fd + Fretro, ss, mat.subsurface) + Fsheen;
}
float3 LambertDiffuse(float3 baseColor)
{
    return INV_PI * baseColor;
}


CBUFFER_START(MaterialParams)
    float4 _MaterialBaseColor;         // xyz = baseColor, w 可留给 metallic(如果有) 或 padding
    float4 _MaterialPacked1;           // x=roughness, y=specularTint, z=sheen, w=sheenTint
    float  _MaterialSubsurface;        
CBUFFER_END

Material LoadMaterial()
{
    Material m;
    m.baseColor    = _MaterialBaseColor.rgb;
    m.roughness    = _MaterialPacked1.x;
    m.specularTint = _MaterialPacked1.y;
    m.sheen        = _MaterialPacked1.z;
    m.sheenTint    = _MaterialPacked1.w;
    m.subsurface   = _MaterialSubsurface;
    return m;
}

#endif // BSDF_CGINC_INCLUDED