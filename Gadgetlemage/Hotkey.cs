using System;
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
        public Action hotkeyAction { get; private set; }
        public VirtualKey Key { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="setSettingsName"></param>
        /// <param name="setAction"></param>
        public Hotkey(string setSettingsName, Action setAction)
        {
            settingsName = setSettingsName;
            hotkeyAction = setAction;
            Key = (VirtualKey)(int)Properties.Settings.Default[settingsName];
        }

        /// <summary>
        /// On KeyUp event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void keyUp(object sender, KeyEventArgs e)
        {
            Key = (VirtualKey)e.Key;
            e.Handled = true;
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
