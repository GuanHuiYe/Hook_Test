#define wpf

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
    class Keyboard_Hook : Hook
    {
        private bool _keyboard_hook_handle = false;
        public bool keyboard_hook_handle { get { return _keyboard_hook_handle; } set { _keyboard_hook_handle = value; } }

        public event KeyEvent KeyDownEvent;
        public event KeyEvent KeyUpEvent;

        public delegate void KeyEvent(object sender, KeyEventArgs e);

        private static int hKeyboardHook = 0; //聲明鍵盤鉤子處理的初始值
        public int keyboard_handler { get { return hKeyboardHook; } }

        //值在Microsoft SDK的Winuser.h裡查詢
        // http://www.bianceng.cn/Programming/csharp/201410/45484.htm
        public const int WH_KEYBOARD = 2; //local
        public const int WH_KEYBOARD_LL = 13;   //線程鍵盤鉤子監聽鼠標消息設為2，全局鍵盤監聽鼠標消息設為13


        HookProc KeyboardHookProcedure; //聲明KeyboardHookProcedure作為HookProc類型

        //鍵盤結構
        [StructLayout(LayoutKind.Sequential)]
        public struct KeyboardHookStruct
        {
            public int VirtualKeyCode;
            public int ScanCode;
            public int Flags;
            public int Time;
            public int ExtraInfo;
        }

        private bool is_global;

        public bool Start(bool is_global = false)
        {
            this.is_global = is_global;
            // 安裝鍵盤鉤子
            if (hKeyboardHook == 0)
            {
                KeyboardHookProcedure = new HookProc(KeyboardHookProc);

                using (Process curProcess = Process.GetCurrentProcess())
                {
                    using (ProcessModule curModule = curProcess.MainModule)
                    {
                        if (is_global) hKeyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyboardHookProcedure, GetModuleHandle(curModule.ModuleName), 0);
                        else hKeyboardHook = SetWindowsHookEx(WH_KEYBOARD, KeyboardHookProcedure, IntPtr.Zero, GetCurrentThreadId());
                    }
                }

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

            KeyEventArgs e = null;
            if (is_global)

            {
                int wParam_Int32 = Convert.ToInt32(wParam);
                if (nCode >= 0)
                {
                    KeyboardHookStruct keyboardHookStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));
                    if (KeyDownEvent != null && (wParam_Int32 == WM_KEYDOWN || wParam_Int32 == WM_SYSKEYDOWN))
                    {
                        e = new KeyEventArgs(keyboardHookStruct.VirtualKeyCode);
                        KeyDownEvent(null, e);
                    }
                    else if (KeyUpEvent != null && (wParam_Int32 == WM_KEYUP || wParam_Int32 == WM_SYSKEYUP))
                    {
                        e = new KeyEventArgs(keyboardHookStruct.VirtualKeyCode);
                        KeyUpEvent(null, e);
                    }
                }


            }
            else
            {
                e = new KeyEventArgs(wParam);

                // 當按鍵按下及鬆開時都會觸發此函式，這裡只處理鍵盤按下的情形。
                bool isPressed = (lParam.ToInt32() & 0x80000000) == 0;

                if (nCode >= 0)
                {
                    if (KeyDownEvent != null && isPressed) KeyDownEvent(null, e);
                    else if (KeyUpEvent != null && !isPressed) KeyUpEvent(null, e);
                }

            }

            if (keyboard_hook_handle || (e != null && e.Handled)) return -1;
            return CallNextHookEx(hKeyboardHook, nCode, wParam, lParam);
        }

        /// <summary>
        /// 提供 GlobalKeyDown 或 GlobalKeyUp 事件的資料。
        /// </summary>
        public class KeyEventArgs : EventArgs
        {
            /// <summary>
            /// 取得或設定值，指出是否處理事件。
            /// </summary>
            public bool Handled { get; set; }
#if !wpf
            /// <summary>
            /// 取得值，虛擬鍵盤碼的System.Windows.Forms.Keys表示。
            /// </summary>
            public System.Windows.Forms.Keys Keys { get { return (System.Windows.Forms.Keys)VirtualKeyCode; } }
#endif
            /// <summary>
            /// 取得值，虛擬鍵盤碼的System.Windows.Input.Key表示。
            /// </summary>
            public System.Windows.Input.Key Key { get { return System.Windows.Input.KeyInterop.KeyFromVirtualKey(VirtualKeyCode); } }
            /// <summary>
            /// 取得值，指出是否按下 ALT 鍵。
            /// </summary>
            public bool Alt
            {
                get
                {
#if wpf
                    return KeyIsDown((int)KeyInterop.VirtualKeyFromKey(Key.LeftAlt)) || KeyIsDown((int)KeyInterop.VirtualKeyFromKey(Key.RightAlt));
#else
                     return KeyIsDown((int)System.Windows.Forms.Keys.LMenu) || KeyIsDown((int)System.Windows.Forms.Keys.RMenu);
#endif


                }

            }
            /// <summary>
            /// 取得值，指出是否按下 CTRL 鍵。
            /// </summary>
            public bool Control
            {
                get
                {
#if wpf
                    return KeyIsDown((int)KeyInterop.VirtualKeyFromKey(Key.LeftCtrl)) || KeyIsDown((int)KeyInterop.VirtualKeyFromKey(Key.RightCtrl));
#else
        return KeyIsDown((int)System.Windows.Forms.Keys.LControlKey) || KeyIsDown((int)System.Windows.Forms.Keys.RControlKey);                     
#endif


                }
            }
            /// <summary>
            /// 取得值，指出是否按下 SHIFT 鍵。
            /// </summary>
            public bool Shift
            {
                get
                {
#if wpf
                    return KeyIsDown((int)KeyInterop.VirtualKeyFromKey(Key.LeftShift)) || KeyIsDown((int)KeyInterop.VirtualKeyFromKey(Key.RightShift));
#else
              return KeyIsDown((int)System.Windows.Forms.Keys.LShiftKey) || KeyIsDown((int)System.Windows.Forms.Keys.RShiftKey);                
#endif

                }
            }
            /// <summary>
            /// 取得值，引發事件的虛擬鍵盤碼。
            /// </summary>
            public int VirtualKeyCode { get; private set; }
            internal KeyEventArgs(int virtualKey)
            {
                this.Handled = false;
                this.VirtualKeyCode = virtualKey;
            }

            private static bool KeyIsDown(int KeyCode)
            {
                if ((GetKeyState(KeyCode) & 0x80) == 0x80)
                    return true;
                else
                    return false;
            }
        }

        ~Keyboard_Hook()
        {
            Stop();
        }
    }
}
