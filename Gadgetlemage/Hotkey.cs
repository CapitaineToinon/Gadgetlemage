using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LowLevelHooking;

namespace Gadgetlemage
{
    public class Hotkey
    {
        /// <summary>
        /// Properties
        /// </summary>
        public string settingsName { get; private set; }
        public GlobalKeyboardHook hook { get; private set; }
        public Action hotkeyAction { get; private set; }
        public TextBox textBox { get; private set; }
        public Button button { get; private set; }
        public VirtualKey Key { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="setSettingsName"></param>
        /// <param name="setAction"></param>
        public Hotkey(string setSettingsName, GlobalKeyboardHook setHook, TextBox setTextBox, Button setButton, Action setAction)
        {
            settingsName = setSettingsName;
            hook = setHook;
            hotkeyAction = setAction;
            textBox = setTextBox;
            button = setButton;

            Key = (VirtualKey)(int)Properties.Settings.Default[settingsName];

            textBox.IsReadOnly = true;
            textBox.Text = this.ToString();
            button.Click += btn_Click;
        }

        private void TextBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            button.IsEnabled = false;
        }

        /// <summary>
        /// Listening for a new hotkey
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button)
            {
                Button b = (sender as Button);
                b.IsEnabled = false;
                b.Content = "Listening...";
                hook.KeyDownOrUp += Hook_KeyDownOrUp; // setup the hook
            }
        }

        /// <summary>
        /// Hook to setup the hotkey
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hook_KeyDownOrUp(object sender, GlobalKeyboardHookEventArgs e)
        {
            // Only on KeyDown, if process has focus and is loaded
            if (!e.IsUp)
            {
                Key = (VirtualKey)e.KeyCode;
                textBox.Text = this.ToString();
                button.IsEnabled = true;
                button.Content = "Change Hotkey";
                e.Handled = true;

                hook.KeyDownOrUp -= Hook_KeyDownOrUp;
            }
        }

        /// <summary>
        /// Trigger the hotkey's event if needed
        /// </summary>
        /// <param name="pressed"></param>
        /// <returns></returns>
        public bool Trigger(VirtualKey pressed)
        {
            bool result = false;
            if (Key != VirtualKey.Escape && pressed == Key)
            {
                hotkeyAction();
                result = true;
            }
            return result;
        }

        /// <summary>
        /// Key's name
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return (Key == VirtualKey.Escape) ? "Unbound" : Key.ToString();
        }

        /// <summary>
        /// Save hotkey to settings
        /// </summary>
        public void Save()
        {
            Properties.Settings.Default[settingsName] = (int)Key;
        }
    }
}
