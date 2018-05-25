using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// Get information here
// https://www.gamasutra.com/view/news/237528/Water_interaction_model_for_boats_in_video_games.php
// https://www.gamasutra.com/view/news/263237/Water_interaction_model_for_boats_in_video_games_Part_2.php

namespace JustPirate
{
    public enum TriangleState
    {
        AboveWater,
        TwoAboveWater,
        OneAboveWater,
        Submerged
    };

    public class TriangleBuilder : MonoBehaviour
    {
        public float epsilon = 0.00001f;
        public List<TriangleInfo> SubmergedTriangles { get; set; }
        public List<int> TriangleBufferIndices { get; set; }
        public TriangleBuffer[] PreviousBuffers { get; set; }
        public TriangleBuffer[] CurrentBuffers { get; set; }
        public float TotalArea { get; set; }

        private Vector3[] meshVertices;
        private int[] meshTriangles;
        private float timeStamp;
        private VertexInfo[] currentVertices = new VertexInfo[3];

        private Mesh mesh;
        protected Mesh Mesh
        {
            get
            {
                if (mesh == null)
                    mesh = GetComponent<MeshFilter>().mesh;
                return mesh;
            }
        }
        private Rigidbody rigidbody3d;
        protected Rigidbody Rigidbody3d
        {
            get
            {
                if (rigidbody3d == null)
                    rigidbody3d = GetComponent<Rigidbody>();
                return rigidbody3d;
            }
        }

        private void OnEnable()
        {
            timeStamp = Time.time;
            SubmergedTriangles = new List<TriangleInfo>();
            TriangleBufferIndices = new List<int>();

            meshVertices = Mesh.vertices;
            meshTriangles = Mesh.triangles;

            // Count of triangles is always multiple of 3
            int triangleCount = meshTriangles.Length / 3;
            PreviousBuffers = new TriangleBuffer[triangleCount];
            CurrentBuffers = new TriangleBuffer[triangleCount];

            TotalArea = 0f;
            for (int triangleIndex = 0; triangleIndex < triangleCount; ++triangleIndex)
            {
                TotalArea += WaterInteractionUtils.GetTriangleArea(
                    transform.TransformPoint(meshVertices[meshTriangles[triangleIndex * 3]]),
                    transform.TransformPoint(meshVertices[meshTriangles[triangleIndex * 3 + 1]]),
                    transform.TransformPoint(meshVertices[meshTriangles[triangleIndex * 3 + 2]]));
            }
        }

        private void Initialize()
        {
            timeStamp = Time.time;

            SubmergedTriangles.Clear();
            TriangleBufferIndices.Clear();
        }

        private void SetSortedTriangleVertices(int triangleIndex)
        {
            for (int i = 0; i < 3; ++i)
            {
                currentVertices[i].clockwiseOrder = i;
                Vector3 globalVertex = transform.TransformPoint(meshVertices[meshTriangles[triangleIndex * 3 + i]]);
                currentVertices[i].globalVertex = globalVertex;
                currentVertices[i].height = WaterPatch.instance.DistanceToWater(globalVertex, timeStamp);
            }
            Array.Sort(currentVertices);
        }

        private TriangleState SetTriangleState()
        {
            if (currentVertices[0].height > 0f && currentVertices[1].height > 0f && currentVertices[2].height > 0f)
                return TriangleState.AboveWater;
            else if (currentVertices[0].height > 0f && currentVertices[1].height > 0f && currentVertices[2].height <= 0f)
                return TriangleState.TwoAboveWater;
            else if (currentVertices[0].height > 0f && currentVertices[1].height <= 0f && currentVertices[2].height <= 0f)
                return TriangleState.OneAboveWater;
            else
                return TriangleState.Submerged;
        }

