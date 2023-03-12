using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class InputHandler : MonoBehaviour, Controls.IPlayerControllerActions
    {
        private static InputHandler instance;

        private Controls input;

        private InputHolder<Vector2> moveInput;
        private InputHolder<Vector2> lookInput;
        private InputHolder<bool> jumpInput;
        private InputHolder<bool> sprintInput;

        public static ref InputHolder<Vector2> MoveInput => ref GetInstance().moveInput;
        public static ref InputHolder<Vector2> LookInput => ref GetInstance().lookInput;
        public static ref InputHolder<bool> JumpInput => ref GetInstance().jumpInput;
        public static ref InputHolder<bool> SprintInput => ref GetInstance().sprintInput;

        private void Awake()
        {
            if (instance != null && instance != this)
                Destroy(this);
            
            DontDestroyOnLoad(this);
            input = new Controls(); 
            input.PlayerController.SetCallbacks(this);
            
            instance = this;
        }

        private static InputHandler GetInstance()
        {
            if (instance == null)
            {
                CreateInstance();
            }

            return instance;
        }

        private static void CreateInstance()
        {
            GameObject inputHandler = new GameObject("InputHandler");
            inputHandler.AddComponent<InputHandler>();
        }

        private void OnEnable()
        {
            input.Enable();
        }

        private void OnDisable()
        {
            input.Disable();
        }

        private void OnDestroy()
        {
            input.Dispose();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput.SetValue(context.ReadValue<Vector2>());
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
                jumpInput.SetValue(true);
            else if (!context.performed)
                jumpInput.SetValue(false);
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            if (context.started)
                sprintInput.SetValue(true);
            else if (!context.performed)
                sprintInput.SetValue(false);
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            if (Application.isFocused)
                lookInput.SetValue(context.ReadValue<Vector2>());
        }

        public struct InputHolder<T>
        {
            private T value;

            public T Value => value;

            public T Consume()
            {
                T returnValue = value;

                SetValue(default);

                return returnValue;
            }

            public void SetValue(T newValue)
            {
                value = newValue;
            }

            public static implicit operator T(InputHolder<T> holder)
            {
                return holder.value;
            }
        }
    }
}