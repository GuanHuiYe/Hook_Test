using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows;
using System.Diagnostics;


namespace Hook_Test.Model
{
    /// <summary>
    /// 鍵盤鉤子
    /// [以下代碼來自某網友，並非本人原創]
    /// </summary>
    class Keyboard_Hook:Hook
    {
        private bool _keyboard_hook_handle = false;
        public bool keyboard_hook_handle { get { return _keyboard_hook_handle; } set { _keyboard_hook_handle = value; } }

        public event KeyEvent KeyDownEvent;
        public event KeyEvent KeyPressEvent;
        public event KeyEvent KeyUpEvent;

        public delegate void KeyEvent(object sender, Key key);

        private static int hKeyboardHook = 0; //聲明鍵盤鉤子處理的初始值
        public int keyboard_handler { get{return hKeyboardHook;}  }

        //值在Microsoft SDK的Winuser.h裡查詢
        // http://www.bianceng.cn/Programming/csharp/201410/45484.htm
        public const int WH_KEYBOARD = 2; //local
        public const int WH_KEYBOARD_LL = 13;   //線程鍵盤鉤子監聽鼠標消息設為2，全局鍵盤監聽鼠標消息設為13


        HookProc KeyboardHookProcedure; //聲明KeyboardHookProcedure作為HookProc類型

        //鍵盤結構
        [StructLayout(LayoutKind.Sequential)]
        public class KeyboardHookStruct
        {
            public int vkCode;  //定一個虛擬鍵碼。該代碼必須有一個價值的范圍1至254
            public int scanCode; // 指定的硬件掃描碼的關鍵
            public int flags;  // 鍵標志
            public int time; // 指定的時間戳記的這個訊息
            public int dwExtraInfo; // 指定額外信息相關的信息
        }


        public bool Start(bool is_global=false)
        {
            // 安裝鍵盤鉤子
            if (hKeyboardHook == 0)
            {
                KeyboardHookProcedure = new HookProc(KeyboardHookProc);

                using (Process curProcess = Process.GetCurrentProcess()) {
                    using (ProcessModule curModule = curProcess.MainModule)
                    {
                        if (is_global) hKeyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyboardHookProcedure, GetModuleHandle(curModule.ModuleName), 0);
                        else hKeyboardHook = SetWindowsHookEx(WH_KEYBOARD, KeyboardHookProcedure, IntPtr.Zero, GetCurrentThreadId());
                    }
                }

              
               
                //hKeyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyboardHookProcedure, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]), 0);


                //************************************
                //鍵盤線程鉤子
                //SetWindowsHookEx( 2,KeyboardHookProcedure, IntPtr.Zero, GetCurrentThreadId());//指定要監聽的線程idGetCurrentThreadId(),
                //鍵盤全局鉤子,需要引用空間(using System.Reflection;)
                //SetWindowsHookEx( 13,MouseHookProcedure,Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]),0);
                //
                //關於SetWindowsHookEx (int idHook, HookProc lpfn, IntPtr hInstance, int threadId)函數將鉤子加入到鉤子鏈表中，說明一下四個參數：
                //idHook 鉤子類型，即確定鉤子監聽何種消息，上面的代碼中設為2，即監聽鍵盤消息並且是線程鉤子，如果是全局鉤子監聽鍵盤消息應設為13，
                //線程鉤子監聽鼠標消息設為7，全局鉤子監聽鼠標消息設為14。lpfn 鉤子子程的地址指針。如果dwThreadId參數為0 或是一個由別的進程創建的
                //線程的標識，lpfn必須指向DLL中的鉤子子程。 除此以外，lpfn可以指向當前進程的一段鉤子子程代碼。鉤子函數的入口地址，當鉤子鉤到任何
                //消息後便調用這個函數。hInstance應用程序實例的句柄。標識包含lpfn所指的子程的DLL。如果threadId 標識當前進程創建的一個線程，而且子
                //程代碼位於當前進程，hInstance必須為NULL。可以很簡單的設定其為本應用程序的實例句柄。threaded 與安裝的鉤子子程相關聯的線程的標識符
                //如果為0，鉤子子程與所有的線程關聯，即為全局鉤子
                //************************************
                //如果SetWindowsHookEx失敗


                if (hKeyboardHook == 0)
                {
                    Stop();
                    return false;
                }
                return true;
            }
            return false;
        }

