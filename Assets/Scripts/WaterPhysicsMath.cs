using UnityEngine;

namespace JustPirate
{

public static class WaterPhysicsMath
{
    public static Vector3 GetTriangleCenter(VertexData p1, VertexData p2, VertexData p3, int triangleType)
    {
        Vector3 v1 = p1.vertexGlobalPos;
        Vector3 v2 = p2.vertexGlobalPos;
        Vector3 v3 = p3.vertexGlobalPos;

        if (triangleType == 0)
        {
            // p1 is upside vertex and p2, p3 are horizontal downside vertices
            float z_0 = p1.distance;
            float h = p2.distance - p1.distance;
            float t_center = (4 * z_0 + 3 * h) / (6 * z_0 + 4 * h);

            Vector3 v1_m = (v2 + v3) / 2f - v1;

            return (v1 + v1_m * t_center);
        }
        else if (triangleType == 1)
        {
            // p1, p2 are horizontal upside vertices and p3 is downside vertex
            float z_0 = p1.distance;
            float h = p3.distance - p1.distance;
            float t_center = (2 * z_0 + h) / (6 * z_0 + 2 * h);

            Vector3 v3_m = (v1 + v2) / 2f - v3;

            return (v3 + v3_m * t_center);
        }
        else
        {
            return (v1 + v2 + v3) / 3f;
        }
    }

    public static Vector3 GetTriangleVelocity(Rigidbody hullRB, Vector3 center)
    {
        // V = V_G + AV_G cross G_C
        Vector3 G_C = center - hullRB.worldCenterOfMass;
        return hullRB.velocity + Vector3.Cross(hullRB.angularVelocity, G_C);
    }

    public static float GetTriangleArea(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        //Area of the triangle
        float a = Vector3.Distance(v1, v2);
        float c = Vector3.Distance(v3, v1);

        return (a * c * Mathf.Sin(Vector3.Angle(v2 - v1, v3 - v1) * Mathf.Deg2Rad)) / 2f;
    }

    public static float ResistanceCoefficient(float rho, float velocity, float length)
    {
        //Reynolds number

        // Rn = (V * L) / nu
        // V - speed of the body
        // L - length of the sumbmerged body
        // nu - viscosity of the fluid [m^2 / s]

        //Viscocity depends on the temperature, but at 20 degrees celcius:
        float nu = 0.000001f;
        //At 30 degrees celcius: nu = 0.0000008f; so no big difference

        //Reynolds number
        float Rn = (velocity * length) / nu;

        //The resistance coefficient
        float Cf = 0.075f / Mathf.Pow((Mathf.Log10(Rn) - 2f), 2f);

        return Cf;
    }

    public static Vector3 BuoyancyForce(float rho, TriangleData triangleData)
    {
        // Buoyancy is a hydrostatic force - it's there even if the water isn't flowing or if the boat stays still

        // F_buoyancy = rho * g * V
        // rho - density of the mediaum you are in
        // g - gravity
        // V - volume of fluid directly above the curved surface 

        // V = z * S * n 
        // z - distance to surface
        // S - surface area
        // n - normal to the surfac
        Vector3 buoyancyForce = rho * Physics.gravity.y * -triangleData.centerDistance * triangleData.area * triangleData.normal;

        //The vertical component of the hydrostatic forces don't cancel out but the horizontal do
        buoyancyForce.x = 0f;
        buoyancyForce.z = 0f;

        return buoyancyForce;
    }

    public static Vector3 ViscousWaterResistance(float rho, float cf, TriangleData triangleData)
    {
        // R = 1/2 * rho * C * S * V^2
        // R = 1/2 * rho * coefficient by Reynolds * area of triangle * tangential velocity of triangle ^ 2

        Vector3 velocity = triangleData.velocity;
        Vector3 normal = triangleData.normal;

        Vector3 tangentialVelocity = velocity - Vector3.Dot(velocity, normal) * normal;
        tangentialVelocity *= -1;
        tangentialVelocity.Normalize();


        Vector3 res = (0.5f * rho * cf * triangleData.area * velocity.magnitude * velocity.magnitude * tangentialVelocity);
        // Debug.DrawRay(triangleData.center, res * 3f, Color.black);
        return res;
    }