        public void CreateTriangleData()
        {
            Initialize();

            for (int triangleIndex = 0; triangleIndex < meshTriangles.Length / 3; ++triangleIndex)
            {
                SetSortedTriangleVertices(triangleIndex);
                PreviousBuffers[triangleIndex] = CurrentBuffers[triangleIndex];
                CurrentBuffers[triangleIndex] = new TriangleBuffer(currentVertices[0].globalVertex, currentVertices[1].globalVertex, currentVertices[2].globalVertex, Rigidbody3d);

                TriangleState triangleState = SetTriangleState();
                switch (triangleState)
                {
                    case TriangleState.AboveWater:
                        CurrentBuffers[triangleIndex].submergedArea = 0f;
                        break;

                    case TriangleState.TwoAboveWater:
                        CurrentBuffers[triangleIndex].submergedArea = CuttingAlgorithmTwoAbove(currentVertices[0], currentVertices[1], currentVertices[2], triangleIndex);
                        break;

                    case TriangleState.OneAboveWater:
                        CurrentBuffers[triangleIndex].submergedArea = CuttingAlgorithmOneAbove(currentVertices[0], currentVertices[1], currentVertices[2], triangleIndex);
                        break;

                    case TriangleState.Submerged:
                        CuttingAlgorithmHorizontal(currentVertices[0], currentVertices[1], currentVertices[2], triangleIndex);
                        CurrentBuffers[triangleIndex].submergedArea = CurrentBuffers[triangleIndex].originalArea;
                        break;

                    default:
                        break;
                }
            }
        }

        private float CuttingAlgorithmTwoAbove(VertexInfo top, VertexInfo mid, VertexInfo bottom, int triangleIndex)
        {
            float tPointCutTB = - bottom.height / (top.height - bottom.height);
            Vector3 pointCutTB = bottom.globalVertex + (top.globalVertex - bottom.globalVertex) * tPointCutTB;
            VertexInfo cutTB = new VertexInfo
            {
                clockwiseOrder = -1,
                globalVertex = pointCutTB,
                height = WaterPatch.instance.DistanceToWater(pointCutTB, timeStamp),
            };

            float tPointCutMB = - bottom.height / (mid.height - bottom.height);
            Vector3 PointCutMB = bottom.globalVertex + (mid.globalVertex - bottom.globalVertex) * tPointCutMB;
            VertexInfo cutMB = new VertexInfo
            {
                clockwiseOrder = -1,
                globalVertex = PointCutMB,
                height = WaterPatch.instance.DistanceToWater(PointCutMB, timeStamp),
            };

            if ((top.clockwiseOrder + 1) % 3 == mid.clockwiseOrder)
            {
                // downLeft: bottom, downRight: mid
                cutTB.clockwiseOrder = (bottom.clockwiseOrder + 1) % 3;
                cutMB.clockwiseOrder = (cutTB.clockwiseOrder + 1) % 3;
            }
            else
            {
                // downLeft: mid, downRight: bottom
                cutMB.clockwiseOrder = (bottom.clockwiseOrder + 1) % 3;
                cutTB.clockwiseOrder = (cutMB.clockwiseOrder + 1) % 3;
            }

            if (cutTB.height > cutMB.height)
                return CuttingAlgorithmHorizontal(cutTB, cutMB, bottom, triangleIndex);
            else
                return CuttingAlgorithmHorizontal(cutMB, cutTB, bottom, triangleIndex);
        }

