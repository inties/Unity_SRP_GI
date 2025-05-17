Shader "ToyRP/gbuffer"
{
    Properties
    {
        _MainTex ("Albedo Map", 2D) = "white" {}
        [Space(25)]

        _Metallic_global ("Metallic", Range(0, 1)) = 0.5
        _Roughness_global ("Roughness", Range(0, 1)) = 0.5
        [Toggle] _Use_Metal_Map ("Use Metal Map", Float) = 1
        _MetallicGlossMap ("Metallic Map", 2D) = "white" {}
        //[Enum(Metallic Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel ("Smoothness texture channel", Float) = 0
        [Space(25)]
        
        _EmissionMap ("Emission Map", 2D) = "black" {}
        [Space(25)]

        _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        [Space(25)]

        [Toggle] _Use_Normal_Map ("Use Normal Map", Float) = 1
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        [Space(25)]
        _BSDF_BaseColor("Base Color", Color)   = (1,1,1,1)
        _BSDF_Packed1 ("Rough/SheenTint/Sheen/Subsurface", Vector) = (0.5,0,0,0)
        //_BSDF_Subsurface("Subsurface", Range(0,1))  = 0
    }
    SubShader
    {
        

        Pass
        {
            Tags { "LightMode"="gbuffer" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "gbufferpass.hlsl"
            ENDHLSL
        }
        
        
        Pass
        {
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            Cull Off
            
            HLSLPROGRAM   
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "ShadowCasterPass.hlsl"
            ENDHLSL
        }

        pass
        {
            Tags { "LightMode"="CSM" }
            ZWrite On
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "CSMpass.hlsl"
            ENDHLSL

        }
         pass
        {
            Tags { "LightMode"="RSM_Caster" }
            ZWrite On
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "RSM_CasterPass.hlsl"
            ENDHLSL

        }
    }
    

}