using UnityEngine;


namespace JustPirate
{

// VertexData has to be reference type -> class
public class VertexData
{
    public float distance;
    public int clockwiseIndex;
    public Vector3 vertexGlobalPos;
}

// TriangleData has to be value type -> struct
public struct TriangleData
{
    public Vector3 v1;
    public Vector3 v2;
    public Vector3 v3;

    public Vector3 center;
    public float centerDistance;
    public float area;
    public Vector3 normal;

    public TriangleData(VertexData p1, VertexData p2, VertexData p3, bool isUpsideTriangle)
    {
        this.v1 = p1.vertexGlobalPos;
        this.v2 = p2.vertexGlobalPos;
        this.v3 = p3.vertexGlobalPos;

        if (isUpsideTriangle)
        {
            // p1 is upside vertex and p2, p3 are horizontal downside vertices
            float z_0 = p1.distance;
            float h = p2.distance - p1.distance;
            float t_center = (4 * z_0 + 3 * h) / (6 * z_0 + 4 * h);

            Vector3 v1_m = (this.v2 + this.v3) / 2f - this.v1;

            this.center = this.v1 + v1_m * t_center;
        }
        else
        {
            // p1, p2 are horizontal upside vertices and p3 is downside vertex
            float z_0 = p1.distance;
            float h = p3.distance - p1.distance;
            float t_center = (2 * z_0 + h) / (6 * z_0 + 2 * h);

            Vector3 v3_m = (this.v1 + this.v2) / 2f - this.v3;

            this.center = v3 + v3_m * t_center;
        }

        // beause of simplification. center distance can be positive in submerged triangle
        // if centerDistance is positive, don't add to force
        centerDistance = WaterPatch.instance.DistanceToWater(this.center, Time.time);

        this.normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;

        //Area of the triangle
        float a = Vector3.Distance(v1, v2);
        float c = Vector3.Distance(v3, v1);

        this.area = (a * c * Mathf.Sin(Vector3.Angle(v2 - v1, v3 - v1) * Mathf.Deg2Rad)) / 2f;
    }
}

}