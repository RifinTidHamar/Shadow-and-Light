using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UVFrag : MonoBehaviour
{
    private ComputeShader comp;
    private Mesh mesh;
    private Transform meshTransform;
    private int texRes;

    int shadingHandel;
    int lightHandel;
    int dtID;

    public UVFrag(ComputeShader c, Mesh m, Transform mt, int textureResolution)
    {
        comp = c;
        mesh = m;
        meshTransform = mt;
        texRes = textureResolution;
    }

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
    usedUV[] usedUVsArr;
    int meshTriangleNum;
    int usedUVNum;// = texRes * texRes;
    int meshTriangleSize = sizeof(float) * 18 + sizeof(float) * 6;
    int usedUVSize = sizeof(float) * 9 + sizeof(int) * 1;

    // Start is called before the first frame update
    void Start()
    {
        usedUVNum = texRes * texRes;
       
        shadingHandel = comp.FindKernel("CSMain");
        lightHandel = comp.FindKernel("DynamicLight");

        populateArray();
        initShader();
    }

    void populateArray()
    {

        Vector3[] worldVerts = new Vector3[mesh.vertices.Length];
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            //worldVerts[i] = new Vector4(mesh.vertices[i].x, mesh.vertices[i].y, mesh.vertices[i].z, 1);
            worldVerts[i] = meshTransform.localToWorldMatrix.MultiplyVector(mesh.vertices[i]);
        }
        meshTriangleNum = mesh.triangles.Length / 3;
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

            triangleArr[i].tangent = mesh.tangents[vertIndices[vCount + 0]];

            triangleArr[i].binormal = Vector3.Cross(triangleArr[i].normal, triangleArr[i].tangent);

            //Debug.Log(triangleArr[i].p1Uv + " " + triangleArr[i].p2Uv + " " + triangleArr[i].p3Uv);
        }


        usedUVsArr = new usedUV[usedUVNum];
        for (int i = 0; i < usedUVNum; i++)
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
        usedUVBuffer = new ComputeBuffer(usedUVNum, usedUVSize);
        triangleBuffer.SetData(triangleArr);
        usedUVBuffer.SetData(usedUVsArr);

        comp.SetBuffer(shadingHandel, "triangles", triangleBuffer);
        comp.SetBuffer(shadingHandel, "lights", lightBuffer);
        comp.SetBuffer(shadingHandel, "usedUVs", usedUVBuffer);

        comp.SetBuffer(lightHandel, "usedUVs", usedUVBuffer);


        comp.SetInt("numTriangles", meshTriangleNum);
        comp.SetInt("texRes", texRes);

        comp.Dispatch(shadingHandel, texRes / 8, texRes / 8, 1);

        //dtID = Shader.PropertyToID("dt");
        //comp.SetVector("stretch", new Vector2(2, 2));
        //comp.SetInt("texRes", texRes);
    }


    private void OnApplicationQuit()
    {
        triangleBuffer.Release();
        lightBuffer.Release();
        usedUVBuffer.Release();
    }
}