        private float CuttingAlgorithmOneAbove(VertexInfo top, VertexInfo mid, VertexInfo bottom, int triangleIndex)
        {
            float submergedArea = 0f;

            float tPointCutTB = - bottom.height / (top.height - bottom.height);
            Vector3 pointCutTB = bottom.globalVertex + (top.globalVertex - bottom.globalVertex) * tPointCutTB;
            VertexInfo cutTB = new VertexInfo
            {
                clockwiseOrder = -1,
                globalVertex = pointCutTB,
                height = WaterPatch.instance.DistanceToWater(pointCutTB, timeStamp),
            };

            float tPointCutTM = - mid.height / (top.height - mid.height);
            Vector3 pointCutTM = mid.globalVertex + (top.globalVertex - mid.globalVertex) * tPointCutTM;
            VertexInfo cutTM = new VertexInfo
            {
                clockwiseOrder = -1,
                globalVertex = pointCutTM,
                height = WaterPatch.instance.DistanceToWater(pointCutTM, timeStamp),
            };

            if ((top.clockwiseOrder + 1) % 3 == mid.clockwiseOrder)
            {
                // downLeft: bottom, downRight: mid
                cutTB.clockwiseOrder = (mid.clockwiseOrder + 1) % 3;
                cutTM.clockwiseOrder = (cutTB.clockwiseOrder + 1) % 3;
            }
            else
            {
                // downLeft: mid, downRight: bottom
                cutTM.clockwiseOrder = (mid.clockwiseOrder + 1) % 3;
                cutTB.clockwiseOrder = (cutTM.clockwiseOrder + 1) % 3;
            }
            VertexInfo[] sortingVertices = new VertexInfo[] {mid, cutTB, cutTM};
            Array.Sort(sortingVertices);
            submergedArea += CuttingAlgorithmHorizontal(sortingVertices[0], sortingVertices[1], sortingVertices[2], triangleIndex);

            if ((top.clockwiseOrder + 1) % 3 == mid.clockwiseOrder)
            {
                // downLeft: bottom, downRight: mid
                bottom.clockwiseOrder = (mid.clockwiseOrder + 1) % 3;
                cutTB.clockwiseOrder = (bottom.clockwiseOrder + 1) % 3;
            }
            else
            {
                // downLeft: mid, downRight: bottom
                cutTB.clockwiseOrder = (mid.clockwiseOrder + 1) % 3;
                bottom.clockwiseOrder = (cutTB.clockwiseOrder + 1) % 3;
            }
            sortingVertices = new VertexInfo[] {mid, cutTB, bottom};
            Array.Sort(sortingVertices);
            submergedArea += CuttingAlgorithmHorizontal(sortingVertices[0], sortingVertices[1], sortingVertices[2], triangleIndex);

            return submergedArea;
        }

        private float CuttingAlgorithmHorizontal(VertexInfo top, VertexInfo mid, VertexInfo bottom, int triangleIndex)
        {
            if (top.height - mid.height < epsilon)
            {
                int triangleType = (mid.height - bottom.height < epsilon) ? 2 : 1;

                TriangleInfo triangle;
                if ((top.clockwiseOrder + 1) % 3 == mid.clockwiseOrder)
                    triangle = new TriangleInfo(top, mid, bottom, triangleType, Rigidbody3d, timeStamp);
                else
                    triangle = new TriangleInfo(mid, top, bottom, triangleType, Rigidbody3d, timeStamp);

                TriangleBufferIndices.Add(triangleIndex);
                SubmergedTriangles.Add(triangle);
                return triangle.area;
            }
            else if (mid.height - bottom.height < epsilon)
            {
                TriangleInfo triangle;
                if ((top.clockwiseOrder + 1) % 3 == mid.clockwiseOrder)
                    triangle = new TriangleInfo(top, mid, bottom, 0, Rigidbody3d, timeStamp);
                else
                    triangle = new TriangleInfo(top, bottom, mid, 0, Rigidbody3d, timeStamp);

                TriangleBufferIndices.Add(triangleIndex);
                SubmergedTriangles.Add(triangle);
                return triangle.area;
            }

            float tPointCutTB = (mid.height - bottom.height) / (top.height - bottom.height);
            Vector3 pointCutTB = bottom.globalVertex + (top.globalVertex - bottom.globalVertex) * tPointCutTB;
            VertexInfo cutTB = new VertexInfo
            {
                clockwiseOrder = -1, // no more use
                globalVertex = pointCutTB,
                height = WaterPatch.instance.DistanceToWater(pointCutTB, timeStamp),
            };

            TriangleInfo upside, downside;
            if ((top.clockwiseOrder + 1) % 3 == mid.clockwiseOrder)
            {
                // downLeft: bottom, downRight: mid
                upside = new TriangleInfo(top, mid, cutTB, 0, Rigidbody3d, timeStamp);
                downside = new TriangleInfo(cutTB, mid, bottom, 1, Rigidbody3d, timeStamp);
            }
            else
            {
                // downLeft: mid, downRight: bottom
                upside = new TriangleInfo(top, cutTB, mid, 0, Rigidbody3d, timeStamp);
                downside = new TriangleInfo(mid, cutTB, bottom, 1, Rigidbody3d, timeStamp);
            }

            TriangleBufferIndices.Add(triangleIndex);
            TriangleBufferIndices.Add(triangleIndex);
            SubmergedTriangles.Add(upside);
            SubmergedTriangles.Add(downside);

            return (upside.area + downside.area);
        }
    }
}
