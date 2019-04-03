using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using LowLevelHooking;

namespace WPF.Gadgetlemage
{
    public class Hotkey
    {
        private string settingsName;
        private Action hotkeyAction;

        public VirtualKey Key;

        public Hotkey(string setSettingsName, Action setAction)
        {
            settingsName = setSettingsName;
            hotkeyAction = setAction;

            Key = (VirtualKey)(int)Properties.Settings.Default[settingsName];
        }

        public void keyUp(object sender, KeyEventArgs e)
        {
            Key = (VirtualKey)e.Key;
            e.Handled = true;
        }

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

        public override string ToString()
        {
            return (Key == VirtualKey.Escape) ? "Unbound" : Key.ToString();
        }

        public void Save()
        {
            Properties.Settings.Default[settingsName] = (int)Key;
        }
    }
}
