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
    public Vector3 velocity;
    public float cosine;
    public float centerDistance;
    public float area;
    public Vector3 normal;

    public TriangleData(VertexData p1, VertexData p2, VertexData p3, int triangleType, Rigidbody hullRB, float timeSinceStart)
    {
        // triangleType => 0 : upside, 1 : downside, 2 : above water

        this.v1 = p1.vertexGlobalPos;
        this.v2 = p2.vertexGlobalPos;
        this.v3 = p3.vertexGlobalPos;

        this.center = WaterPhysicsMath.GetTriangleCenter(p1, p2, p3, triangleType);

        // beause of simplification. center distance can be positive in submerged triangle
        // if centerDistance is positive, don't add to force
        this.centerDistance = WaterPatch.instance.DistanceToWater(this.center, timeSinceStart);

        this.normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;
        this.velocity = WaterPhysicsMath.GetTriangleVelocity(hullRB, this.center);
        this.cosine = Vector3.Dot(this.velocity, this.normal) / this.velocity.magnitude;
        this.area = WaterPhysicsMath.GetTriangleArea(v1, v2, v3);
    }
}

public class SlammingForceData 
{
    //The area of the original triangles - calculate once in the beginning because always the same
    public float originalArea;
    //How much area of a triangle in the whole boat is submerged
    public float submergedArea;
    //Same as above but previous time step
    public float previousSubmergedArea;
    //Need to save the center of the triangle to calculate the velocity
    public Vector3 triangleCenter;
    //Velocity
    public Vector3 velocity;
    //Same as above but previous time step
    public Vector3 previousVelocity;
}

}