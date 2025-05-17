using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

using System.Runtime.InteropServices;
//using System.Runtime.Intrinsics.Arm;
public class ToyRenderPipeline : RenderPipeline
{   RenderTexture shadowMap;                                           // shadow map attachment
    RenderTexture shadowMap_1;                                        // CSM level 1
    RenderTexture shadowMap_2;                                        // CSM level 2
    RenderTexture shadowMap_3;                                        // CSM level 3
    RenderTexture shadowMap_4;                                        // CSM level 4
    
    RenderTexture gdepth;                                               // depth attachment
    RenderTexture[] gbuffers = new RenderTexture[6];                    // color attachments 
    RenderTargetIdentifier[] gbufferID = new RenderTargetIdentifier[6]; // tex ID 
    RenderTexture lightPassTex;                                         // 存储 light pass 的结果
    List<LightController> rsmLights = new List<LightController>();           // 存储 rsm 数据
    //CSM csm = new CSM();
    // IBL 贴图
    public Cubemap diffuseIBL;
    public Cubemap specularIBL;
    public Texture brdfLut;
    Light[] lights = new Light[10];
    Light directionalLight = null;
    public Texture noiseTexture;


    // 更新RSM光源列表
    public void UpdateRSMLights()
    {
        // 释放旧的RSM纹理
        foreach (var rsmData in rsmLights)
        {
            rsmData.ReleaseRSMTextures();
        }
        rsmLights.Clear();

        // 查找所有带有LightController组件的光源
        var lightControllers = Object.FindObjectsOfType<LightController>();
        foreach (var controller in lightControllers)
        {
            if(controller.enableRSM){
                rsmLights.Add(controller);
                controller.CreateRSMTextures();
            }
        }
    }

    public ToyRenderPipeline()
    {
        // 创建纹理
        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);
        
        gdepth = new RenderTexture(width, height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        gbuffers[0] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        gbuffers[1] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);
        gbuffers[2] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
        gbuffers[3] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        gbuffers[4] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        gbuffers[5] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        lightPassTex = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
     
