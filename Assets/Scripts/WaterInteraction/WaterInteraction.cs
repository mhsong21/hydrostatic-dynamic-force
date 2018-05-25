using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace JustPirate
{
    [RequireComponent (typeof (Rigidbody))]
    [RequireComponent (typeof (CurveLookUpTable))]
    [RequireComponent (typeof (TriangleBuilder))]
    public class WaterInteraction : MonoBehaviour
    {
        public bool debugBuoyancyForce = true;
        public bool debugViscousWaterResistance = true;
        public bool debugPressureDrag = true;
        public bool debugSlammingForce = true;
        public CurveLookUpTable curveLookUpTable;
        public TriangleBuilder triangleBuilder;

        [Header("Pressure drag")]
        public float referenceSpeed;
        public float pdCoefficient1;
        public float pdCoefficient2;
        public float pdFallOffPower;

        [Header("Suction drag")]
        public float sdCoefficient1;
        public float sdCoefficient2;
        public float sdFallOffPower;

        [Header("Slamming Force")]
        public float smRampUpPower;
        public float maxAcceleration;

        // Density of water, kg/m^3 in 15 Celcius
        private float densityOfWater = 999.1026f;

        // Viscosity depends on the temperature
        // At 15 degrees celcius: v = 0.0000011f
        // At 30 degrees celcius: v = 0.0000008f
        // Log10(0.0000011f) = -5.958607314841775f
        private float logViscosity = -5.958607314841775f;
        private float meshBoundZ;

        private float totalArea = -1f;
        public float TotalArea
        {
            get
            {
                if (totalArea < 0)
                    totalArea = triangleBuilder.TotalArea;
                return totalArea;
            }
        }

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

        private void FixedUpdate()
        {
            triangleBuilder.CreateTriangleData();

            if (triangleBuilder.SubmergedTriangles.Count > 0)
                CalculateForce();
        }

        private void CalculateForce()
        {
            float zMin = float.MaxValue;
            float zMax = float.MinValue;
            for (int i = 0; i < triangleBuilder.SubmergedTriangles.Count; ++i)
            {
                TriangleInfo triangle = triangleBuilder.SubmergedTriangles[i];
                if (triangle.center.z < zMin)
                    zMin = triangle.center.z;
                if (triangle.center.z > zMax)
                    zMax = triangle.center.z;
            }

            float logRigidbodyVelocity = Mathf.Log10(Rigidbody3d.velocity.magnitude);
            float travelLengthScaleFactor = Mathf.Log10(zMax - zMin) * 2;

            for (int i = 0; i < triangleBuilder.SubmergedTriangles.Count; ++i)
            {
                TriangleInfo triangle = triangleBuilder.SubmergedTriangles[i];
                if (triangle.height > 0f)
                    continue;

                float lookUpKey = (triangle.center.z - zMin) / (zMax - zMin);
                float logFluidTravelLength = curveLookUpTable.LookUp(lookUpKey) * travelLengthScaleFactor;
                float viscosityCoefficient = WaterInteractionUtils.CalculateViscosityCoefficient(logRigidbodyVelocity, logFluidTravelLength, logViscosity);

                int TriangleBufferIndex = triangleBuilder.TriangleBufferIndices[i];

                Vector3 buoyancyForce = WaterInteractionUtils.BuoyancyForce(densityOfWater, triangle);
                Vector3 viscousWaterResistance = WaterInteractionUtils.ViscousWaterResistance(densityOfWater, viscosityCoefficient, triangle);
                Vector3 pressureDragForce = WaterInteractionUtils.PressureDragForce(triangle, referenceSpeed, pdCoefficient1, pdCoefficient2, pdFallOffPower, sdCoefficient1, sdCoefficient2, sdFallOffPower);
                Vector3 slammingForce = WaterInteractionUtils.SlammingForce(triangleBuilder.PreviousBuffers[TriangleBufferIndex], triangleBuilder.CurrentBuffers[TriangleBufferIndex], triangle, maxAcceleration, smRampUpPower, Rigidbody3d.mass, TotalArea);

                Vector3 netForce = Vector3.zero;
                if (debugBuoyancyForce)
                    netForce += buoyancyForce;
                if (debugViscousWaterResistance)
                    netForce += viscousWaterResistance;
                if (debugPressureDrag)
                    netForce += pressureDragForce;
                if (debugSlammingForce)
                    netForce += slammingForce;

                Rigidbody3d.AddForceAtPosition(netForce, triangle.center);

                // Debug.DrawRay(triangle.center, slammingForce, Color.white);
                // Debug.DrawRay(triangle.center, triangle.normal, Color.white);
                // Debug.DrawRay(triangle.center, buoyancyForce, Color.blue);
                // Debug.DrawRay(triangle.center, viscousWaterResistance, Color.green);
            }
        }
    }
}
