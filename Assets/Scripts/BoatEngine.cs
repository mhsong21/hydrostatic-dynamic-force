using UnityEngine;

namespace WaterInteraction
{
    public class BoatEngine : MonoBehaviour
    {
        public float enginePower;
        public float rotationPower;
        public float currentSpeed;
        public float currentAcceleration;
        public float currentRotation;
        public float maxAcceleration;
        public float maxBackwardAcceleration;
        public Transform engineTransform;

        private Rigidbody engineRB;
        protected Rigidbody EngineRB
        {
            get
            {
                if (engineRB == null)
                    engineRB = engineTransform.GetComponent<Rigidbody>();
                return engineRB;
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

        public void Update()
        {
            UpdateUserInput();
        }

        public void UpdateUserInput()
        {
            if (Input.GetAxis("Vertical") > 0)
            {
                currentAcceleration = Mathf.Clamp(currentAcceleration + enginePower, 0, maxAcceleration);
            }
            else if (Input.GetAxis("Vertical") < 0)
            {
                currentAcceleration = Mathf.Clamp(currentAcceleration - enginePower, -maxBackwardAcceleration, 0);
            }
            else
            {
                currentAcceleration = 0;
            }

            if (Input.GetAxis("Horizontal") > 0)
            {
                // currentRotation += rotationPower;
                currentRotation = Mathf.Clamp(currentRotation + rotationPower, -80f, 80f);
            }
            else if (Input.GetAxis("Horizontal") < 0)
            {
                // currentRotation -= rotationPower;
                currentRotation = Mathf.Clamp(currentRotation - rotationPower, -80f, 80f);
            }
            else
            {
                currentRotation *= 0.8f;
            }
        }

        public void FixedUpdate()
        {
            currentSpeed = Rigidbody3d.velocity.magnitude;
            AddForce();
        }

        public void AddForce()
        {
            // vector = Quaternion.Euler(0, -45, 0) * vector;

            Vector3 force = Quaternion.Euler(0, -currentRotation, 0) * -engineTransform.up * currentAcceleration;
            EngineRB.AddForceAtPosition(force, engineTransform.position, ForceMode.Acceleration);
        }
    }
}