        // 初始化所有shadowmap
        shadowMap = new RenderTexture(1024, 1024, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        shadowMap_1 = new RenderTexture(1024, 1024, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        shadowMap_2 = new RenderTexture(1024, 1024, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        shadowMap_3 = new RenderTexture(1024, 1024, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        shadowMap_4 = new RenderTexture(1024, 1024, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        
        // 给纹理 ID 赋值
        for(int i=0; i<6; i++)
            gbufferID[i] = gbuffers[i];

        // 获取场景中的平行光
        lights = Object.FindObjectsOfType<Light>();
       
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                directionalLight = light;
                break;
            }
        }

        if (directionalLight == null)
        {
            Debug.LogWarning("场景中没有找到平行光，无法渲染阴影");
            return;
        }

        // 初始化RSM光源列表
        UpdateRSMLights();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)  
     {
        //设置RSM纹理为全局
        for(int i=0; i<rsmLights.Count; i++){
            Shader.SetGlobalTexture("_RSM_Position_"+i, rsmLights[i].rsmPosition);
            Shader.SetGlobalTexture("_RSM_Normal_"+i, rsmLights[i].rsmNormal);
            Shader.SetGlobalTexture("_RSM_Flux_"+i, rsmLights[i].rsmFlux);
        }
        Shader.SetGlobalInt("_RSMLightsCount", rsmLights.Count);
        Shader.SetGlobalTexture("_NoiseTex", noiseTexture);
        Shader.SetGlobalFloat("_noiseTexResolution", noiseTexture.width);
        Shader.SetGlobalFloat("_ScreenWidth", Screen.width);
        Shader.SetGlobalFloat("_ScreenHeight", Screen.height);
        // 主相机
        Camera camera = cameras[0];
        context.SetupCameraProperties(camera);

        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "gbuffer";

        // 清屏
        cmd.SetRenderTarget(gbufferID, gdepth);
        cmd.ClearRenderTarget(true, true, Color.clear);
        
        // 设置 gbuffer 为全局纹理
        cmd.SetGlobalTexture("_gdepth", gdepth);
        for(int i=0; i<6; i++) 
            cmd.SetGlobalTexture("_GT"+i, gbuffers[i]);

        // 设置相机矩阵
        Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        Matrix4x4 vpMatrix = projMatrix * viewMatrix;
        Matrix4x4 vpMatrixInv = vpMatrix.inverse;
        cmd.SetGlobalMatrix("_vpMatrix", vpMatrix);
        cmd.SetGlobalMatrix("_vpMatrixInv", vpMatrixInv);

        // 设置 IBL 贴图
        cmd.SetGlobalTexture("_diffuseIBL", diffuseIBL);
        cmd.SetGlobalTexture("_specularIBL", specularIBL);
        cmd.SetGlobalTexture("_brdfLut", brdfLut);

        context.ExecuteCommandBuffer(cmd);

        //Gbuffer pass
            // 剔除
        camera.TryGetCullingParameters(out var cullingParameters);
        var cullingResults = context.Cull(ref cullingParameters);

            // config settings
        ShaderTagId shaderTagId = new ShaderTagId("gbuffer");   // 使用 LightMode 为 gbuffer 的 shader
        SortingSettings sortingSettings = new SortingSettings(camera);
        DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;

            // 绘制
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        context.Submit();  
        RSM_CasterPass(context, camera); 
        // 渲染阴影
        ShadowPass(context, camera);
        
        //CSMPass(context, camera);
        LightPass(context, camera);
   
        // 前向渲染 Pass
        ForwardPass(context, camera, cullingResults);
        
        // 最终合成 Pass
        FinalPass(context, camera);

        // skybox and Gizmos
        context.DrawSkybox(camera);
        if (Handles.ShouldRenderGizmos()) 
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }

        // 提交绘制命令
        context.Submit();
    }
    void RSM_CasterPass(ScriptableRenderContext context, Camera camera){
        SaveMainCameraSettings(ref camera);
    for(int i=0; i<rsmLights.Count; i++)
        {
            // 1. 设置相机到光源视角（可用临时相机或修改主相机，渲染完后还原）
            // 2. 设置MRT渲染目标
            //完成深度和相机视角的设置
          
            Vector3 center = rsmLights[i].transform.position;
            float shadowDistance = 20f;
            camera.transform.position = center - rsmLights[i].transform.forward * shadowDistance;
            camera.transform.rotation = Quaternion.LookRotation(rsmLights[i].transform.forward);
            camera.orthographic = true;
            camera.orthographicSize = 20;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = shadowDistance * 2;
            context.SetupCameraProperties(camera);
        // 设置相机矩阵
            Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
            Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            Matrix4x4 vpMatrix = projMatrix * viewMatrix;
            Matrix4x4 vpMatrixInv = vpMatrix.inverse;
            Shader.SetGlobalMatrix("_RSM_VPMatrix_"+i, vpMatrix);
            Shader.SetGlobalMatrix("_RSM_VPMatrixInv_"+i, vpMatrixInv);


            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "RSM_CasterPass_" + rsmLights[i].name;
            RenderTargetIdentifier[] mrt = new RenderTargetIdentifier[] {
                rsmLights[i].rsmPosition, rsmLights[i].rsmNormal, rsmLights[i].rsmFlux
            };
            cmd.SetRenderTarget(mrt, rsmLights[i].rsmDepth);
            cmd.ClearRenderTarget(true, true, Color.clear);
            cmd.SetGlobalVector("_LightColor", rsmLights[i].lightColor);
            cmd.SetGlobalFloat("_RSMIntensity", rsmLights[i].intensity);
            cmd.SetGlobalFloat("_LightType", (int)rsmLights[i].lightType);
            context.ExecuteCommandBuffer(cmd);
            cmd.Release();
            Camera rsmCamera = camera;
            // 3. 剔除
            rsmCamera.TryGetCullingParameters(out var cullingParams);
            var cullingResults = context.Cull(ref cullingParams);

            // 4. 设置DrawingSettings
            ShaderTagId shaderTagId = new ShaderTagId("RSM_Caster");
            SortingSettings sortingSettings = new SortingSettings(rsmCamera);
            DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;

            // 5. 绘制
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
            context.Submit();

            // 6. 恢复相机参数（如有）
        }
        RevertMainCameraSettings(ref camera);
        context.SetupCameraProperties(camera);
    }
    // 光照 Pass : 计算 PBR 光照并且存储到 lightPassTex 纹理
    void LightPass(ScriptableRenderContext context, Camera camera)
    {
        // 使用 Blit
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "lightpass";
        for(int i=0; i<rsmLights.Count; i++){
            cmd.SetGlobalTexture("_RSM_WorldPos_"+i, rsmLights[i].rsmPosition);
            cmd.SetGlobalTexture("_RSM_Normal_"+i, rsmLights[i].rsmNormal);
            cmd.SetGlobalTexture("_RSM_Flux_"+i, rsmLights[i].rsmFlux);
        }
        cmd.SetGlobalInt("_RSMLightsCount", rsmLights.Count);
        Material mat = new Material(Shader.Find("ToyRP/lightpass"));
        mat.EnableKeyword("_BRDF");
        //mat.DisableKeyword("_BRDF");
        cmd.Blit(gbufferID[0], lightPassTex, mat);
        context.ExecuteCommandBuffer(cmd);
    }

    // 前向渲染 Pass：使用Unity内置的Unlit shader
    void ForwardPass(ScriptableRenderContext context, Camera camera, CullingResults cullingResults)
    {
        context.SetupCameraProperties(camera);
        // 设置渲染目标为lightPassTex，使用gbuffer的深度
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "forwardpass";
        
        // 设置渲染目标，但保留深度缓冲区
        cmd.SetRenderTarget(lightPassTex, gdepth);
        
        // 设置全局深度纹理
        cmd.SetGlobalTexture("_CameraDepthTexture", gdepth);
        
        context.ExecuteCommandBuffer(cmd);
        
        // 配置前向渲染设置
        ShaderTagId shaderTagId = new ShaderTagId("ForwardBase"); // 使用Unity内置的ForwardBase pass
        SortingSettings sortingSettings = new SortingSettings(camera);
        DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
        
        // 设置正确的着色器状态
        drawingSettings.enableInstancing = true;
        drawingSettings.enableDynamicBatching = true;
        drawingSettings.perObjectData = PerObjectData.None;
        
        // 添加额外的shader pass
        drawingSettings.SetShaderPassName(1, new ShaderTagId("ForwardAdd"));
        drawingSettings.SetShaderPassName(2, new ShaderTagId("PrepassBase"));
        drawingSettings.SetShaderPassName(3, new ShaderTagId("Always"));
        drawingSettings.SetShaderPassName(4, new ShaderTagId("Vertex"));
        drawingSettings.SetShaderPassName(5, new ShaderTagId("VertexLMRGBM"));
        drawingSettings.SetShaderPassName(6, new ShaderTagId("VertexLM"));
        
        // 添加对Standard Shader的支持
        drawingSettings.SetShaderPassName(7, new ShaderTagId("SRPDefaultUnlit"));
        drawingSettings.SetShaderPassName(8, new ShaderTagId("StandardMeta"));
        drawingSettings.SetShaderPassName(9, new ShaderTagId("Universal2D"));
        drawingSettings.SetShaderPassName(10, new ShaderTagId("UniversalForward"));
        drawingSettings.SetShaderPassName(11, new ShaderTagId("LightweightForward"));
        drawingSettings.SetShaderPassName(12, new ShaderTagId("Universal2D"));
        
        // 使用默认的过滤设置
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;
        
        // 绘制前向渲染对象
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        // 提交绘制命令
        context.Submit();
    }

    // 后处理和最终合成 Pass
    void FinalPass(ScriptableRenderContext context, Camera camera)
    {
        context.SetupCameraProperties(camera);
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "finalpass";

        Material mat = new Material(Shader.Find("ToyRP/finalpass"));
        
        cmd.Blit(lightPassTex, BuiltinRenderTextureType.CameraTarget, mat);
        context.ExecuteCommandBuffer(cmd);
        
        context.Submit();
    }
   

    public void SaveMainCameraSettings(ref Camera camera)
    {
        settings.position = camera.transform.position;
        settings.rotation = camera.transform.rotation;
        settings.farClipPlane = camera.farClipPlane;
        settings.nearClipPlane = camera.nearClipPlane;
        settings.aspect = camera.aspect;
        camera.orthographic = true;
    }

    // 还原相机参数, 更改为透视投影
    public void RevertMainCameraSettings(ref Camera camera)
    {
        camera.transform.position = settings.position;
        camera.transform.rotation = settings.rotation;
        camera.farClipPlane = settings.farClipPlane;
        camera.nearClipPlane = settings.nearClipPlane;
        camera.aspect = settings.aspect;
        camera.orthographic = false;
    }
    // 添加相机设置结构体
    struct CameraSettings
    {
        public Vector3 position;
        public Quaternion rotation;
        public float farClipPlane;
        public float nearClipPlane;
        public float aspect;
        public bool orthographic;
    }
    CameraSettings settings;
    // void ConfigCam(
    //   int level,Camera camera
    // ){
    //     Vector3[] nearPoints=csm.box_near[level];
    //     Vector3[] farPoints=csm.box_far[level];

    //    Vector3 center=(nearPoints[0]+nearPoints[3])/2;
    //    float distance=farPoints[0].y-nearPoints[0].y;
    //    float w=Vector3.Magnitude(nearPoints[1]-nearPoints[0]);
    //    float h=Vector3.Magnitude(nearPoints[2]-nearPoints[0]);
    //    float depth=Vector3.Magnitude(farPoints[0]-nearPoints[0]);
    //    camera.aspect=w/h;
    //    camera.orthographicSize=h/2;
    //    camera.transform.position=center;
    //    camera.transform.rotation=Quaternion.LookRotation(directionalLight.transform.forward);
    //    camera.orthographic=true;
    //    camera.nearClipPlane=0.01f;
    //    camera.farClipPlane=1.5f*depth;

    // }
    // void CSMPass(ScriptableRenderContext context, Camera camera)
    // {
    //     CommandBuffer cmd = new CommandBuffer();
    //     cmd.name = "CSMPass";
        
    //     // 添加CSMPass总体采样开始
    //     // cmd.BeginSample("CSMPass");
    //     context.ExecuteCommandBuffer(cmd);
    //     cmd.Clear();
        
    //     csm.UpdateCSM(camera, directionalLight);
    //     SaveMainCameraSettings(ref camera);
        
    //     // 使用数组存储所有shadowmap
    //     RenderTexture[] shadowMaps = new RenderTexture[] { shadowMap_1, shadowMap_2, shadowMap_3, shadowMap_4 };
        
    //     for(int i = 0; i < 4; i++)
    //     {   
    //         ConfigCam(i, camera);
    //         context.SetupCameraProperties(camera);
            
    //         // 使用对应序号的shadowmap
    //         cmd.SetRenderTarget(shadowMaps[i]);
    //         cmd.ClearRenderTarget(true, true, Color.clear);
            
    //         // 设置阴影矩阵
    //         Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
    //         Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
    //         Matrix4x4 shadowVP = projMatrix * viewMatrix;
            
    //         // 使用正确的序号设置全局变量
    //         cmd.SetGlobalMatrix("_CSMVP" + (i + 1), shadowVP);
    //         cmd.SetGlobalTexture("_CSM" + (i + 1), shadowMaps[i]);
            
    //         context.ExecuteCommandBuffer(cmd);
    //         cmd.Clear();
            
    //         camera.TryGetCullingParameters(out var cullingParams);
    //         var cullingResults = context.Cull(ref cullingParams);

    //         // 绘制阴影投射物
    //         ShaderTagId shaderTagId = new ShaderTagId("CSM");
    //         SortingSettings sortingSettings = new SortingSettings(camera);
    //         DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
    //         FilteringSettings filteringSettings = FilteringSettings.defaultValue;

    //         context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    //         context.Submit();
    //     }
        
    //     RevertMainCameraSettings(ref camera);
        
    //     // 添加CSMPass总体采样结束
    //     cmd.EndSample("CSMPass");
    //     context.ExecuteCommandBuffer(cmd);
    // }
    
    void ShadowPass(ScriptableRenderContext context, Camera camera)
    {
       

        if (directionalLight == null) return;
        
        SaveMainCameraSettings(ref camera);
        
        // 修改相机参数，设置为正交相机

        Vector3 center = directionalLight.transform.position;
        float shadowDistance = 20f;
        camera.transform.position = center - directionalLight.transform.forward * shadowDistance;
        camera.transform.rotation = Quaternion.LookRotation(directionalLight.transform.forward);
        camera.orthographic = true;
        camera.orthographicSize = 20;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = shadowDistance * 2;

        context.SetupCameraProperties(camera);

        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "ShadowPass";
        
        // 清理并设置渲染目标
        cmd.SetRenderTarget(shadowMap);
        cmd.ClearRenderTarget(true, true, Color.clear);
        
        // 设置阴影矩阵
        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
        Matrix4x4 shadowVP = projMatrix * viewMatrix;
        
     
        
        cmd.SetGlobalMatrix("_ShadowVP", shadowVP);
        cmd.SetGlobalVector("_LightDir", directionalLight.transform.forward);
        
       
        
        context.ExecuteCommandBuffer(cmd);
        
        // 剔除设置
        camera.TryGetCullingParameters(out var cullingParams);
        var cullingResults = context.Cull(ref cullingParams);

        // 绘制阴影投射物
        ShaderTagId shaderTagId = new ShaderTagId("ShadowCaster");
        SortingSettings sortingSettings = new SortingSettings(camera);
        DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        
        // 在渲染完阴影贴图后，设置它为全局纹理
        CommandBuffer shadowCmd = new CommandBuffer();
        shadowCmd.name = "SetShadowMap";
        shadowCmd.SetGlobalTexture("_ShadowMap", shadowMap);
        context.ExecuteCommandBuffer(shadowCmd);
        
        context.Submit();
        RevertMainCameraSettings(ref camera);
    }
}

