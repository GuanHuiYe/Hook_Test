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
    /// Keyboard_Hook_Test.xaml 的互動邏輯
    /// </summary>
    public partial class Keyboard_Hook_Test : UserControl
    {
        public Keyboard_Hook_Test()
        {
            InitializeComponent();
        }

        Keyboard_Hook keyboard_hook = new Keyboard_Hook();

        private void hook_KeyDown(object sender, Keyboard_Hook.KeyEventArgs e)
        {
            display.Text += "press " + e.Key + Environment.NewLine;
        }

        private void set_hook_event()
        {
            keyboard_hook.KeyDownEvent += hook_KeyDown;
        }

        private void set_local_hook(object sender, RoutedEventArgs e)
        {
           
            if (keyboard_hook.keyboard_handler == 0)
            {
               
                set_hook_event();
                display.Text += "設置 Local Hook " + (keyboard_hook.Start() ? "成功" : "失敗") + "!\r\n";
            }
            else
            {
                display.Text += "移除 Local Hook " + (keyboard_hook.Stop() ? "成功" : "失敗") + "!\r\n";
                keyboard_hook = new Keyboard_Hook();
            }
            local_hook_btn.Content = keyboard_hook.keyboard_handler == 0 ? "設置 Local Hook" : "移除 Global Hook";
            global_hook_btn.IsEnabled = keyboard_hook.keyboard_handler == 0 ? true : false;
        }

        private void set_global_hook(object sender, RoutedEventArgs e)
        {        
            if (keyboard_hook.keyboard_handler == 0)
            {
          
                set_hook_event();
                display.Text += "設置 Global Hook " + (keyboard_hook.Start(true) ? "成功" : "失敗") + "!\r\n";               
            }
            else
            {
                display.Text += "移除 Global Hook " + (keyboard_hook.Stop() ? "成功" : "失敗") + "!\r\n";
                keyboard_hook = new Keyboard_Hook();
            }
            global_hook_btn.Content = keyboard_hook.keyboard_handler == 0 ? "設置 Global Hook" : "移除 Global Hook";
            local_hook_btn.IsEnabled = keyboard_hook.keyboard_handler == 0 ? true :false ;
        }

        private void handle_keyboard(object sender, RoutedEventArgs e) {
            keyboard_hook.keyboard_hook_handle = !keyboard_hook.keyboard_hook_handle;
            Button btn = (Button)sender;
            btn.Content = keyboard_hook.keyboard_hook_handle ? "取消 攔截不輸入鍵盤" : "設定 攔截不輸入鍵盤";

        }

        private void display_TextChanged(object sender, TextChangedEventArgs e)
        {
            display.ScrollToEnd();
        }

        private void display_clear(object sender, RoutedEventArgs e) {
            display.Clear();
        }




    }
}
