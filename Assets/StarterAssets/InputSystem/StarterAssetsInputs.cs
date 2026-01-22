using FMODUnity;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
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

		[Header("Sfx Settings")]

        public float stepRate;
        private float nextStepTime;



        private void Update()
        {
            if(FirstPersonController.Grounded)
			{
				if(Mathf.Abs(move.x) >0 || Mathf.Abs(move.y) >0)
				{
					PlayFootStep();
				}
			}
        }

        void PlayFootStep()
		{
			if (sprint && Time.time >= nextStepTime)
			{
				stepRate = 0.25f;
                RuntimeManager.PlayOneShot(ReverbManager.RevInstance.runSFX);
				nextStepTime = Time.time + stepRate;

            }
			if(!sprint && Time.time >= nextStepTime) 
			{
				stepRate = 0.5f;
                RuntimeManager.PlayOneShot(ReverbManager.RevInstance.walkSFX);
                nextStepTime = Time.time + stepRate;
            }
				
			
		}


#if ENABLE_INPUT_SYSTEM
        public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}
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
		
		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}