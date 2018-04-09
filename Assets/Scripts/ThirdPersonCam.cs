using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace Song
{
    public class ThirdPersonCam : PivotBasedCameraRig
    {
        // This script is designed to be placed on the root object of a camera rig,
        // comprising 3 gameobjects, each parented to the next:

        // 	Camera Rig
        // 		Pivot
        // 			Camera

        [SerializeField] private float m_MoveSpeed = 1f;                      // How fast the rig will move to keep up with the target's position.
        [Range(0f, 10f)] [SerializeField] private float m_TurnSpeed = 1.5f;   // How fast the rig will rotate from user input.
        [SerializeField] private Transform m_Target;
        [SerializeField] private bool m_AutoTargetPlayer = true;  // Whether the rig should automatically target the player.

        [SerializeField] private float m_TargetDistance = 0.0f;
        [SerializeField] private float m_TurnSmoothing = 0.0f;                // How much smoothing to apply to the turn input, to reduce mouse-turn jerkiness
        [SerializeField] private float m_TiltMax = 75f;                       // The maximum value of the x axis rotation of the pivot.
        [SerializeField] private float m_TiltMin = 0f;                       // The minimum value of the x axis rotation of the pivot.
        [SerializeField] private bool m_LockCursor = false;                   // Whether the cursor should be hidden and locked.
        [SerializeField] private bool m_VerticalAutoReturn = false;           // set wether or not the vertical axis should auto return

        private float m_LookAngle;                    // The rig's y axis rotation.
        private float m_TiltAngle;                    // The pivot's x axis rotation.
        private const float k_LookDistance = 100f;    // How far in front of the pivot the character's look target is.
		private Vector3 m_PivotEulers;
		private Quaternion m_PivotTargetRot;
		private Quaternion m_TransformTargetRot;
        private Vector3 m_PivotPosition;
        private Vector3 m_TargetPosition;
        private float m_Theta;
        private float m_Pie;

        protected virtual void Start()
        {
            // if auto targeting is used, find the object tagged "Player"
            // any class inheriting from this should call base.Start() to perform this action!
            if (m_AutoTargetPlayer)
            {
                FindAndTargetPlayer();
            }
            if (m_Target == null) return;
        }

        public void FindAndTargetPlayer()
        {
            // auto target an object tagged player, if no target has been assigned
            var targetObj = GameObject.FindGameObjectWithTag("Player");
            if (targetObj)
            {
                SetTarget(targetObj.transform);
            }
        }

        public virtual void SetTarget(Transform newTransform)
        {
            m_Target = newTransform;
        }

        protected override void Awake()
        {
            base.Awake();
			m_PivotEulers = m_Pivot.rotation.eulerAngles;
			m_PivotPosition = m_Pivot.transform.position;
			m_TargetPosition = m_Target.transform.position;
            m_Theta = 0f;
            m_Pie = 0f;

	        m_PivotTargetRot = m_Pivot.transform.localRotation;
			m_TransformTargetRot = transform.localRotation;
        }


        private void LateUpdate()
        {
            HandlePositionMovement();
        }


        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void HandlePositionMovement()
        {
            if (Time.timeScale < float.Epsilon)
			return;

            // Read the user input
            var x = CrossPlatformInputManager.GetAxis("Mouse X");
            var y = CrossPlatformInputManager.GetAxis("Mouse Y");
            m_Theta += y * m_TurnSpeed;
            m_Theta = Mathf.Clamp(m_Theta, m_TiltMin, m_TiltMax);
            m_Pie += x * m_TurnSpeed;
            Vector3 dir = new Vector3(0, 0, -m_TargetDistance);
            Quaternion rotation = Quaternion.Euler(m_Theta, m_Pie, 0);
            m_PivotPosition = m_TargetPosition + rotation * dir;
            m_Pivot.localPosition = m_PivotPosition;
            m_Pivot.LookAt(m_TargetPosition);
        }
    }
}
