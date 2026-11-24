using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Rafidain
{
    public enum GameAction { Attack = 0, HeavyAttack = 1, Dodge = 2, LockOn = 3, Interact = 4, Heal = 5 }

    /// <summary>
    /// One place that reads keyboard/mouse, gamepad and on-screen touch controls.
    /// Button presses are buffered for a short time (souls-like input buffering).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : MonoBehaviour
    {
        public static PlayerInputReader Instance { get; private set; }

        [Header("Sensitivity")]
        public float mouseSensitivity = 0.12f;
        public float gamepadLookSpeed = 160f;
        public float touchLookSensitivity = 0.18f;

        [Header("Buffer")]
        public float bufferTime = 0.25f;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool PausePressed { get; private set; }

        const int ActionCount = 6;
        readonly float[] lastPress = new float[ActionCount];

        Vector2 mobileMove;
        Vector2 mobileLook;
        bool mobileSprint;
        float padEastDownTime = -1f;

        void Awake()
        {
            Instance = this;
            for (int i = 0; i < ActionCount; i++) lastPress[i] = -10f;
        }

        public void Press(GameAction a) => lastPress[(int)a] = Time.time;

        /// <summary>Returns true once if the action was pressed within the buffer window.</summary>
        public bool Consume(GameAction a)
        {
            int i = (int)a;
            if (Time.time - lastPress[i] <= bufferTime)
            {
                lastPress[i] = -10f;
                return true;
            }
            return false;
        }

        public void SetMobileMove(Vector2 v) => mobileMove = v;
        public void AddMobileLook(Vector2 delta) => mobileLook += delta * touchLookSensitivity;
        public void SetMobileSprint(bool held) => mobileSprint = held;

        void Update()
        {
            Vector2 move = Vector2.zero;
            Vector2 look = Vector2.zero;
            bool sprint = false;
            PausePressed = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            var pad = Gamepad.current;

            if (kb != null)
            {
                move.x += (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
                move.y += (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
                sprint |= kb.leftShiftKey.isPressed;
                if (kb.spaceKey.wasPressedThisFrame) Press(GameAction.Dodge);
                if (kb.eKey.wasPressedThisFrame) Press(GameAction.Interact);
                if (kb.rKey.wasPressedThisFrame) Press(GameAction.Heal);
                if (kb.qKey.wasPressedThisFrame) Press(GameAction.LockOn);
                if (kb.escapeKey.wasPressedThisFrame) PausePressed = true;
            }

            if (mouse != null)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                    look += mouse.delta.ReadValue() * mouseSensitivity;
                if (mouse.leftButton.wasPressedThisFrame) Press(GameAction.Attack);
                if (mouse.rightButton.wasPressedThisFrame) Press(GameAction.HeavyAttack);
                if (mouse.middleButton.wasPressedThisFrame) Press(GameAction.LockOn);
            }

            if (pad != null)
            {
                move += pad.leftStick.ReadValue();
                look += pad.rightStick.ReadValue() * gamepadLookSpeed * Time.deltaTime;

                // B / Circle: tap = dodge, hold = sprint (like Elden Ring)
                if (pad.buttonEast.wasPressedThisFrame) padEastDownTime = Time.time;
                if (pad.buttonEast.isPressed && padEastDownTime >= 0f && Time.time - padEastDownTime > 0.3f) sprint = true;
                if (pad.buttonEast.wasReleasedThisFrame)
                {
                    if (padEastDownTime >= 0f && Time.time - padEastDownTime <= 0.3f) Press(GameAction.Dodge);
                    padEastDownTime = -1f;
                }

                if (pad.rightShoulder.wasPressedThisFrame) Press(GameAction.Attack);
                if (pad.rightTrigger.wasPressedThisFrame) Press(GameAction.HeavyAttack);
                if (pad.rightStickButton.wasPressedThisFrame) Press(GameAction.LockOn);
                if (pad.buttonNorth.wasPressedThisFrame) Press(GameAction.Interact);
                if (pad.buttonWest.wasPressedThisFrame) Press(GameAction.Heal);
                if (pad.startButton.wasPressedThisFrame) PausePressed = true;
            }
#else
            move.x = Input.GetAxisRaw("Horizontal");
            move.y = Input.GetAxisRaw("Vertical");
            if (Cursor.lockState == CursorLockMode.Locked)
                look += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f * mouseSensitivity;
            sprint |= Input.GetKey(KeyCode.LeftShift);
            if (Input.GetKeyDown(KeyCode.Space)) Press(GameAction.Dodge);
            if (Input.GetKeyDown(KeyCode.E)) Press(GameAction.Interact);
            if (Input.GetKeyDown(KeyCode.R)) Press(GameAction.Heal);
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(2)) Press(GameAction.LockOn);
            if (Input.GetMouseButtonDown(0)) Press(GameAction.Attack);
            if (Input.GetMouseButtonDown(1)) Press(GameAction.HeavyAttack);
            if (Input.GetKeyDown(KeyCode.Escape)) PausePressed = true;
#endif

            move += mobileMove;
            look += mobileLook;
            mobileLook = Vector2.zero;
            sprint |= mobileSprint;

            Move = Vector2.ClampMagnitude(move, 1f);
            Look = look;
            SprintHeld = sprint;
        }
    }
}
