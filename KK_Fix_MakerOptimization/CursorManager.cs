using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KK_Fix_MakerOptimization
{
    public class CursorManager : MonoBehaviour
    {
        private bool mouseButtonDown0 = false;
        private bool mouseButtonDown1 = false;
        private WinCursor.Point lockPos;
        private bool cursorLocked = false;

        private void Update()
        {
            if (MakerOptimization.ManageCursor.Value && Application.isFocused)
            {
                if (!cursorLocked)
                {
                    if (GUIUtility.hotControl == 0 && !EventSystem.current.IsPointerOverGameObject())
                    {
                        bool mouseDown0 = Input.GetMouseButtonDown(0);
                        bool mouseDown1 = Input.GetMouseButtonDown(1);

                        if (mouseDown0 || mouseDown1)
                        {
                            if (mouseDown0) mouseButtonDown0 = true;
                            if (mouseDown1) mouseButtonDown1 = true;

                            Cursor.visible = false;
                            Cursor.lockState = CursorLockMode.Confined;
                            cursorLocked = true;
                            WinCursor.GetCursorPos(out lockPos);
                        }
                    }
                }

                if (cursorLocked)
                {
                    bool mouseUp0 = Input.GetMouseButtonUp(0);
                    bool mouseUp1 = Input.GetMouseButtonUp(1);

                    if ((mouseButtonDown0 || mouseButtonDown1) && (mouseUp0 || mouseUp1))
                    {
                        if (mouseUp0) mouseButtonDown0 = false;
                        if (mouseUp1) mouseButtonDown1 = false;

                        if (!mouseButtonDown0 && !mouseButtonDown1)
                        {
                            Cursor.lockState = CursorLockMode.None;
                            Cursor.visible = true;
                            cursorLocked = false;
                        }
                    }

                    if (cursorLocked)
                        WinCursor.SetCursorPos(lockPos.x, lockPos.y);
                }
            }
            else if (cursorLocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                cursorLocked = false;
            }
        }

        private static class WinCursor
        {
            [DllImport("user32.dll")]
            public static extern bool SetCursorPos(int X, int Y);

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
