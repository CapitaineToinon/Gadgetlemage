using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using LowLevelHooking;

namespace Gadgetlemage
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// PHook where all the magic happens
        /// https://github.com/JKAnderson/PropertyHook
        /// </summary>
        private DarkSoulsHook Hook { get; set; }

        /// <summary>
        /// Hotkey
        /// </summary>
        private GlobalKeyboardHook keyboardHook;
        private Hotkey createHotkey;

        /// <summary>
        /// Threads
        /// </summary>
        private Thread RefreshThread;
        private CancellationTokenSource RefreshCancellationSource;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;
        }

        /// <summary>
        /// Starts the main thread
        /// </summary>
        public void Start()
        {
            if (RefreshThread == null)
            {
                RefreshCancellationSource = new CancellationTokenSource();
                var threadStart = new ThreadStart(() => AutoRefresh(RefreshCancellationSource.Token));
                RefreshThread = new Thread(threadStart);
                RefreshThread.IsBackground = true;
                RefreshThread.Start();
            }
        }

        /// <summary>
        /// Stops the automatic hooking thread.
        /// </summary>
        public void Stop()
        {
            if (RefreshThread != null)
            {
                RefreshCancellationSource.Cancel();
                RefreshThread = null;
                RefreshCancellationSource = null;
            }
        }

        /// <summary>
        /// Refresh method. Manually Refresh instead of using Hook.Start()
        /// Because we also need to check for the automatic get item
        /// </summary>
        /// <param name="ct"></param>
        private void AutoRefresh(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                Hook.Refresh();

                Dispatcher.Invoke(new Action(() =>
                {
                    btnCreate.IsEnabled = Hook.Hooked && Hook.Loaded;

                    bool auto = cbxAuto.IsChecked ?? false;
                    if (Hook.Hooked && Hook.Loaded && auto)
                    {
                        Hook.AutomaticallyGetItem();
                    }
                }));

                Thread.Sleep(Hook.RefreshInterval);
            }
        }

        /// <summary>
        /// On Load
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // threds
            RefreshThread = null;
            RefreshCancellationSource = null;

            // Keyboard hook and hotkeys
            keyboardHook = new GlobalKeyboardHook();
            Hook = new DarkSoulsHook()
            {
                RefreshInterval = 1000 / 30 // 30 times a second
            };

            // Get Item Hotkey
            createHotkey = new Hotkey("HotkeyCreate", GetItem);

            // Load defaults
            int selectedIndex = (int)Properties.Settings.Default["SelectedIndex"];
            Hook.SelectedWeapon = Hook.Weapons[selectedIndex];
            cbxAuto.IsChecked = (bool)Properties.Settings.Default["Auto"];
            cbxHotkey.IsChecked = (bool)Properties.Settings.Default["Hotkey"];
            cbxConsume.IsChecked = (bool)Properties.Settings.Default["Consume"];
            cbxSound.IsChecked = (bool)Properties.Settings.Default["Sound"];

            // UI elements
            comboWeapons.Items.Clear();
            Hook.Weapons.ForEach(w => comboWeapons.Items.Add(w));
            comboWeapons.SelectedIndex = selectedIndex;
            comboWeapons.SelectionChanged += ComboWeapons_SelectionChanged;

            tbxHotkey.IsReadOnly = true;
            tbxHotkey.Text = createHotkey.ToString();

            btnCreate.IsEnabled = false;

            // Events
            keyboardHook.KeyDownOrUp += KeyboardHook_KeyDownOrUp_ListenHotkey;

            Hook.OnItemAcquired += Hook_OnItemAcquired;
            Hook.OnHooked += Hook_OnHookedUnHook;
            Hook.OnUnhooked += Hook_OnHookedUnHook;

            btnCreate.Click += BtnCreate_Click;
            btnHotkey.Click += BtnHotkey_Click;

            // Start the main hook
            Start();
        }

        private void Hook_OnHookedUnHook(object sender, PropertyHook.PHEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                btnCreate.IsEnabled = Hook.Hooked;
            }));
        }

        /// <summary>
        /// Plays a sound if needed when item acquired
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hook_OnItemAcquired(object sender, EventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                bool sound = cbxSound.IsChecked != null && cbxSound.IsChecked == true;

                if (sound)
                    Console.Beep();
            }));
        }

        /// <summary>
        /// Form Closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            // Stop the thread
            Stop();

            // Save settings
            createHotkey.Save();

            int selectedIndex = comboWeapons.SelectedIndex;
            bool auto = cbxAuto.IsChecked ?? false;
            bool hotkeyEnabled = cbxHotkey.IsChecked ?? false;
            bool consume = cbxConsume.IsChecked ?? false;
            bool sound = cbxSound.IsChecked ?? false;

            Properties.Settings.Default["SelectedIndex"] = selectedIndex;
            Properties.Settings.Default["Auto"] = auto;
            Properties.Settings.Default["Hotkey"] = hotkeyEnabled;
            Properties.Settings.Default["Consume"] = consume;
            Properties.Settings.Default["Sound"] = sound;

            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// GlobalKeyboardHook event used to listen for hotkeys
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeyboardHook_KeyDownOrUp_ListenHotkey(object sender, GlobalKeyboardHookEventArgs e)
        {
            bool hotkeyEnabled = cbxHotkey.IsChecked ?? false;
            bool consume = cbxConsume.IsChecked ?? false;
            if (!e.IsUp && hotkeyEnabled && Hook.Focused)
            {
                if (createHotkey.Trigger(e.KeyCode) && consume)
                {
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// GlobalKeyboardHook event used to listen for a new hotkey
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeyboardHook_KeyDownOrUp_SetupHotkey(object sender, GlobalKeyboardHookEventArgs e)
        {
            // Only on KeyDown, if process has focus and is loaded
            if (!e.IsUp)
            {
                createHotkey.Key = (VirtualKey)e.KeyCode;
                tbxHotkey.Text = createHotkey.ToString();
                btnHotkey.IsEnabled = true;
                btnHotkey.Content = "Change Hotkey";
                e.Handled = true;

                // Swap the GlobalKeyboardHook events
                keyboardHook.KeyDownOrUp -= KeyboardHook_KeyDownOrUp_SetupHotkey;
                keyboardHook.KeyDownOrUp += KeyboardHook_KeyDownOrUp_ListenHotkey;
            }
        }

        /// <summary>
        /// Selected Weapon Changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboWeapons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Hook.SelectedWeapon = (Weapon)comboWeapons.SelectedItem;
        }

        /// <summary>
        /// Manually create an item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            GetItem();
        }

        /// <summary>
        /// Listening for a new hotkey
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnHotkey_Click(object sender, RoutedEventArgs e)
        {
            btnHotkey.IsEnabled = false;
            btnHotkey.Content = "Listening...";

            // Swap the GlobalKeyboardHook events
            keyboardHook.KeyDownOrUp -= KeyboardHook_KeyDownOrUp_ListenHotkey;
            keyboardHook.KeyDownOrUp += KeyboardHook_KeyDownOrUp_SetupHotkey;
        }

        /// <summary>
        /// Get the Selected Item
        /// </summary>
        public void GetItem()
        {
            if (Hook.Hooked && Hook.Loaded)
            {
                Hook.GetItem();
            }
        }

        /// <summary>
        /// Open Github
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
