using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace JustPirate
{

public class WaterInteraction : MonoBehaviour
{
    public GameObject underWaterObj;
    public GameObject aboveWaterObj;
    private Rigidbody hullRB;
    private Mesh underWaterMesh;
    private Mesh aboveWaterMesh;

    private HullMeshModifier hullMeshModifier;
    private float densityOfWater = 999.1026f; // kg/m^3 in 15 Celcius

    void Start()
    {
        hullRB = GetComponent<Rigidbody>();
        hullMeshModifier = new HullMeshModifier(gameObject, underWaterObj, aboveWaterObj);
        underWaterMesh = underWaterObj.GetComponent<MeshFilter>().mesh;
        aboveWaterMesh = aboveWaterObj.GetComponent<MeshFilter>().mesh;
    }

    void Update()
    {
        // Update Data
        hullMeshModifier.CalculateVertexData();
        hullMeshModifier.DisplayMesh(underWaterMesh, "UnderWaterMesh", hullMeshModifier.submergedTriangles);
    }

    void FixedUpdate()
    {
        // Update Force
        if (hullMeshModifier.submergedTriangles.Count > 0)
            AddHydroForce();
    }

    private void AddHydroForce()
    {
        // Need density, gravity, h_center, normal vector
        List<TriangleData> submergedTriangles = hullMeshModifier.submergedTriangles;
        float Cf = WaterPhysicsMath.ResistanceCoefficient(
                densityOfWater,
                hullRB.velocity.magnitude,
                hullMeshModifier.CalculateUnderWaterLength());

        //To calculate the slamming force we need the velocity at each of the original triangles
        List<SlammingForceData> slammingForceData = hullMeshModifier.slammingForceDataList;

        CalculateSlammingVelocities(slammingForceData);

        //Need this data for slamming forces
        float boatArea = hullMeshModifier.boatArea;
        float boatMass = hullRB.mass; //Replace this line with your boat's total mass

        //To connect the submerged triangles with the original triangles
        List<int> indexOfOriginalTriangle = hullMeshModifier.indexOfOriginalTriangle;

        for (int i = 0; i < submergedTriangles.Count; i++)
        {
            // This triangle
            TriangleData triangleData = submergedTriangles[i];

            if (triangleData.centerDistance > 0)
            {
                // Debug.DrawRay(triangleData.center, triangleData.normal * 3f, Color.red);
                continue;
            }

            Vector3 buoyancyForce = WaterPhysicsMath.BuoyancyForce(densityOfWater, triangleData);
            Vector3 viscousWaterResistance = WaterPhysicsMath.ViscousWaterResistance(densityOfWater, Cf, triangleData);
            Vector3 pressureDragForce = WaterPhysicsMath.PressureDragForce(triangleData);

            int originalTriangleIndex = indexOfOriginalTriangle[i];
            SlammingForceData slammingData = slammingForceData[originalTriangleIndex];
            Vector3 slammingForce = WaterPhysicsMath.SlammingForce(slammingData, triangleData, boatArea, boatMass);
            Vector3 totalForce = Vector3.zero;
            totalForce += buoyancyForce;
            totalForce += viscousWaterResistance;
            totalForce += pressureDragForce;
            totalForce += slammingForce;

            //Add the force to the boat
            hullRB.AddForceAtPosition(totalForce, triangleData.center);
            //Normal
            // Debug.DrawRay(triangleData.center, triangleData.normal * 3f, Color.white);
            //Buoyancy
            // Debug.DrawRay(triangleData.center, buoyancyForce.normalized * -3f, Color.blue);
        }
    }

    //Calculate the current velocity at the center of each triangle of the original boat mesh
    private void CalculateSlammingVelocities(List<SlammingForceData> slammingForceData)
    {
        for (int i = 0; i < slammingForceData.Count; i++)
        {
            //Set the new velocity to the old velocity
            slammingForceData[i].previousVelocity = slammingForceData[i].velocity;

            //Center of the triangle in world space
            Vector3 center = transform.TransformPoint(slammingForceData[i].triangleCenter);

            //Get the current velocity at the center of the triangle
            slammingForceData[i].velocity = WaterPhysicsMath.GetTriangleVelocity(hullRB, center);
        }
    }

}

}