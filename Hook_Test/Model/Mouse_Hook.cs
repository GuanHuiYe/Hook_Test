using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace Hook_Test.Model
{
    class Mouse_Hook : Hook
    {
        public delegate void MouseEvent(object sender, MouseEventArgs key);

        /// <summary>
        /// 當滑鼠按鍵壓下時引發此事件。
        /// </summary>
        public event MouseEvent GlobalMouseDown;
        /// <summary>
        /// 當滑鼠按鍵放開時引發此事件。
        /// </summary>
        public event MouseEvent GlobalMouseUp;
        /// <summary>
        /// 當滑鼠按鍵點擊時引發此事件。
        /// </summary>
        public event MouseEvent GlobalMouseClick;
        /// <summary>
        /// 當滑鼠按鍵連點兩次時引發此事件。
        /// </summary>
        public static event MouseEvent GlobalMouseDoubleClick;
        /// <summary>
        /// 當滑鼠滾輪滾動時引發此事件。
        /// </summary>
        public event MouseEvent GlobalMouseWheel;
        /// <summary>
        /// 當滑鼠移動時引發此事件。
        /// </summary>
        public event MouseEvent GlobalMouseMove;

        [DllImport("user32")]
        public static extern int GetDoubleClickTime();


        private bool is_global;

        private bool _mouse_hook_handle = false;
        public bool mouse_hook_handle { get { return _mouse_hook_handle; } set { _mouse_hook_handle = value; } }


        private static int _HookHandle = 0; //聲明鍵盤鉤子處理的初始值
        public int HookHandle { get { return _HookHandle; } }

        HookProc HookProcedure; //聲明KeyboardHookProcedure作為HookProc類型

        public const int WH_MOUSE = 7; //local
        public const int WH_MOUSE_LL = 14;   //global

        public bool Start(bool is_global = false)
        {
            this.is_global = is_global;

            if (_HookHandle == 0)
            {
                using (Process curProcess = Process.GetCurrentProcess())
                {
                    using (ProcessModule curModule = curProcess.MainModule)
                    {
                        HookProcedure = new HookProc(HookProc);
                        _HookHandle = SetWindowsHookEx(WH_MOUSE_LL, HookProcedure, GetModuleHandle(curModule.ModuleName), 0);

                    }
                }

                _DoubleClick_BackgroundWorker = new BackgroundWorker();
                _DoubleClick_BackgroundWorker.WorkerSupportsCancellation = true;
                _DoubleClick_BackgroundWorker.WorkerReportsProgress = true;
                BackgroundWorker_Interval = GetDoubleClickTime();
                _DoubleClick_BackgroundWorker.DoWork += new DoWorkEventHandler(DoubleClick_BackgroundWorkerDoWork);
                _DoubleClick_BackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(DoubleClick_BackgroundWorkerElapsed);

                GlobalMouseDown += OnMouseDown;

                if (_HookHandle == 0)
                {
                    return false;
                }
                else {
                    return true;
                }
                    
            }
            return false;
        }

        public bool Stop()
        {
            if (_HookHandle != 0)
            {
                bool ret = UnhookWindowsHookEx(_HookHandle);

                if (ret)
                {
                    _HookHandle = 0;
                    return true;
                }
                else
                {
                    return false;
                }


            }
            return false;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSELLHookStruct
        {
            public Point Point;
            public int MouseData;
            public int Flags;
            public int Time;
            public int ExtraInfo;
        }

        public const int WM_MOUSEMOVE = 0x200;
        public const int WM_LBUTTONDOWN = 0x201;
        public const int WM_RBUTTONDOWN = 0x204;
        public const int WM_MBUTTONDOWN = 0x207;
        public const int WM_LBUTTONUP = 0x202;
        public const int WM_RBUTTONUP = 0x205;
        public const int WM_MBUTTONUP = 0x208;
        public const int WM_LBUTTONDBLCLK = 0x203;
        public const int WM_RBUTTONDBLCLK = 0x206;
        public const int WM_MBUTTONDBLCLK = 0x209;
        public const int WM_MOUSEWHEEL = 0x020A;

        //記憶游標上一次的位置，避免MouseMove事件一直引發。
        private static int m_OldX = 0;
        private static int m_OldY = 0;

        //記憶上次MouseDonw的引發位置，如果與MouseUp的位置不同則不引發Click事件。
        private static int m_LastBTDownX = 0;
        private static int m_LastBTDownY = 0;
        /// <summary>
        /// 註冊Windows Hook時用到的委派方法，當全域事件發生時會執行這個方法，並提供全域事件資料。
        /// </summary>

        private int HookProc(int nCode, Int32 wParam, IntPtr lParam)
        {
            MouseEventArgs e = null;

            if (nCode >= 0)
            {
                int wParam_Int32 = Convert.ToInt32(wParam);
                MOUSELLHookStruct mouseHookStruct = (MOUSELLHookStruct)Marshal.PtrToStructure(lParam, typeof(MOUSELLHookStruct));

                short mouseDelta = 0;

                if (GlobalMouseWheel != null && wParam_Int32 == WM_MOUSEWHEEL)
                    mouseDelta = (short)((mouseHookStruct.MouseData >> 16) & 0xffff);

                e = new MouseEventArgs(wParam_Int32, mouseHookStruct.Point.X, mouseHookStruct.Point.Y, mouseDelta);

                if (GlobalMouseWheel != null && wParam_Int32 == WM_MOUSEWHEEL)
                    GlobalMouseWheel.Invoke(null, e);
                else if (GlobalMouseUp != null && (wParam_Int32 == WM_LBUTTONUP || wParam_Int32 == WM_RBUTTONUP || wParam_Int32 == WM_MBUTTONUP))
                {
                    GlobalMouseUp.Invoke(null, e);
                    if (GlobalMouseClick != null && (mouseHookStruct.Point.X == m_LastBTDownX && mouseHookStruct.Point.Y == m_LastBTDownY))
                        GlobalMouseClick.Invoke(null, e);
                }
                else if (GlobalMouseDown != null && (wParam_Int32 == WM_LBUTTONDOWN || wParam_Int32 == WM_RBUTTONDOWN || wParam_Int32 == WM_MBUTTONDOWN))
                {
                    m_LastBTDownX = mouseHookStruct.Point.X;
                    m_LastBTDownY = mouseHookStruct.Point.Y;
                    GlobalMouseDown.Invoke(null, e);
                }
                else if (GlobalMouseMove != null && (m_OldX != mouseHookStruct.Point.X || m_OldY != mouseHookStruct.Point.Y))
                {
                    m_OldX = mouseHookStruct.Point.X;
                    m_OldY = mouseHookStruct.Point.Y;
                    if (GlobalMouseMove != null)
                        GlobalMouseMove.Invoke(null, e);
                }
            }

            if (_mouse_hook_handle || (e != null && e.Handled))
                return -1;

            return CallNextHookEx(_HookHandle, nCode, wParam, lParam);

        }

        private static void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button.Equals(_LastClickedButton))
            {
                if (GlobalMouseDoubleClick != null)
                    GlobalMouseDoubleClick(null, e);
            }
            else
            {
                _DoubleClick_BackgroundWorker.RunWorkerAsync();
                _LastClickedButton = e.Button;
            }
        }
        private static Buttons _LastClickedButton;
        private static int BackgroundWorker_Interval;
        private static BackgroundWorker _DoubleClick_BackgroundWorker;
        private static void DoubleClick_BackgroundWorkerDoWork(object sender, DoWorkEventArgs e) {
            System.Threading.Thread.Sleep(BackgroundWorker_Interval);
            _DoubleClick_BackgroundWorker.ReportProgress(100);
        }
        private static void DoubleClick_BackgroundWorkerElapsed(object sender, RunWorkerCompletedEventArgs e)
        {       
            _LastClickedButton = Buttons.None;     
        }


        /// <summary>
        /// 提供 GlobalMouseUp、GlobalMouseDown 和 GlobalMouseMove 事件的資料。
        /// </summary>
        public class MouseEventArgs : EventArgs
        {
            /// <summary>
            /// 取得按下哪個滑鼠鍵的資訊。
            /// </summary>
            public Buttons Button { get; private set; }
            /// <summary>
            /// 取得滑鼠滾輪滾動時帶有正負號的刻度數乘以 WHEEL_DELTA 常數。 一個刻度是一個滑鼠滾輪的刻痕。
            /// </summary>
            public int Delta { get; private set; }
            /// <summary>
            /// 取得滑鼠在產生滑鼠事件期間的 X 座標。
            /// </summary>
            public int X { get; private set; }
            /// <summary>
            /// 取得滑鼠在產生滑鼠事件期間的 Y 座標。
            /// </summary>
            public int Y { get; private set; }
            internal MouseEventArgs()
            {
                Button = Buttons.None;
              
                this.X = 0;
                this.Y = 0;
                this.Delta = 0;
            }
            internal MouseEventArgs(int wParam, int x, int y, int delta)
            {
                Button = Buttons.None;
                switch (wParam)
                {
                    case (int)WM_LBUTTONDOWN:
                    case (int)WM_LBUTTONUP:
                        Button = Buttons.Left;
                        break;
                    case (int)WM_RBUTTONDOWN:
                    case (int)WM_RBUTTONUP:
                        Button = Buttons.Right;
                        break;
                    case (int)WM_MBUTTONDOWN:
                    case (int)WM_MBUTTONUP:
                        Button = Buttons.Middle;
                        break;
                }
                this.X = x;
                this.Y = y;
                this.Delta = delta;
            }
            private bool _Handled;
            /// <summary>
            /// 取得或設定值，指出是否處理事件。
            /// </summary>
            public bool Handled
            {
                get { return _Handled; }
                set { _Handled = value; }
            }
        }


        /// <summary>
        /// 指定定義按哪個滑鼠按鈕的常數。
        /// </summary>
        public enum Buttons
        {
            /// <summary>
            /// 不按任何滑鼠鍵。
            /// </summary>
            None,
            /// <summary>
            /// 按滑鼠左鍵。
            /// </summary>
            Left,
            /// <summary>
            /// 按滑鼠右鍵。
            /// </summary>
            Right,
            /// <summary>
            /// 按滑鼠中間鍵。
            /// </summary>
            Middle,
        }


        ~Mouse_Hook()
        {
            Stop();
        }

    }
}
