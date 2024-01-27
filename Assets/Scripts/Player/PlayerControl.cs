using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.PanicBuying.Character;

namespace Unity.PanicBuying.Character
{
    public class PlayerControl : NetworkBehaviour
    {
        [Header("Movement Speed")]
        public float moveSpeed;
        public float moveMultiplier;
        public float jumpForce;
        public float gravityScale;

        [Header("Jump & Ground Check")]
        public float jumpCooldownTime;
        public float playerHeight;
        bool isGrounded; 

        public Transform orientation;
        public Transform playerObj;
        public GameObject tpsCamera;
        public GameObject fpsCamera;

        float horizontalInput;
        float verticalInput;

        bool jumpAvailable = true;

        Vector3 moveDir;

        Rigidbody rbody;

        // Start is called before the first frame update
        void Start()
        {
            rbody = GetComponent<Rigidbody>();
            Transform spawnPoint = GameObject.Find("PlayerSpawnPoint").transform;
            transform.position = spawnPoint.position;

            if (IsOwner)
            {
                //// setting TPS cam
                //GameObject cam = Instantiate(tpsCamera);
                //CinemachineFreeLook cineCam = cam.GetComponent<CinemachineFreeLook>();
                //cineCam.Follow = transform;
                //cineCam.LookAt = transform;
                //TPSCameraController camController = cam.GetComponent<TPSCameraController>();
                //camController.orientation = orientation;
                //camController.player = transform;
                //camController.playerObj = playerObj;

                // setting FPS cam
                GameObject cam = Instantiate(fpsCamera);
                FPSCameraController camController = cam.GetComponent<FPSCameraController>();
                camController.orientation = orientation;
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f);

            if (Input.GetKeyDown(KeyCode.Space) && isGrounded && jumpAvailable)
            {
                rbody.velocity = new Vector3(rbody.velocity.x, 0f, rbody.velocity.z);
                rbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

                jumpAvailable = false;
                Invoke("JumpCooldown", jumpCooldownTime);
            }
            GetInput();

        }

        private void FixedUpdate()
        {
            if (IsOwner)
            {
                GiveGravity();
                MovePlayer();
                SpeedControl();
            }
        }

        private void GiveGravity()
        {
            rbody.AddForce(Vector3.down * gravityScale, ForceMode.Force);
        }

        private void GetInput()
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");
        }

        private void MovePlayer()
        {
            moveDir = orientation.forward * verticalInput + orientation.right * horizontalInput;

            rbody.AddForce(moveDir.normalized * moveSpeed * moveMultiplier, ForceMode.Force);
        }

        private void SpeedControl()
        {
            Vector3 moveVelocity = new Vector3(rbody.velocity.x, 0f, rbody.velocity.z);

            if (moveVelocity.magnitude > moveSpeed)
            {
                Vector3 controlledVelocity = moveVelocity.normalized * moveSpeed;
                rbody.velocity = new Vector3(controlledVelocity.x, rbody.velocity.y, controlledVelocity.z);
            }
        }

        private void JumpCooldown()
        {
            jumpAvailable = true;
        }
    }
}