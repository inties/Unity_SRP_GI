using UnityEngine;
public class CSM
{
    public float[] splitPoints = {0f, 0.1f, 0.25f, 0.5f, 1.0f};
    
    // 子视锥体近平面顶点
    public Vector3[] nearPoints = new Vector3[4];
    
    // CSM包围盒顶点
    public Vector3[] near_0 = new Vector3[4];
    public Vector3[] far_0 = new Vector3[4];
    public Vector3[] near_1 = new Vector3[4];
    public Vector3[] far_1 = new Vector3[4];
    public Vector3[] near_2 = new Vector3[4];
    public Vector3[] far_2 = new Vector3[4];
    public Vector3[] near_3 = new Vector3[4];
    public Vector3[] far_3 = new Vector3[4];
    
    public Vector3[][] box_near;
    public Vector3[][] box_far;

    public CSM()
    {
        // 在构造函数中初始化数组
        box_near = new Vector3[][] { near_0, near_1, near_2, near_3 };
        box_far = new Vector3[][] { far_0, far_1, far_2, far_3 };
    }

    public void UpdateCSM(Camera camera,Light directionalLight){
        //更新子视锥体近平面顶点（世界坐标空间）
        updateNearPoints(camera);
        drawNearPoints(Color.red);
        Vector3[] PointsDir=new Vector3[4];
        for(int i=0;i<4;i++){
            PointsDir[i]=nearPoints[i]-camera.transform.position;
        }
        float distance=camera.farClipPlane-camera.nearClipPlane;
        for(int i=0;i<4;i++){
            Vector3[] nearPos=new Vector3[4];
            Vector3[] farPos=new Vector3[4];
            float ratio_near=splitPoints[i]*distance/camera.nearClipPlane;
            float ratio_far=splitPoints[i+1]*distance/camera.nearClipPlane;
            for(int j=0;j<4;j++){
                nearPos[j]=nearPoints[j]+PointsDir[j]*ratio_near;
                farPos[j]=nearPoints[j]+PointsDir[j]*ratio_far;
            }
            drawFrustum(nearPos,farPos,Color.red);
            //更新包围盒顶点
            switch (i)
            {
                case 0:
                    updateBox(nearPos, farPos, ref near_0, ref far_0,directionalLight);
                    break;
                case 1:
                    updateBox(nearPos, farPos, ref near_1, ref far_1,directionalLight);
                    break;
                case 2:
                    updateBox(nearPos, farPos, ref near_2, ref far_2,directionalLight);
                    break;
                case 3:
                    updateBox(nearPos, farPos, ref near_3, ref far_3,directionalLight);
                    break;
            }
        }
        
         drawBox(near_0,far_0,Color.blue);
        // drawBox(near_1,far_1,Color.green);
        // drawBox(near_2,far_2,Color.yellow);
        drawBox(near_3,far_3,Color.cyan);
    }
    private void updateNearPoints(Camera camera){
        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, nearPoints);
        for (int i = 0; i < 4; i++)
    {
       
        nearPoints[i] = camera.transform.TransformVector(nearPoints[i]) + camera.transform.position;
    }
        // //计算子视锥体近平面顶点
        // Vector3 centor=camera.transform.position+camera.transform.forward*camera.nearClipPlane;
        // float halfFov=camera.fieldOfView*0.5f*Mathf.Deg2Rad;
        // float halfHeight=camera.nearClipPlane*Mathf.Tan(halfFov);
        // float halfWidth=halfHeight*camera.aspect;
        // nearPoints[0]=centor+new Vector3(-halfWidth,halfHeight,0);
        // nearPoints[1]=centor+new Vector3(halfWidth,halfHeight,0);
        // nearPoints[2]=centor+new Vector3(halfWidth,-halfHeight,0);
        // nearPoints[3]=centor+new Vector3(-halfWidth,-halfHeight,0);
    }
    //计算包围盒顶点
    private void updateBox(Vector3[] nearPoints, Vector3[] farPoints, ref Vector3[] nearBox, ref Vector3[] farBox,Light directionalLight)
    {
        Matrix4x4 toShadowViewInv = Matrix4x4.LookAt(Vector3.zero, directionalLight.transform.forward, Vector3.up);
        Matrix4x4 toShadowView = toShadowViewInv.inverse;
        for(int i=0;i<4;i++){
            nearPoints[i]=matTransform(toShadowView,nearPoints[i]);
            farPoints[i]=matTransform(toShadowView,farPoints[i]);
        }
        //求包围盒顶点
        Vector3 minPoint,maxPoint;
        minPoint=maxPoint=nearPoints[0];
        for(int i=0;i<4;i++){
            minPoint.x=Mathf.Min(minPoint.x,nearPoints[i].x);
            minPoint.y=Mathf.Min(minPoint.y,nearPoints[i].y);
            minPoint.z=Mathf.Min(minPoint.z,nearPoints[i].z);
            maxPoint.x=Mathf.Max(maxPoint.x,nearPoints[i].x);
            maxPoint.y=Mathf.Max(maxPoint.y,nearPoints[i].y);
            maxPoint.z=Mathf.Max(maxPoint.z,nearPoints[i].z);
            minPoint.x=Mathf.Min(minPoint.x,farPoints[i].x);
            minPoint.y=Mathf.Min(minPoint.y,farPoints[i].y);
            minPoint.z=Mathf.Min(minPoint.z,farPoints[i].z);
            maxPoint.x=Mathf.Max(maxPoint.x,farPoints[i].x);
            maxPoint.y=Mathf.Max(maxPoint.y,farPoints[i].y);
            maxPoint.z=Mathf.Max(maxPoint.z,farPoints[i].z);
        }
        nearBox[0]=new Vector3(minPoint.x,minPoint.y,minPoint.z);
        nearBox[1]=new Vector3(maxPoint.x,minPoint.y,minPoint.z);
        nearBox[2]=new Vector3(minPoint.x,maxPoint.y,minPoint.z);
        nearBox[3]=new Vector3(maxPoint.x,maxPoint.y,minPoint.z);
        farBox[0]=new Vector3(minPoint.x,minPoint.y,maxPoint.z);
        farBox[1]=new Vector3(maxPoint.x,minPoint.y,maxPoint.z);
        farBox[2]=new Vector3(minPoint.x,maxPoint.y,maxPoint.z);
        farBox[3]=new Vector3(maxPoint.x,maxPoint.y,maxPoint.z);
         for(int i=0;i<4;i++){
            nearPoints[i]=matTransform(toShadowViewInv,nearPoints[i]);
            farPoints[i]=matTransform(toShadowViewInv,farPoints[i]);
        }
        for(int i=0;i<4;i++){
            nearBox[i]=matTransform(toShadowViewInv,nearBox[i]);
            farBox[i]=matTransform(toShadowViewInv,farBox[i]);
        }
    }
    private Vector3 matTransform(Matrix4x4 mat,Vector3 points){
        Vector4 result=mat*new Vector4(points.x,points.y,points.z,1);
        //返回Vector3
        return new Vector3(result.x/result.w,result.y/result.w,result.z/result.w);
    }
    private void drawBox(Vector3[] nearBox,Vector3[] farBox,Color color){
        Debug.DrawLine(nearBox[0],nearBox[1],color);
        Debug.DrawLine(nearBox[1],nearBox[3],color);
        Debug.DrawLine(nearBox[3],nearBox[2],color);
        Debug.DrawLine(nearBox[2],nearBox[0],color);
        Debug.DrawLine(nearBox[0],farBox[0],color);
        Debug.DrawLine(nearBox[1],farBox[1],color);
        Debug.DrawLine(nearBox[2],farBox[2],color);
        Debug.DrawLine(nearBox[3],farBox[3],color);
        Debug.DrawLine(farBox[0],farBox[1],color);
        Debug.DrawLine(farBox[1],farBox[3],color);
        Debug.DrawLine(farBox[3],farBox[2],color);
        Debug.DrawLine(farBox[2],farBox[0],color);
    }
    private void drawNearPoints(Color color){
        Debug.DrawLine(nearPoints[0],nearPoints[1],color);
        Debug.DrawLine(nearPoints[1],nearPoints[2],color);
        Debug.DrawLine(nearPoints[2],nearPoints[3],color);
        Debug.DrawLine(nearPoints[3],nearPoints[0],color);
    }
    private void drawFrustum(Vector3[] nearPoints,Vector3[] farPoints,Color color){
        Debug.DrawLine(nearPoints[0],nearPoints[1],color);
        Debug.DrawLine(nearPoints[1],nearPoints[2],color);
        Debug.DrawLine(nearPoints[2],nearPoints[3],color);
        Debug.DrawLine(nearPoints[3],nearPoints[0],color);
        Debug.DrawLine(nearPoints[0],farPoints[0],color);
        Debug.DrawLine(nearPoints[1],farPoints[1],color);
        Debug.DrawLine(nearPoints[2],farPoints[2],color);
        Debug.DrawLine(nearPoints[3],farPoints[3],color);
        Debug.DrawLine(farPoints[0],farPoints[1],color);
        Debug.DrawLine(farPoints[1],farPoints[2],color);
        Debug.DrawLine(farPoints[2],farPoints[3],color);
        Debug.DrawLine(farPoints[3],farPoints[0],color);
    }
}

