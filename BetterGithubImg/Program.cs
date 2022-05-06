using System.Diagnostics;
using System.Media;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

class InterceptKeys
{
    private static readonly Regex MarkdownImgRegex = new Regex(@"\!\[[a-zA-Z_-]+\]\((https:.+)\)");
    private static readonly Regex MarkdownLinkRegex = new Regex(@"\[([^\]]+)\]\((https:.+)\)"); //[/enterprise/custom-messages](https://11112-enterprise.stackoverflow.cloud/enterprise/custom-messages) 
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;

    [STAThread]
    public static void Main()
    {
        _hookID = SetHook(_proc);
        Application.Run();
        UnhookWindowsHookEx(_hookID);
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(
        int nCode, IntPtr wParam, IntPtr lParam);

    private static IntPtr HookCallback(
        int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Debug.WriteLine((Keys)vkCode);
                if ((Keys)vkCode == Keys.F4)
                {
                    //![image](https://user-images.githubusercontent.com/4502154/142855066-c3258e9b-3a26-4855-8e83-5a5d6754536e.png)
                    var clipboardText = Clipboard.GetText();

                    var match = MarkdownImgRegex.Match(clipboardText);
                    if (match.Success)
                    {
                        string imgHtmlTag = $"<img src=\"{match.Groups[1]}\" width=100 />";
                        Clipboard.SetText(imgHtmlTag);
                        SystemSounds.Hand.Play();
                    }
                    else
                    {
                        match = MarkdownLinkRegex.Match(clipboardText);
                        if (match.Success)
                        {
                            string imgHtmlTag = $"<a href=\"{match.Groups[2]}\">{match.Groups[1]}</a>";
                            Clipboard.SetText(imgHtmlTag);
                            SystemSounds.Hand.Play();
                        }
                        else
                        {
                            SystemSounds.Exclamation.Play();
                        }
                    }
                }
            }
        }
        catch(Exception)
        {
            SystemSounds.Exclamation.Play();
            SystemSounds.Exclamation.Play();
        }
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}