using UnityEngine;

namespace JustPirate
{
    public class BoatEngine : MonoBehaviour
    {
        public float enginePower;
        public float currentSpeed;
        public float currentAcceleration;
        public float maxAcceleration;
        public float maxBackwardAcceleration;
        public Transform engineTransform;

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
        }

        public void FixedUpdate()
        {
            currentSpeed = Rigidbody3d.velocity.magnitude;
            AddForce();
        }

        public void AddForce()
        {
            Vector3 force = -engineTransform.up * currentAcceleration;
            Rigidbody3d.AddForceAtPosition(force, engineTransform.position, ForceMode.Acceleration);
        }
    }
}