using UnityEngine;

namespace WaterInteraction
{
    public static class WaterInteractionUtils
    {
        public static Vector3 GetTriangleCenter(VertexInfo vi1, VertexInfo vi2, VertexInfo vi3, int triangleType)
        {
            Vector3 v1 = vi1.globalVertex;
            Vector3 v2 = vi2.globalVertex;
            Vector3 v3 = vi3.globalVertex;

            if (triangleType == 0)
            {
                // vi1 is upside vertex and vi2, vi3 are horizontal downside vertices
                float z0 = vi1.height;
                float h = vi2.height - vi1.height;
                float tCenter = (4 * z0 + 3 * h) / (6 * z0 + 4 * h);
                Vector3 v1ToMid = (v2 + v3) / 2f - v1;

                return (v1 + v1ToMid * tCenter);
            }
            else if (triangleType == 1)
            {
                // vi1, vi2 are horizontal upside vertices and vi3 is downside vertex
                float z0 = vi1.height;
                float h = vi3.height - vi1.height;
                float tCenter = (2 * z0 + h) / (6 * z0 + 2 * h);
                Vector3 v3ToMid = (v1 + v2) / 2f - v3;

                return (v3 + v3ToMid * tCenter);
            }
            else
            {
                return (v1 + v2 + v3) / 3f;
            }
        }

        public static Vector3 GetTriangleVelocity(Vector3 center, Rigidbody rB)
        {
            // V = V_G + AV_G cross G_C
            Vector3 globalCenterToCenter = center - rB.worldCenterOfMass;
            return rB.velocity + Vector3.Cross(rB.angularVelocity, globalCenterToCenter);
        }

        public static float GetTriangleArea(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            float a = Vector3.Distance(v1, v2);
            float c = Vector3.Distance(v3, v1);
            return (a * c * Mathf.Sin(Vector3.Angle(v2 - v1, v3 - v1) * Mathf.Deg2Rad)) / 2f;
        }

        public static Vector3 BuoyancyForce(float densityOfWater, TriangleInfo triangle)
        {
            Vector3 buoyancyForce = densityOfWater * Physics.gravity.y * -1 * triangle.height * triangle.area * triangle.normal;

            // The vertical component of the hydrostatic forces don't cancel out but the horizontal do
            buoyancyForce.x = 0f;
            buoyancyForce.z = 0f;

            return buoyancyForce;
        }

        public static float CalculateViscosityCoefficient(float logRigidbodyVelocityCache, float logFluidTravelLengthCache, float logViscosityCache)
        {
            // Reynolds number = (V * L) / v
            // V - speed of the body
            // L - length the fluid has to traverse across the surface
            // v - viscosity of the fluid [m^2 / s]

            // Log10 is very long calc, so I made lookUp table
            // Log10(Reynolds Number) = Log10(rigidbodyVelocity) + Log10(fluidTravelLength) - Log10(fluidViscosity)
            float reynoldsNumber = logRigidbodyVelocityCache + logFluidTravelLengthCache - logViscosityCache;

            // The resistance coefficient
            return 0.075f / Mathf.Pow((reynoldsNumber - 2f), 2f);
        }

        public static Vector3 ViscousWaterResistance(float densityOfWater, float viscosityCoefficient, TriangleInfo triangle)
        {
            Vector3 velocity = triangle.velocity;
            Vector3 normal = triangle.normal;
            Vector3 tangentialVelocity = velocity - Vector3.Dot(velocity, normal) * normal;
            tangentialVelocity *= -1;
            tangentialVelocity.Normalize();

            return (0.5f * densityOfWater * viscosityCoefficient * triangle.area * velocity.magnitude * velocity.magnitude * tangentialVelocity);
        }

        public static Vector3 PressureDragForce(TriangleInfo triangle, float referenceVelocity, float pdCoefficient1, float pdCoefficient2, float pdFallOffPower, float sdCoefficient1, float sdCoefficient2, float sdFallOffPower)
        {
            float speed = triangle.velocity.magnitude / referenceVelocity;
            float cosine = triangle.cosine;

            if (cosine > 0)
                return -1 * (pdCoefficient1 * speed + pdCoefficient2 * speed * speed) * triangle.area * Mathf.Pow(cosine, pdFallOffPower) * triangle.normal;
            else
                return (sdCoefficient1 * speed + sdCoefficient2 * speed * speed) * triangle.area * Mathf.Pow(-cosine, sdFallOffPower) * triangle.normal;
        }

        public static Vector3 SlammingForce(TriangleBuffer previousBuffer, TriangleBuffer currentBuffer, TriangleInfo triangle, float maxAcceleration, float rampUpPower, float totalMass, float totalArea)
        {
            if (triangle.cosine < 0f)
                return Vector3.zero;

            Vector3 currentDeltaVolume = currentBuffer.submergedArea * currentBuffer.velocity;
            Vector3 previousDeltaVolume = previousBuffer.submergedArea * previousBuffer.velocity;
            Vector3 acceleration = (currentDeltaVolume - previousDeltaVolume) / (currentBuffer.originalArea * Time.fixedDeltaTime);

            Vector3 stoppingForce = totalMass * triangle.velocity * (2f * triangle.area / totalArea);
            float realAcceleration = Mathf.Clamp01(acceleration.magnitude / maxAcceleration);

            return -1 * Mathf.Pow(realAcceleration, rampUpPower) * triangle.cosine * stoppingForce;
        }
    }
}
