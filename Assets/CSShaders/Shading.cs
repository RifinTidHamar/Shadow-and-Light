using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shading : MonoBehaviour
{
    public ComputeShader comp;//compute shader
    public int texRes;
    public Material shadeMat;
    public Mesh mesh;
    public Transform meshTransform;
    public Texture normalMap;
    //public Texture2DArray
    RenderTexture normMapTex;//normal map texture
    RenderTexture bShadowText;//The actual shadows baked
    RenderTexture bUnlitText;//the parts of objects just not lit baked
    RenderTexture rShadowText;//The actual shadows real time
    RenderTexture rUnlitText;//the parts of objects just not lit real time
    RenderTexture RlTLightText;//real time light texture
    RenderTexture BLightText;//baked light texture
    RenderTexture finalLightText;

    Renderer rend;

    int uvToWorldHandle;
    int lightHandle;
    int blurHandle;
    int applyHandle;
    int dtID;
     
    struct MeshTriangle
    {
        public Vector3 p1WPos;
        public Vector2 p1Uv;
        public Vector3 p2WPos;
        public Vector2 p2Uv;
        public Vector3 p3WPos;
        public Vector2 p3Uv;
        public Vector3 normal;
        public Vector3 tangent;
        public Vector3 binormal;
    }

    struct CSLight
    {
        public int castShadow;
        public Vector3 loc;
        public Vector4 color;
        public float range;
        public float intensity;
    }

    struct usedUV
    {
        public float distFromShadowedObject;
        public float distFromLight;
        public Vector3 worldLoc;
        public Vector3 normal;
        public int used;
        public float lit;
    };

    ComputeBuffer triangleBuffer;
    ComputeBuffer RlTLightBuffer;
    ComputeBuffer BLightBuffer;
    ComputeBuffer usedUVBuffer;
    MeshTriangle[] triangleArr;
    CSLight[] RlTLightArr;
    CSLight[] BLightArr;
    usedUV[] usedUVsArr;
    int meshTriangleNum;
    int lightNum;
    int RlTLightNum;
    int BLightNum;
    int usedUVNum;// = texRes * texRes;
    int meshTriangleSize = sizeof(float) * 18 + sizeof(float) * 6;
    int lightSize = sizeof(int) * 1 + sizeof(float) * 9;
    int usedUVSize = sizeof(float) * 9 + sizeof(int) * 1;
    GameObject[] lightObject;
    LightData[] lightData;

    // Start is called before the first frame update
    void Start()
    {
        usedUVNum = texRes * texRes;
        finalLightText = new RenderTexture(texRes, texRes, 4);
        finalLightText.enableRandomWrite = true;
        finalLightText.filterMode = FilterMode.Point;
        finalLightText.Create();

        RlTLightText = new RenderTexture(texRes, texRes, 4);
        RlTLightText.enableRandomWrite = true;
        RlTLightText.filterMode = FilterMode.Point;
        RlTLightText.Create();

        bShadowText = new RenderTexture(texRes, texRes, 4);
        bShadowText.enableRandomWrite = true;
        bShadowText.filterMode = FilterMode.Point;
        bShadowText.Create();

        bUnlitText = new RenderTexture(texRes, texRes, 4);
        bUnlitText.enableRandomWrite = true;
        bUnlitText.filterMode = FilterMode.Point;
        bUnlitText.Create();

        rShadowText = new RenderTexture(texRes, texRes, 4);
        rShadowText.enableRandomWrite = true;
        rShadowText.filterMode = FilterMode.Point;
        rShadowText.Create();

        rUnlitText = new RenderTexture(texRes, texRes, 4);
        rUnlitText.enableRandomWrite = true;
        rUnlitText.filterMode = FilterMode.Point;
        rUnlitText.Create();

        normMapTex = new RenderTexture(texRes, texRes, 4);
        normMapTex.enableRandomWrite = true;
        normMapTex.filterMode = FilterMode.Point;
        normMapTex.Create();

        BLightText = new RenderTexture(texRes, texRes, 4);
        BLightText.enableRandomWrite = true;
        BLightText.filterMode = FilterMode.Point;
        BLightText.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        uvToWorldHandle = comp.FindKernel("uvToWorld");
        lightHandle = comp.FindKernel("DynamicLight");
        blurHandle = comp.FindKernel("Blur");
        applyHandle = comp.FindKernel("Apply");

        populateArray();
        initShader();
    }

    void populateArray()
    {
        //change this so lights register themselves with a list
        lightObject = GameObject.FindGameObjectsWithTag("Light");
        lightNum = lightObject.Length;
        lightData = new LightData[lightNum];

        for (int i = 0; i < lightNum; i ++)
        {
            lightData[i] = lightObject[i].gameObject.GetComponent<LightData>();
            if (lightData[i].baked == true)
                BLightNum++;
            else
                RlTLightNum++;
        }

        RlTLightArr = new CSLight[RlTLightNum];
        BLightArr = new CSLight[BLightNum];

        Vector3[] worldVerts = new Vector3[mesh.vertices.Length];
        for(int i = 0; i < mesh.vertices.Length; i++)
        {
            //worldVerts[i] = new Vector4(mesh.vertices[i].x, mesh.vertices[i].y, mesh.vertices[i].z, 1);
            worldVerts[i] = meshTransform.localToWorldMatrix.MultiplyVector(mesh.vertices[i]);
        }
        meshTriangleNum = mesh.triangles.Length/3;
        //mesh.triangles[0];
        //Debug.Log(meshTriangleNum);
        triangleArr = new MeshTriangle[meshTriangleNum];
        int[] vertIndices = mesh.triangles;
        /*for(int i = 0; i < vertIndices.Length; i++)
        {
            Debug.Log(vertIndices[i]);
        }*/
        //Debug.Log(vertIndices.Length / 3);
        //Debug.Log(mesh.uv.Length);
        for (int i = 0; i < meshTriangleNum; i++)
        {
            int vCount = i * 3;
            Vector3 v1 = worldVerts[vertIndices[vCount + 0]];
            Vector3 v2 = worldVerts[vertIndices[vCount + 1]];
            Vector3 v3 = worldVerts[vertIndices[vCount + 2]];
            triangleArr[i].p1WPos = v1;
            triangleArr[i].p2WPos = v2;
            triangleArr[i].p3WPos = v3;

            triangleArr[i].p1Uv = mesh.uv[vertIndices[vCount + 0]];
            triangleArr[i].p2Uv = mesh.uv[vertIndices[vCount + 1]];
            triangleArr[i].p3Uv = mesh.uv[vertIndices[vCount + 2]];

            triangleArr[i].normal = mesh.normals[vertIndices[vCount + 0]];
            //triangleArr[i].normal = new Vector3(-triangleArr[i].normal.x, triangleArr[i].normal.y, triangleArr[i].normal.z);

            triangleArr[i].tangent = mesh.tangents[vertIndices[vCount + 0]];

            triangleArr[i].binormal = Vector3.Cross(triangleArr[i].normal, triangleArr[i].tangent);
            
            //Debug.Log(triangleArr[i].p1Uv + " " + triangleArr[i].p2Uv + " " + triangleArr[i].p3Uv);
        }


        usedUVsArr = new usedUV[usedUVNum];
        for(int i = 0; i < usedUVNum; i++)
        {
            usedUVsArr[i].distFromShadowedObject = 0;
            usedUVsArr[i].distFromLight = 0;
            usedUVsArr[i].worldLoc = new Vector3(0, 0, 0);
            //usedUVsArr[i].uvPos = new Vector2(0, 0);
            usedUVsArr[i].normal = new Vector3(0, 0, 0);
            usedUVsArr[i].used = 0;
            usedUVsArr[i].lit = 1;
        }
    }

    /*Vector3 shaveOffEndPoint(Vector4 x)
    {
        Vector3 ret = new Vector3(x.x, x.y, x.z);
        return ret;
    }*/

    void initShader()
    {
        //init
        comp.SetInt("numTriangles", meshTriangleNum);
        comp.SetInt("texRes", texRes);

        //comp.SetFloat(dtID, 0);
        triangleBuffer = new ComputeBuffer(meshTriangleNum, meshTriangleSize);
        if(RlTLightNum != 0)
            RlTLightBuffer = new ComputeBuffer(RlTLightNum, lightSize);
        if(BLightNum != 0)
            BLightBuffer = new ComputeBuffer(BLightNum, lightSize);
        usedUVBuffer = new ComputeBuffer(usedUVNum, usedUVSize);
        ComputeBuffer lightNumBuff = new ComputeBuffer(1, sizeof(int));

        triangleBuffer.SetData(triangleArr);
        usedUVBuffer.SetData(usedUVsArr);
        Graphics.Blit(normalMap, normMapTex);
        comp.SetTexture(uvToWorldHandle, "nm", normMapTex);
        comp.SetBuffer(uvToWorldHandle, "triangles", triangleBuffer);
        comp.SetBuffer(uvToWorldHandle, "usedUVs", usedUVBuffer);
        comp.Dispatch(uvToWorldHandle, texRes / 8, texRes / 8, 1);
        //init

        //frag
        shadeMat.SetTexture("_ShadowTex", finalLightText);
        //frag

        //doesn't change
        comp.SetBuffer(lightHandle, "triangles", triangleBuffer);
        comp.SetBuffer(lightHandle, "usedUVs", usedUVBuffer);
        comp.SetBuffer(blurHandle, "usedUVs", usedUVBuffer);
        //doesn't change

        //bLight
        if (BLightNum != 0)
        {
            lightNumBuff.SetData(new int[] { BLightNum });
            comp.SetBuffer(lightHandle, "numLights", lightNumBuff);

            int BLightInd = 0;
            for (int i = 0; i < lightNum; i++)
            {
                if (lightData[i].baked == true)
                {
                    BLightArr[BLightInd].castShadow = lightData[i].castShadow ? 1 : 0;
                    BLightArr[BLightInd].loc = lightObject[i].gameObject.transform.position;
                    BLightArr[BLightInd].color = lightData[i].color;
                    BLightArr[BLightInd].range = lightData[i].range;
                    BLightArr[BLightInd].intensity = lightData[i].intensity;
                    BLightInd++;
                }
            }

            BLightBuffer.SetData(BLightArr);
            comp.SetBuffer(lightHandle, "lights", BLightBuffer);
            comp.SetTexture(lightHandle, "shad", bShadowText);
            comp.SetTexture(lightHandle, "unlit", bUnlitText);

            comp.Dispatch(lightHandle, texRes / 8, texRes / 8, 1);

            comp.SetBuffer(blurHandle, "lights", BLightBuffer);
            comp.SetTexture(blurHandle, "unlit", bUnlitText);
            comp.SetTexture(blurHandle, "shad", bShadowText);
            comp.SetTexture(blurHandle, "totalResult", BLightText);

            comp.Dispatch(blurHandle, texRes / 8, texRes / 8, 1);
        }

        //blight

        //RlTLight
        if (RlTLightNum != 0)
        {
            lightNumBuff.SetData(new int[] { RlTLightNum });
            comp.SetBuffer(lightHandle, "numLights", lightNumBuff);
            comp.SetTexture(lightHandle, "shad", rShadowText);
            comp.SetTexture(lightHandle, "unlit", rUnlitText);
            comp.SetBuffer(lightHandle, "lights", RlTLightBuffer);

            comp.SetBuffer(blurHandle, "lights", RlTLightBuffer);
            comp.SetTexture(blurHandle, "unlit", rUnlitText);
            comp.SetTexture(blurHandle, "shad", rShadowText);
            comp.SetTexture(blurHandle, "totalResult", RlTLightText);
        }
        //RlTLight

        //finalCombine
        comp.SetTexture(applyHandle, "RlTLight", RlTLightText);
        comp.SetTexture(applyHandle, "BLight", BLightText);
        comp.SetTexture(applyHandle, "totalResult", finalLightText);

        if(RlTLightNum == 0)
            comp.Dispatch(applyHandle, texRes / 8, texRes / 8, 1);//apply for baked light, incase there is no real time light
    }

    // Update is called once per frame
    void Update()
    {
        if (RlTLightNum != 0)
        {
            int RlTLightInd = 0;
            for (int i = 0; i < lightNum; i++)
            {
                if (lightData[i].baked == false)
                {
                    RlTLightArr[RlTLightInd].castShadow = lightData[i].castShadow ? 1 : 0;
                    RlTLightArr[RlTLightInd].loc = lightObject[i].gameObject.transform.position;
                    RlTLightArr[RlTLightInd].color = lightData[i].color;
                    RlTLightArr[RlTLightInd].range = lightData[i].range;
                    RlTLightArr[RlTLightInd].intensity = lightData[i].intensity;
                    RlTLightArr[RlTLightInd].castShadow = lightData[i].castShadow ? 1 : 0;
                    RlTLightInd++;
                }
            }
            RlTLightBuffer.SetData(RlTLightArr);

            comp.Dispatch(lightHandle, texRes / 8, texRes / 8, 1);

        }
        comp.Dispatch(blurHandle, texRes / 8, texRes / 8, 1);
        comp.Dispatch(applyHandle, texRes / 8, texRes / 8, 1);


        //uint x;
        //uint y;
        //uint z;
        //comp.GetKernelThreadGroupSizes(RlTLightHandle, out x, out y, out z);
        //Debug.Log(x + " " + y + " " + z);
        //Debug.Log(lightNum);
    }

    private void OnApplicationQuit()
    {
        triangleBuffer.Release();
        if (RlTLightNum != 0) RlTLightBuffer.Release();
        if (BLightNum != 0) BLightBuffer.Release();
        usedUVBuffer.Release();
    }
}
