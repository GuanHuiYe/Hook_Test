using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Hook_Test.Model
{
    class Hook
    {
        protected delegate int HookProc(int nCode, Int32 wParam, IntPtr lParam);

        //使用此功能，安裝了一個鉤子
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);


        //調用此函數卸載鉤子
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        protected static extern bool UnhookWindowsHookEx(int idHook);


        //使用此功能，通過信息鉤子繼續下一個鉤子
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        protected static extern int CallNextHookEx(int idHook, int nCode, Int32 wParam, IntPtr lParam);

        // 取得當前線程編號（線程鉤子需要用到）
        [DllImport("kernel32.dll")]
        protected static extern int GetCurrentThreadId();

        //使用WINDOWS API函數代替獲取當前實例的函數,防止鉤子失效
        [DllImport("kernel32.dll")]
        protected static extern IntPtr GetModuleHandle(string name);

    }
}
