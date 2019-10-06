using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;

namespace IllusionFixes
{
    public class CursorManager : MonoBehaviour
    {
        private bool _mouseButtonDown0;
        private bool _mouseButtonDown1;
        private WinCursor.Point _lockPos;
        private bool _cursorLocked;

        internal void Update()
        {
            if (MakerOptimizations.ManageCursor.Value && Application.isFocused)
            {
                if (!_cursorLocked)
                {
                    if (GUIUtility.hotControl == 0 && !EventSystem.current.IsPointerOverGameObject())
                    {
                        bool mouseDown0 = Input.GetMouseButtonDown(0);
                        bool mouseDown1 = Input.GetMouseButtonDown(1);

                        if (mouseDown0 || mouseDown1)
                        {
                            if (mouseDown0) _mouseButtonDown0 = true;
                            if (mouseDown1) _mouseButtonDown1 = true;

                            Cursor.visible = false;
                            Cursor.lockState = CursorLockMode.Confined;
                            _cursorLocked = true;
                            WinCursor.GetCursorPos(out _lockPos);
                        }
                    }
                }

                if (_cursorLocked)
                {
                    bool mouseUp0 = Input.GetMouseButtonUp(0);
                    bool mouseUp1 = Input.GetMouseButtonUp(1);

                    if ((_mouseButtonDown0 || _mouseButtonDown1) && (mouseUp0 || mouseUp1))
                    {
                        if (mouseUp0) _mouseButtonDown0 = false;
                        if (mouseUp1) _mouseButtonDown1 = false;

                        if (!_mouseButtonDown0 && !_mouseButtonDown1)
                        {
                            Cursor.lockState = CursorLockMode.None;
                            Cursor.visible = true;
                            _cursorLocked = false;
                        }
                    }

                    if (_cursorLocked)
                        WinCursor.SetCursorPos(_lockPos.x, _lockPos.y);
                }
            }
            else if (_cursorLocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                _cursorLocked = false;
            }
        }

        private static class WinCursor
        {
            [DllImport("user32.dll")]
            public static extern bool SetCursorPos(int x, int y);

            [DllImport("user32.dll")]
            public static extern bool GetCursorPos(out Point pos);

            [StructLayout(LayoutKind.Sequential)]
            public struct Point
            {
                public int x;
                public int y;

                public static implicit operator Vector2(Point point) => new Vector2(point.x, point.y);
            }
        }
    }
}
