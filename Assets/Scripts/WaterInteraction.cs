using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace JustPirate
{

public class WaterInteraction : MonoBehaviour
{
    public GameObject underWaterObj;
    private Rigidbody boatRB;
    private Mesh underWaterMesh;
    private HullMeshModifier hullMeshModifier;
    private float density = 999.1026f; // kg/m^3 in 15 Celcius


    void Start()
    {
        boatRB = GetComponent<Rigidbody>();
        hullMeshModifier = new HullMeshModifier(gameObject);
        underWaterMesh = underWaterObj.GetComponent<MeshFilter>().mesh;
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
            CalculateHydroStaticForce();
    }

    private void CalculateHydroStaticForce()
    {
        // Need density, gravity, h_center, normal vector
        List<TriangleData> submergedTriangles = hullMeshModifier.submergedTriangles;

        for (int i = 0; i < submergedTriangles.Count; i++)
        {
            // This triangle
            TriangleData triangleData = submergedTriangles[i];

            // Calculate the buoyancy force
            Vector3 buoyancyForce = BuoyancyForce(density, triangleData);

            if (triangleData.centerDistance > 0)
            {
                //Normal
                Debug.DrawRay(triangleData.center, triangleData.normal * 3f, Color.red);

                //Buoyancy
                Debug.DrawRay(triangleData.center, buoyancyForce.normalized * -3f, Color.yellow);
                continue;
            }

            //Add the force to the boat
            boatRB.AddForceAtPosition(buoyancyForce, triangleData.center);

            //Normal
            Debug.DrawRay(triangleData.center, triangleData.normal * 3f, Color.white);

            //Buoyancy
            Debug.DrawRay(triangleData.center, buoyancyForce.normalized * -3f, Color.blue);
        }
    }

    private Vector3 BuoyancyForce(float rho, TriangleData triangleData)
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

}

}