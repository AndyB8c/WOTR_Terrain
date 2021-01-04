using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AquariusMax.Medieval
{
    // influenced by Unity
    [RequireComponent(typeof(CharacterController))]
    public class DemoCharacter : MonoBehaviour
    {
        [SerializeField]
        Camera cam;

        [SerializeField]
        float gravityModifier = 2f;
        [SerializeField]
        float walkSpeed = 5f;
        [SerializeField]
        float runSpeed = 10f;


        [SerializeField]
        public float jumpSpeed = 10f;

        [SerializeField]
        private float jumpIndicator;

        [SerializeField]
        public bool isJumpPad = false;

        [SerializeField]
        float landingForce = 10f;

        

        [SerializeField]
        float mouseXSensitivity = 2f;
        [SerializeField]
        float mouseYSensitivity = 2f;

        CharacterController charControl;

        Quaternion characterTargetRot;
        Quaternion cameraTargetRot;

//        bool isGrounded = true;
        bool isWalking = true;
        Vector2 moveInput = Vector2.zero;
        Vector3 move = Vector3.zero;
        bool jumpPressed = false;
//        bool isJumping = false;

        CollisionFlags collisionFlags;


        private void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Start()
        {
            if (cam == null)
            {
                cam = Camera.main;
            }

            charControl = GetComponent<CharacterController>();

            characterTargetRot = transform.localRotation;
            cameraTargetRot = cam.transform.localRotation;
        }


        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag == "JumpPad")
            {
                isJumpPad = true;
                Debug.Log(isJumpPad);
 //               jumpIndicator = jumpSpeed + other.GetComponent<JumpPad>().speedUp;

                Debug.Log(jumpIndicator);
            }


            

        }

        private void OnTriggerExit(Collider other)
        {
            if(other.gameObject.tag == "JumpPad")
            {
                isJumpPad = false;
            }

           
        }

        void GetMoveInput(out float speed)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            moveInput = new Vector2(horizontal, vertical);
            // normalize input if it exceeds 1 in combined length:
            if (moveInput.sqrMagnitude > 1)
            {
                moveInput.Normalize();
            }

            isWalking = !Input.GetKey(KeyCode.LeftShift);

            speed = isWalking ? walkSpeed : runSpeed;
        }

        void CameraLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseXSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseYSensitivity;

            characterTargetRot *= Quaternion.Euler(0f, mouseX, 0f);
            cameraTargetRot *= Quaternion.Euler(-mouseY, 0f, 0f);

            cameraTargetRot = ClampRotationAroundXAxis(cameraTargetRot);

            transform.localRotation = characterTargetRot;
            cam.transform.localRotation = cameraTargetRot;
        }

        void Update()
        {
            CameraLook();

            jumpPressed = Input.GetKeyDown(KeyCode.Space);

        }

       
        private void FixedUpdate()
        {
            float speed;
            GetMoveInput(out speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward * moveInput.y + transform.right * moveInput.x;

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, charControl.radius, Vector3.down, out hitInfo,
                               charControl.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            move.x = desiredMove.x * speed;
            move.z = desiredMove.z * speed;

            if (charControl.isGrounded)
            {
                move.y = -landingForce;

                if (jumpPressed)
                {
                    move.y = jumpSpeed;
                    jumpPressed = false;
//                    isJumping = true;

                    if (isJumpPad)
                    {
                        move.y = jumpIndicator ;
                        jumpPressed = false;
//                        isJumping = true;
                    }
                }

                
            }
            

            else
            {
                move += Physics.gravity * gravityModifier * Time.fixedDeltaTime;
            }
            collisionFlags = charControl.Move(move * Time.fixedDeltaTime);
        }





        Quaternion ClampRotationAroundXAxis(Quaternion q)
        {
            q.x /= q.w;
            q.y /= q.w;
            q.z /= q.w;
            q.w = 1.0f;

            float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);

            angleX = Mathf.Clamp(angleX, -90f, 90f);

            q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

            return q;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (collisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(charControl.velocity * 0.1f, hit.point, ForceMode.Impulse);
        }
    }
}
