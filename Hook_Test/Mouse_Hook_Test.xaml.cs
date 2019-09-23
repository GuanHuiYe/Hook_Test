using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Hook_Test.Model;

namespace Hook_Test
{
    /// <summary>
    /// Mouse_Hook_Test.xaml 的互動邏輯
    /// </summary>
    public partial class Mouse_Hook_Test : UserControl
    {
        public Mouse_Hook_Test()
        {
           mouse_status= new Mouse_Hook.MouseEventArgs();
            InitializeComponent();
            this.DataContext = mouse_status;
        }

        Mouse_Hook mouse_hook=new Mouse_Hook();

        Mouse_Hook.MouseEventArgs mouse_status;

        private void hook_event(object sender, Mouse_Hook.MouseEventArgs mouse)
        {
            //display.Text += "press " + key + Environment.NewLine;
            mouse_status = mouse;
            this.DataContext = mouse_status;
        }

        private void double_click_hook(object sender, Mouse_Hook.MouseEventArgs mouse)
        {
            display.Text += "Double Click " + mouse.Button + "\r\n";
        }

        private void set_hook_event()
        {
            mouse_hook.GlobalMouseDown += new Mouse_Hook.MouseEvent(hook_event);
            mouse_hook.GlobalMouseUp += new Mouse_Hook.MouseEvent(hook_event);
            mouse_hook.GlobalMouseClick += new Mouse_Hook.MouseEvent(hook_event);
            mouse_hook.GlobalMouseDoubleClick += new Mouse_Hook.MouseEvent(hook_event);
            mouse_hook.GlobalMouseDoubleClick += new Mouse_Hook.MouseEvent(double_click_hook);
            mouse_hook.GlobalMouseWheel += new Mouse_Hook.MouseEvent(hook_event);
            mouse_hook.GlobalMouseMove += new Mouse_Hook.MouseEvent(hook_event);           
        }

        private void set_local_hook(object sender, RoutedEventArgs e)
        {

            if (mouse_hook.HookHandle == 0)
            {

                set_hook_event();
                display.Text += "設置 Local Hook " + (mouse_hook.Start() ? "成功" : "失敗") + "!\r\n";
            }
            else
            {
                display.Text += "移除 Local Hook " + (mouse_hook.Stop() ? "成功" : "失敗") + "!\r\n";
                mouse_hook = new Mouse_Hook();
            }
            local_hook_btn.Content = mouse_hook.HookHandle == 0 ? "設置 Local Hook" : "移除 Global Hook";
            global_hook_btn.IsEnabled = mouse_hook.HookHandle == 0 ? true : false;
        }

        private void set_global_hook(object sender, RoutedEventArgs e)
        {
            if (mouse_hook.HookHandle == 0)
            {

                set_hook_event();
                display.Text += "設置 Global Hook " + (mouse_hook.Start(true) ? "成功" : "失敗") + "!\r\n";
            }
            else
            {
                display.Text += "移除 Global Hook " + (mouse_hook.Stop() ? "成功" : "失敗") + "!\r\n";
                mouse_hook = new Mouse_Hook();
            }
            global_hook_btn.Content = mouse_hook.HookHandle == 0 ? "設置 Global Hook" : "移除 Global Hook";
            local_hook_btn.IsEnabled = mouse_hook.HookHandle == 0 ? true : false;
        }


    }
}
