using System;
using RS.GamePlay;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RS
{
	public class AssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

		private PlayerInput m_playerInput;
		private Player m_player;
		private Raycast m_raycast;

		private void Awake()
		{
			m_playerInput = GetComponent<PlayerInput>();
			m_player = GetComponent<Player>();
			m_raycast = GetComponent<Raycast>();
			var actions = m_playerInput.actions;

			actions["Move"].performed += OnMove;
			actions["Move"].canceled  += OnMove;

			actions["Look"].performed += OnLook;
			actions["Look"].canceled  += OnLook;

			actions["Jump"].performed += OnJump;
			actions["Jump"].canceled  += OnJump;

			actions["Sprint"].performed += OnSprint;
			actions["Sprint"].canceled  += OnSprint;
			
			actions["Item"].performed += m_player.OnItem;
			// actions["Put"].performed += m_raycast.OnPut;
			actions["Attack"].performed += m_raycast.OnAttack;
			
			
		}

		private void OnDestroy()
		{
			var actions = m_playerInput.actions;

            actions["Move"].performed -= OnMove;
            actions["Move"].canceled  -= OnMove;

            actions["Look"].performed -= OnLook;
            actions["Look"].canceled  -= OnLook;

            actions["Jump"].performed -= OnJump;
            actions["Jump"].canceled  -= OnJump;

            actions["Sprint"].performed -= OnSprint;
            actions["Sprint"].canceled  -= OnSprint;
            
            actions["Item"].performed -= m_player.OnItem;
            actions["Attack"].performed -= m_raycast.OnAttack;
            // actions["Put"].performed -= m_raycast.OnPut;
		}

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputAction.CallbackContext context)
		{
			if (context.performed || context.canceled)
			{
				MoveInput(context.ReadValue<Vector2>());
			}
		}

		public void OnLook(InputAction.CallbackContext context)
		{
			if (cursorInputForLook && (context.performed || context.canceled))
			{
				LookInput(context.ReadValue<Vector2>());
			}
		}

		public void OnJump(InputAction.CallbackContext context)
		{
			if (context.performed)
			{
				JumpInput(true);
			}
			else if (context.canceled)
			{
				JumpInput(false);
			}
		}

		public void OnSprint(InputAction.CallbackContext context)
		{
			if (context.performed)
			{
				SprintInput(true);
			}
			else if (context.canceled)
			{
				SprintInput(false);
			}
		}
		// public void OnMove(InputValue value)
		// {
		// 	MoveInput(value.Get<Vector2>());
		// }
		//
		// public void OnLook(InputValue value)
		// {
		// 	if(cursorInputForLook)
		// 	{
		// 		LookInput(value.Get<Vector2>());
		// 	}
		// }
		//
		// public void OnJump(InputValue value)
		// {
		// 	JumpInput(value.isPressed);
		// }
		//
		// public void OnSprint(InputValue value)
		// {
		// 	SprintInput(value.isPressed);
		// }
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		// private void OnApplicationFocus(bool hasFocus)
		// {
		// 	SetCursorState(cursorLocked);
		// }

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}