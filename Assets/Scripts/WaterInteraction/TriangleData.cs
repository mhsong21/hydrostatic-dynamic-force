using UnityEngine;
using System;

namespace WaterInteraction
{
    public struct VertexInfo : IComparable<VertexInfo>
    {
        public float height; // distance to surface of water
        public int clockwiseOrder;
        public Vector3 globalVertex; // global position of this vertex

        public int CompareTo(VertexInfo other)
        {
            return other.height.CompareTo(this.height);
        }
    }

    public struct TriangleInfo
    {
        public Vector3 center;
        public float height;
        public Vector3 normal;
        public float area;
        public Vector3 velocity;
        public float cosine;

        public TriangleInfo(VertexInfo vi1, VertexInfo vi2, VertexInfo vi3, int triangleType, Rigidbody rB, float timeStamp)
        {
            Vector3 v1 = vi1.globalVertex;
            Vector3 v2 = vi2.globalVertex;
            Vector3 v3 = vi3.globalVertex;

            this.center = WaterInteractionUtils.GetTriangleCenter(vi1, vi2, vi3, triangleType);

            // beause of simplification. center distance can be positive in submerged triangle
            // if height is positive, don't add to force
            this.height = WaterPatch.instance.DistanceToWater(this.center, timeStamp);
            this.normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;
            this.velocity = WaterInteractionUtils.GetTriangleVelocity(this.center, rB);
            if (this.velocity.magnitude < 0.0001f)
                this.cosine = 0;
            else
                this.cosine = Vector3.Dot(this.velocity, this.normal) / this.velocity.magnitude;
            this.area = WaterInteractionUtils.GetTriangleArea(v1, v2, v3);
        }
    }

    public struct TriangleBuffer
    {
        public float submergedArea;
        public float originalArea;
        public Vector3 velocity;

        public TriangleBuffer(Vector3 v1, Vector3 v2, Vector3 v3, Rigidbody rB)
        {
            Vector3 center = (v1 + v2 + v3) / 3f;
            this.velocity = WaterInteractionUtils.GetTriangleVelocity(center, rB);
            this.submergedArea = 0f;
            this.originalArea = WaterInteractionUtils.GetTriangleArea(v1, v2, v3);
        }
    }
}