        public bool Stop()
        {
            bool retKeyboard = true;
            
            if (hKeyboardHook != 0)
            {
                retKeyboard = UnhookWindowsHookEx(hKeyboardHook);
                hKeyboardHook = 0;
            }

            if (!(retKeyboard)) return false;
            return true;
        }

        //ToAscii職能的轉換指定的虛擬鍵碼和鍵盤狀態的相應字符或字符
        [DllImport("user32")]
        public static extern int ToAscii(int uVirtKey, //[in] 指定虛擬關鍵代碼進行翻譯。
                                         int uScanCode, // [in] 指定的硬件掃描碼的關鍵須翻譯成英文。高階位的這個值設定的關鍵，如果是（不壓）
                                         byte[] lpbKeyState, // [in] 指針，以256字節數組，包含當前鍵盤的狀態。每個元素（字節）的數組包含狀態的一個關鍵。如果高階位的字節是一套，關鍵是下跌（按下）。在低比特，如果設置表明，關鍵是對切換。在此功能，只有肘位的CAPS LOCK鍵是相關的。在切換狀態的NUM個鎖和滾動鎖定鍵被忽略。
                                         byte[] lpwTransKey, // [out] 指針的緩沖區收到翻譯字符或字符。
                                         int fuState); // [in] Specifies whether a menu is active. This parameter must be 1 if a menu is active, or 0 otherwise.

        //獲取按鍵的狀態
        [DllImport("user32")]
        public static extern int GetKeyboardState(byte[] pbKeyState);


        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern short GetKeyState(int vKey);

        private const int WM_KEYDOWN = 0x100;//KEYDOWN
        private const int WM_KEYUP = 0x101;//KEYUP
        private const int WM_SYSKEYDOWN = 0x104;//SYSKEYDOWN
        private const int WM_SYSKEYUP = 0x105;//SYSKEYUP

        private int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
        {
            // 偵聽鍵盤事件
            if ((nCode >= 0) && (KeyDownEvent != null || KeyUpEvent != null || KeyPressEvent != null))
            {               
                KeyboardHookStruct MyKeyboardHookStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));

                Key keyData = KeyInterop.KeyFromVirtualKey(MyKeyboardHookStruct.vkCode);
           
                // raise KeyDown
                if (KeyDownEvent != null && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
                {
                    KeyDownEvent(this, keyData);
                }

                //鍵盤按下
                if (KeyPressEvent != null && wParam == WM_KEYDOWN)
                {
                    byte[] keyState = new byte[256];
                    GetKeyboardState(keyState);

                    byte[] inBuffer = new byte[2];
                    if (ToAscii(MyKeyboardHookStruct.vkCode, MyKeyboardHookStruct.scanCode, keyState, inBuffer, MyKeyboardHookStruct.flags) == 1)
                    {                   
                        KeyPressEvent(this, keyData);
                    }
                }

                // 鍵盤抬起
                if (KeyUpEvent != null && (wParam == WM_KEYUP || wParam == WM_SYSKEYUP))
                {                  
                    KeyUpEvent(this, keyData);
                }

            }
            //如果返回1，則結束消息，這個消息到此為止，不再傳遞。
            //如果返回0或調用CallNextHookEx函數則消息出了這個鉤子繼續往下傳遞，也就是傳給消息真正的接受者
            if (keyboard_hook_handle) return 1;
            else return CallNextHookEx(hKeyboardHook, nCode, wParam, lParam);
        }
        ~Keyboard_Hook()
        {
            Stop();
        }
    }
}
