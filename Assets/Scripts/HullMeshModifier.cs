using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace JustPirate
{

// Get information here
// https://www.gamasutra.com/view/news/237528/Water_interaction_model_for_hulls_in_video_games.php#appendix

public class HullMeshModifier
{
    private Transform hullTrans;
    private Vector3[] hullVertices;
    private int[] hullTriangles;

    private Vector3[] hulVerticesGlobalPos;
    private float[] hullDistances;

    public List<TriangleData> submergedTriangles = new List<TriangleData>();

    public HullMeshModifier(GameObject hullObj)
    {
        hullTrans = hullObj.transform;
        hullVertices = hullObj.GetComponent<MeshFilter>().mesh.vertices;
        hullTriangles = hullObj.GetComponent<MeshFilter>().mesh.triangles;

        hulVerticesGlobalPos = new Vector3[hullVertices.Length];
        hullDistances = new float[hullVertices.Length];
    }
    
    public void CalculateVertexData()
    {
        submergedTriangles.Clear();

        for (int vertexIndex = 0; vertexIndex < hullVertices.Length; ++vertexIndex)
        {
            Vector3 globalPos = hullTrans.TransformPoint(hullVertices[vertexIndex]);

            hulVerticesGlobalPos[vertexIndex] = globalPos;

            hullDistances[vertexIndex] = WaterPatch.instance.DistanceToWater(globalPos, Time.time);
        }

        GenerateSubmergedTriangles();
    }

    public void GenerateSubmergedTriangles()
    {
        List<VertexData> vertexDataList = new List<VertexData>();
        vertexDataList.Add(new VertexData());
        vertexDataList.Add(new VertexData());
        vertexDataList.Add(new VertexData());

        int i = 0;
        while (i < hullTriangles.Length)
        {
            // One triangle consists of three vertices
            for (int clockwiseIndex = 0; clockwiseIndex < 3; ++clockwiseIndex)
            {
                vertexDataList[clockwiseIndex].distance = hullDistances[hullTriangles[i]];
                vertexDataList[clockwiseIndex].clockwiseIndex = clockwiseIndex;
                vertexDataList[clockwiseIndex].vertexGlobalPos = hulVerticesGlobalPos[hullTriangles[i]];
                ++i;
            }

            // Over the surface
            if (vertexDataList[0].distance > 0f && vertexDataList[1].distance > 0f && vertexDataList[2].distance > 0f)
                continue;

            // Sort to cut triangles
            vertexDataList.Sort((x, y) => x.distance.CompareTo(y.distance));
            vertexDataList.Reverse();

            // Submerged
            if (vertexDataList[0].distance < 0f && vertexDataList[1].distance < 0f && vertexDataList[2].distance < 0f)
            {
                CutSubmergedTriangleToHorizontal(vertexDataList);
            }
            else
            {
                if (vertexDataList[0].distance > 0f && vertexDataList[1].distance < 0f && vertexDataList[2].distance < 0f)
                {
                    CutTriangleOneAboveWater(vertexDataList);
                }
                //Two vertices are above the water, the other is below
                else if (vertexDataList[0].distance > 0f && vertexDataList[1].distance > 0f && vertexDataList[2].distance < 0f)
                {
                    CutTriangleTwoAboveWater(vertexDataList);
                }
            }
        }
    }

    private void CutSubmergedTriangleToHorizontal(List<VertexData> vertexDataList)
    {
        // List parameter is sorted already and count of 3
        VertexData U = vertexDataList[0];
        VertexData M = vertexDataList[1];
        VertexData L = vertexDataList[2];

        // DL is left of U
        // DR is right of U
        VertexData DL = new VertexData();
        VertexData DR = new VertexData();

        int DL_clockwiseIndex = U.clockwiseIndex - 1;
        if (DL_clockwiseIndex < 0)
            DL_clockwiseIndex = 2;

        if (vertexDataList[1].clockwiseIndex == DL_clockwiseIndex)
        {
            DL = vertexDataList[1];
            DR = vertexDataList[2];
        }
        else
        {
            DL = vertexDataList[2];
            DR = vertexDataList[1];
        }

        // Already horizontal triangle
        if (U.distance - M.distance < 0.001f)
        {
            submergedTriangles.Add(new TriangleData(U, DR, DL, false));
            return;
        }
        else if (M.distance - L.distance < 0.001f)
        {
            submergedTriangles.Add(new TriangleData(U, DR, DL, true));
            return;
        }

        // Cut triangle horizontally
        float t_cutLU = (M.distance - L.distance) / (U.distance - L.distance);
        Vector3 cutLU = L.vertexGlobalPos + (U.vertexGlobalPos - L.vertexGlobalPos) * t_cutLU;

        VertexData cutLUData = new VertexData();
        cutLUData.vertexGlobalPos = cutLU;
        cutLUData.distance = WaterPatch.instance.DistanceToWater(cutLU, Time.time);
        
        // add upside, downside triangle clockwisely
        if (L.clockwiseIndex == DL.clockwiseIndex)
        {
            submergedTriangles.Add(new TriangleData(U, M, cutLUData, true));
            submergedTriangles.Add(new TriangleData(cutLUData, M, L, false));
        }
        else
        {
            submergedTriangles.Add(new TriangleData(U, cutLUData, M, true));
            submergedTriangles.Add(new TriangleData(M, cutLUData, L, false));
        }
    }

    private void CutTriangleOneAboveWater(List<VertexData> vertexDataList)
    {
        // List parameter is sorted already and count of 3
        VertexData U = vertexDataList[0];

        // DL is left of U
        // DR is right of U
        VertexData DL = new VertexData();
        VertexData DR = new VertexData();

        int DL_clockwiseIndex = U.clockwiseIndex - 1;
        if (DL_clockwiseIndex < 0)
            DL_clockwiseIndex = 2;

        if (vertexDataList[1].clockwiseIndex == DL_clockwiseIndex)
        {
            DL = vertexDataList[1];
            DR = vertexDataList[2];
        }
        else
        {
            DL = vertexDataList[2];
            DR = vertexDataList[1];
        }

        //Point I_M
        Vector3 MU = U.vertexGlobalPos - DL.vertexGlobalPos;
        float t_M = -DL.distance / (U.distance - DL.distance);
        Vector3 MI_M = t_M * MU;
        Vector3 I_M = MI_M + DL.vertexGlobalPos;
        VertexData I_MData = new VertexData();
        I_MData.vertexGlobalPos = I_M;
        I_MData.distance = WaterPatch.instance.DistanceToWater(I_M, Time.time);
        if (I_MData.distance > 0.0001f)
            Debug.Log(I_MData.distance);

        //Point I_L
        Vector3 LU = U.vertexGlobalPos - DR.vertexGlobalPos;
        float t_L = -DR.distance / (U.distance - DR.distance);
        Vector3 LI_L = t_L * LU;
        Vector3 I_L = LI_L + DR.vertexGlobalPos;
        VertexData I_LData = new VertexData();
        I_LData.vertexGlobalPos = I_L;
        I_LData.distance = WaterPatch.instance.DistanceToWater(I_L, Time.time);
        if (I_LData.distance > 0.0001f)
            Debug.Log(I_LData.distance);

        List<VertexData> tempDataList = new List<VertexData>();
        DL.clockwiseIndex = 0;
        I_MData.clockwiseIndex = 1;
        I_LData.clockwiseIndex = 2;
        tempDataList.Add(DL);
        tempDataList.Add(I_MData);
        tempDataList.Add(I_LData);
        tempDataList.Sort((x, y) => x.distance.CompareTo(y.distance));
        tempDataList.Reverse();
        CutSubmergedTriangleToHorizontal(tempDataList);

        tempDataList.Clear();
        DL.clockwiseIndex = 0;
        I_LData.clockwiseIndex = 1;
        DR.clockwiseIndex = 2;
        tempDataList.Add(DL);
        tempDataList.Add(I_LData);
        tempDataList.Add(DR);
        tempDataList.Sort((x, y) => x.distance.CompareTo(y.distance));
        tempDataList.Reverse();
        CutSubmergedTriangleToHorizontal(tempDataList);
    }

    private void CutTriangleTwoAboveWater(List<VertexData> vertexDataList)
    {
        //H and M are above the water
        //H is after the vertice that's below water, which is L
        //So we know which one is L because it is last in the sorted list
        VertexData LData = vertexDataList[2];
        Vector3 L = vertexDataList[2].vertexGlobalPos;

        //Find the index of H
        int H_clockwiseIndex = vertexDataList[2].clockwiseIndex + 1;
        if (H_clockwiseIndex > 2)
            H_clockwiseIndex = 0;

        //We also need the heights to water
        float h_L = vertexDataList[2].distance;
        float h_H = 0f;
        float h_M = 0f;

        Vector3 H = Vector3.zero;
        Vector3 M = Vector3.zero;

        //This means that H is at position 1 in the list
        if (vertexDataList[1].clockwiseIndex == H_clockwiseIndex)
        {
            H = vertexDataList[1].vertexGlobalPos;
            M = vertexDataList[0].vertexGlobalPos;

            h_H = vertexDataList[1].distance;
            h_M = vertexDataList[0].distance;
        }
        else
        {
            H = vertexDataList[0].vertexGlobalPos;
            M = vertexDataList[1].vertexGlobalPos;

            h_H = vertexDataList[0].distance;
            h_M = vertexDataList[1].distance;
        }

        //Now we can find where to cut the triangle

        //Point J_M
        Vector3 LM = M - L;
        float t_M = -h_L / (h_M - h_L);
        Vector3 LJ_M = t_M * LM;
        Vector3 J_M = LJ_M + L;
        VertexData J_MData = new VertexData();
        J_MData.vertexGlobalPos = J_M;
        J_MData.distance = WaterPatch.instance.DistanceToWater(J_M, Time.time);

        //Point J_H
        Vector3 LH = H - L;
        float t_H = -h_L / (h_H - h_L);
        Vector3 LJ_H = t_H * LH;
        Vector3 J_H = LJ_H + L;
        VertexData J_HData = new VertexData();
        J_HData.vertexGlobalPos = J_H;
        J_HData.distance = WaterPatch.instance.DistanceToWater(J_H, Time.time);

        List<VertexData> tempDataList = new List<VertexData>();
        LData.clockwiseIndex = 0;
        J_HData.clockwiseIndex = 1;
        J_MData.clockwiseIndex = 2;
        tempDataList.Add(LData);
        tempDataList.Add(J_HData);
        tempDataList.Add(J_MData);
        tempDataList.Sort((x, y) => x.distance.CompareTo(y.distance));
        tempDataList.Reverse();
        CutSubmergedTriangleToHorizontal(tempDataList);
    }

    public void DisplayMesh(Mesh mesh, string name, List<TriangleData> trianglesData)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        //Build the mesh
        for (int i = 0; i < trianglesData.Count; i++)
        {
            //From global coordinates to local coordinates
            Vector3 p1 = hullTrans.InverseTransformPoint(trianglesData[i].v1);
            Vector3 p2 = hullTrans.InverseTransformPoint(trianglesData[i].v2);
            Vector3 p3 = hullTrans.InverseTransformPoint(trianglesData[i].v3);

            vertices.Add(p1);
            triangles.Add(vertices.Count - 1);

            vertices.Add(p2);
            triangles.Add(vertices.Count - 1);

            vertices.Add(p3);
            triangles.Add(vertices.Count - 1);
        }

        //Remove the old mesh
        mesh.Clear();

        //Give it a name
        mesh.name = name;

        //Add the new vertices and triangles
        mesh.vertices = vertices.ToArray();

        mesh.triangles = triangles.ToArray();

        mesh.RecalculateBounds();
    }
}

}