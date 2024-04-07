using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shading : MonoBehaviour
{
    public ComputeShader comp;
    public int texRes;
    public Material shadeMat;
    public Mesh mesh;
    public Transform meshTransform;
    public Texture normalMap;
    //public Texture2DArray
    RenderTexture normMapTex;
    RenderTexture shadowText;
    RenderTexture lighCastText;
    RenderTexture ResultText;

    Renderer rend;

    int initHandle;
    int lightHandel;
    int shadowHandle;
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
        public Vector3 loc;
        public Vector4 color;
        public float range;
        public float intensity;
    }

    struct usedUV
    {
        public Vector2 uvPos;
        public Vector3 worldLoc;
        public Vector3 normal;
        public int used;
        public float lit;
    };

    ComputeBuffer triangleBuffer;
    ComputeBuffer lightBuffer;
    ComputeBuffer usedUVBuffer;
    MeshTriangle[] triangleArr;
    CSLight[] lightArr;
    usedUV[] usedUVsArr;
    int meshTriangleNum;
    int CSlightNum;
    int usedUVNum;// = texRes * texRes;
    int meshTriangleSize = sizeof(float) * 18 + sizeof(float) * 6;
    int CSLightSize = sizeof(float) * 9;
    int usedUVSize = sizeof(float) * 9 + sizeof(int) * 1;
    GameObject[] lightObject;
    LightData[] lightData;

    // Start is called before the first frame update
    void Start()
    {
        usedUVNum = texRes * texRes;
        ResultText = new RenderTexture(texRes, texRes, 4);
        ResultText.enableRandomWrite = true;
        ResultText.filterMode = FilterMode.Point;
        ResultText.Create();

        lighCastText = new RenderTexture(texRes, texRes, 4);
        lighCastText.enableRandomWrite = true;
        lighCastText.filterMode = FilterMode.Point;
        lighCastText.Create();

        shadowText = new RenderTexture(texRes, texRes, 4);
        shadowText.enableRandomWrite = true;
        shadowText.filterMode = FilterMode.Point;
        shadowText.Create();

        normMapTex = new RenderTexture(texRes, texRes, 4);
        normMapTex.enableRandomWrite = true;
        normMapTex.filterMode = FilterMode.Point;
        normMapTex.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        rend = GetComponent<Renderer>();
        rend.enabled = true;
        initHandle = comp.FindKernel("CSMain");
        lightHandel = comp.FindKernel("DynamicLight");
        shadowHandle = comp.FindKernel("BlurAndApplyShadow");

        populateArray();
        initShader();
    }

    void populateArray()
    {
        lightObject = GameObject.FindGameObjectsWithTag("Light");
        CSlightNum = lightObject.Length;
        lightArr = new CSLight[CSlightNum];
        lightData = new LightData[CSlightNum];
        //TODO: make values changebale in editor

        for (int i = 0; i < CSlightNum; i++)
        {
            lightData[i] = lightObject[i].gameObject.GetComponent<LightData>();

            lightArr[i].loc = lightObject[i].gameObject.transform.position;
            lightArr[i].color = lightData[i].color;
            lightArr[i].range = lightData[i].range;
            lightArr[i].intensity = lightData[i].intensity;
        }

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
            usedUVsArr[i].worldLoc = new Vector3(0, 0, 0);
            usedUVsArr[i].uvPos = new Vector2(0, 0);
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
        //comp.SetFloat(dtID, 0);
        triangleBuffer = new ComputeBuffer(meshTriangleNum, meshTriangleSize);
        lightBuffer = new ComputeBuffer(CSlightNum, CSLightSize);
        usedUVBuffer = new ComputeBuffer(usedUVNum, usedUVSize);
        triangleBuffer.SetData(triangleArr);
        lightBuffer.SetData(lightArr);
        usedUVBuffer.SetData(usedUVsArr);
        Graphics.Blit(normalMap, normMapTex);
        comp.SetTexture(initHandle, "nm", normMapTex);

        comp.SetBuffer(initHandle, "triangles", triangleBuffer);
        comp.SetBuffer(initHandle, "lights", lightBuffer);
        comp.SetBuffer(initHandle, "usedUVs", usedUVBuffer);

        comp.SetBuffer(lightHandel, "triangles", triangleBuffer);
        comp.SetBuffer(lightHandel, "usedUVs", usedUVBuffer);


        comp.SetInt("numLights", CSlightNum);
        comp.SetInt("numTriangles", meshTriangleNum);
        comp.SetInt("texRes", texRes);

        comp.Dispatch(initHandle, texRes / 8, texRes / 8, 1);

        //dtID = Shader.PropertyToID("dt");
        //comp.SetVector("stretch", new Vector2(2, 2));
        //comp.SetInt("texRes", texRes);
        shadeMat.SetTexture("_ShadowTex", lighCastText);

        for (int i = 0; i < CSlightNum; i++)
        {
            lightArr[i].loc = lightObject[i].gameObject.transform.position;
            lightArr[i].color = lightData[i].color;
            lightArr[i].range = lightData[i].range;
            lightArr[i].intensity = lightData[i].intensity;
        }
        lightBuffer.SetData(lightArr);

        comp.SetTexture(lightHandel, "light", lighCastText);
        comp.SetBuffer(lightHandel, "lights", lightBuffer);
        comp.Dispatch(lightHandel, texRes / 8, texRes / 8, 1);
        uint x;
        uint y;
        uint z;
        comp.GetKernelThreadGroupSizes(lightHandel, out x, out y, out z);
        Debug.Log(x + " " + y + " " + z);
        Debug.Log(CSlightNum);
    }


    // Update is called once per frame
    void Update()
    {
        //for (int i = 0; i < CSlightNum; i++)
        //{
        //    lightArr[i].loc = lightObject[i].gameObject.transform.position;
        //    lightArr[i].color = lightData[i].color;
        //    lightArr[i].range = lightData[i].range;
        //    lightArr[i].intensity = lightData[i].intensity;
        //}
        //lightBuffer.SetData(lightArr);

        //comp.SetTexture(lightHandel, "light", lighCastText);
        //comp.SetTexture(lightHandel, "shad", shadowText);
        //comp.SetBuffer(lightHandel, "lights", lightBuffer);
        //comp.Dispatch(lightHandel, texRes / 8, texRes / 8, 1);

        //comp.SetTexture(shadowHandle, "light", lighCastText);
        //comp.SetTexture(shadowHandle, "shad", shadowText);
        //comp.SetTexture(shadowHandle, "Result", ResultText);
        //comp.Dispatch(shadowHandle, texRes / 8, texRes / 8, 1);

        //uint x;
        //uint y;
        //uint z;
        //comp.GetKernelThreadGroupSizes(lightHandel, out x, out y, out z);
        //Debug.Log(x + " " + y + " " + z);
        //Debug.Log(CSlightNum);
    }

    private void OnApplicationQuit()
    {
        triangleBuffer.Release();
        lightBuffer.Release();
        usedUVBuffer.Release();
    }
}