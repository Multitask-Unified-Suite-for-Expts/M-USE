using System.Runtime.InteropServices;
using UnityEngine;
public class WindowManager : MonoBehaviour
{
    [DllImport("user32.dll")]
    public static extern bool SetWindowText(System.IntPtr hwnd, System.String lpString);
    [DllImport("user32.dll")]
    public static extern System.IntPtr GetActiveWindow();
    void Start()
    {
        SetWindowText(GetActiveWindow(), "WindowTitle");
    }
}