    public static Vector3 PressureDragForce(TriangleData triangleData)
    {
        // by cosine value, determine where triangle is (front, back)
        // this force acts along the normal of the surface
        // f_P, f_S is 0 ~ 1 falloff value
        // C_PD1, PD2, SD1, SD2 is coefficient
        Vector3 drag;

        // float speed = triangleData.velocity.magnitude / DebugPhysics.instance.velocityReference;
        float speed = triangleData.velocity.magnitude;
        float cosine = triangleData.cosine;
        if (cosine > 0)
        {
            // float C_PD1 = 10f;
            // float C_PD2 = 10f;
            // float f_P = 0.5f;

            // To change the variables real-time - add the finished values later
            float C_PD1 = DebugPhysics.instance.C_PD1;
            float C_PD2 = DebugPhysics.instance.C_PD2;
            float f_P = DebugPhysics.instance.f_P;

            drag = -1 * (C_PD1 * speed + C_PD2 * speed * speed) * triangleData.area * Mathf.Pow(cosine, f_P) * triangleData.normal;
        }
        else
        {
            // float C_SD1 = 10f;
            // float C_SD2 = 10f;
            // float f_S = 0.5f;

            // To change the variables real-time - add the finished values later
            float C_SD1 = DebugPhysics.instance.C_SD1;
            float C_SD2 = DebugPhysics.instance.C_SD2;
            float f_S = DebugPhysics.instance.f_S;

            drag = (C_SD1 * speed + C_SD2 * speed * speed) * triangleData.area * Mathf.Pow(-cosine, f_S) * triangleData.normal;
        }
        
        return drag;
    }

    public static Vector3 SlammingForce(SlammingForceData slammingData, TriangleData triangleData, float boatArea, float boatMass)
    {
        //To capture the response of the fluid to sudden accelerations or penetrations

        //Add slamming if the normal is in the same direction as the velocity (the triangle is not receding from the water)
        //Also make sure thea area is not 0, which it sometimes is for some reason
        if (triangleData.cosine < 0f || slammingData.originalArea <= 0f)
        {
            return Vector3.zero;
        }

        //Step 1 - Calculate acceleration
        //Volume of water swept per second
        Vector3 dV = slammingData.submergedArea * slammingData.velocity;
        Vector3 dV_previous = slammingData.previousSubmergedArea * slammingData.previousVelocity;

        //Calculate the acceleration of the center point of the original triangle (not the current underwater triangle)
        //But the triangle the underwater triangle is a part of
        Vector3 accVec = (dV - dV_previous) / (slammingData.originalArea * Time.fixedDeltaTime);

        //The magnitude of the acceleration
        float acc = accVec.magnitude;

        //Debug.Log(slammingForceData.originalArea);

        //Step 2 - Calculate slamming force
        // F = clamp(acc / acc_max, 0, 1)^p * cos(theta) * F_stop
        // p - power to ramp up slamming force - should be 2 or more

        // F_stop = m * v * (2A / S)
        // m - mass of the entire boat
        // v - velocity
        // A - this triangle's area
        // S - total surface area of the entire boat

        Vector3 F_stop = boatMass * triangleData.velocity * ((2f * triangleData.area) / boatArea);

        //float p = DebugPhysics.current.p;

        float acc_max = DebugPhysics.instance.acc_max;

        float p = 2f;

        // float acc_max = acc;

        float slammingCheat = DebugPhysics.instance.slammingCheat;

        Vector3 slammingForce = Mathf.Pow(Mathf.Clamp01(acc / acc_max), p) * triangleData.cosine * F_stop * slammingCheat;

        //Vector3 slammingForce = Vector3.zero;

        //Debug.Log(slammingForce);

        //The force acts in the opposite direction
        slammingForce *= -1f;

        return slammingForce;   
        
    }
}

}