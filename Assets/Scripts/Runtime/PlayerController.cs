using UnityEngine;
using UnityEngine.InputSystem;

namespace RS
{
    public class PlayerController : MonoBehaviour
    {
        public float speed = 5.0f;
        public float rotationSpeed = 2.0f;
        public float jumpHeight = 2.0f;
        public float gravity = -9.8f;

        public PlayerInput playerInput;

        private CharacterController m_controller;
        
        private Vector2 m_moveInput;
        private float m_yVelocity;
        
        private void Start()
        {
            m_controller = GetComponent<CharacterController>();
        }

        private void OnJump(InputValue value)
        {
            if (m_controller.isGrounded)
            {
                m_yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        private void OnMove(InputValue value)
        {
            m_moveInput = value.Get<Vector2>();
        }
        
        private void OnLook(InputValue value)
        {
            var direction = value.Get<Vector2>();
            var rotation = new Vector3(0, direction.x * rotationSpeed, 0);
            transform.Rotate(rotation);
        }

        private void Update()
        {
            var move = new Vector3(m_moveInput.x, 0, m_moveInput.y);
            move = transform.TransformDirection(move) * speed;

            if (m_controller.isGrounded && m_yVelocity < 0)
            {
                m_yVelocity = -2f; // 保持贴地
            }
            m_yVelocity += gravity * Time.deltaTime;
            move.y = m_yVelocity;

            m_controller.Move(move * Time.deltaTime);
        }
    }
}