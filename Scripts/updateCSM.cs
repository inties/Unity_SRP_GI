using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 这个脚本挂相机上测试
//[ExecuteAlways]
public class ShadowCameraDebug : MonoBehaviour
{
    CSM csm;
    //获取摄像机组件
    Camera cam;

    void OnEnable()
    {
        // 在启用时获取摄像机组件
        cam = GetComponent<Camera>();
       
    }

    void Update()
    {
        if(cam == null)
        {
            cam = GetComponent<Camera>();
            if(cam == null)
            {
                return;
            }
        }
        
        // 获取光源信息
        Light light = RenderSettings.sun;

        // 更新 shadowmap
        if(csm == null) csm = new CSM();
        csm.UpdateCSM(cam, light);
    }
}
