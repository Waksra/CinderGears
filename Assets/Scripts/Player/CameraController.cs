using UnityEngine;

namespace Player
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Transform targetTransform;
        [SerializeField, Range(-180, 180)] private float minPitch = -75f;
        [SerializeField, Range(-180, 180)] private float maxPitch = 75f;
        [SerializeField, Range(0, 0.5f)] private float moveSmoothTime = 0.01f;
        [SerializeField, Range(0, 720f)] private float rotationSpeed = 480;

        private new Transform transform;

        private Vector3 moveVelocity = Vector3.zero;

        private Vector3 rotation;
        float xRotation;

        private void Awake()
        {
            transform = GetComponent<Transform>();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            rotation = targetTransform.eulerAngles;
            xRotation = targetTransform.eulerAngles.x;
        }

        private void LateUpdate()
        {
            transform.position =
                Vector3.SmoothDamp(transform.position, targetTransform.position, ref moveVelocity, moveSmoothTime);

            Vector2 lookVector = InputHandler.LookInput.Value;
            xRotation += lookVector.y;
            xRotation = Mathf.Clamp(xRotation, minPitch, maxPitch);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetTransform.rotation * Quaternion.Euler(xRotation, 0, 0), 
                rotationSpeed * Time.deltaTime);
        }
    }
}