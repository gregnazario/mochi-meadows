using System.Collections.Generic;
using UnityEngine;

namespace MochiMeadows.Inputs
{
    // Cross-platform input: keyboard + dynamic touch joystick.
    // The UI feeds joystick values here; keyboard is read directly.
    public class InputService : MonoBehaviour
    {
        public static InputService I;

        public Vector2 MoveAxis { get; private set; }
        Vector2 moveAxis;
        public Vector2 JoystickAxis;
        public bool TouchActive;
        public bool IsMobile => Application.isMobilePlatform;

        public bool ActionPressed { get; private set; }
        public bool ActionHeld { get; private set; }
        public bool ActPressed;   // set by the mobile Act button
        public bool ClosePressed { get; private set; }
        public bool MenuPressed { get; private set; }
        public int? HotbarPressed;
        public int? TapTileX, TapTileY;      // touch tap on world tile
        public bool TappedNpc;

        void Awake() { I = this; }

        void Update()
        {
            moveAxis = Vector2.zero;
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f)
                moveAxis.x = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.01f)
                moveAxis.y = Input.GetAxisRaw("Vertical");
            if (TouchActive && JoystickAxis.sqrMagnitude > 0.01f)
                moveAxis = JoystickAxis;
            if (moveAxis.sqrMagnitude > 1f) moveAxis.Normalize();
            MoveAxis = moveAxis;

            ActionPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || TappedNpc
                || Input.GetButtonDown("Submit");
            ActionHeld = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.E);
            ClosePressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown("Cancel");
            MenuPressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7) || Input.GetKeyDown(KeyCode.M);

            HotbarPressed = null;
            for (int i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { HotbarPressed = i; break; }
            }
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.01f) HotbarPressed = NextSlot(1);
            else if (scroll < -0.01f) HotbarPressed = NextSlot(-1);
        }

        int NextSlot(int dir)
        {
            var gm = Game.GameManager.I;
            if (gm == null) return 0;
            int next = gm.SelectedSlot + dir;
            next = (next + gm.Hotbar.Length) % gm.Hotbar.Length;
            return next;
        }

        public void ConsumeTap()
        {
            TappedNpc = false;
            TapTileX = null;
            TapTileY = null;
            ActPressed = false;
        }
    }
}
