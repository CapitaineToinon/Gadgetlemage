using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Gadgetlemage.DarkSouls;
using LowLevelHooking;

namespace Gadgetlemage
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Constants
        /// </summary>
        private const int THREAD_REFRESH_RATE = 1000 / 30;

        /// <summary>
        /// PHook where all the magic happens
        /// https://github.com/JKAnderson/PropertyHook
        /// </summary>
        private Model Model { get; set; }

        /// <summary>
        /// Hotkey
        /// </summary>
        private GlobalKeyboardHook keyboardHook;
        private List<Hotkey> hotkeys = new List<Hotkey>();

        /// <summary>
        /// Threads
        /// </summary>
        private Thread RefreshThread;
        private CancellationTokenSource RefreshCancellationSource;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // display version
            this.Title = $"Gadgetlemage ({getRunningVersion()})";

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
                Model.Refresh();

                Dispatcher.Invoke(new Action(() =>
                {
                    if (Model.Hooked && Model.Loaded)
                    {
                        btnCreate.IsEnabled = true;
                        // Automatically creates the weapon if needed
                        if (cbxAutoCreate.IsChecked ?? false)
                        {
                            Model.AutomaticallyCreateWeapon();
                        }
                        
                        // Automatically delete the shield if needed
                        if (cbxAutoDelete.IsChecked ?? false)
                        {
                            Model.AutomaticallyRemoveShield();
                        }
                        
                        // Stop searching once the associated Black Knight dies
                        if (Model.SelectedWeapon.IsConditionSatisfied())
                        {
                            ct.IsCancellationRequested = true;
                        }
                    }
                    else
                    {
                        btnCreate.IsEnabled = false;
                    }
                }));

                Thread.Sleep(Model.RefreshInterval);
            }
        }

        /// <summary>
        /// Save the settings
        /// </summary>
        public void SaveSettings()
        {
            // Save settings
            hotkeys.ForEach(hotkey => hotkey.Save());

            int selectedIndex = comboWeapons.SelectedIndex;
            bool autoCreate = cbxAutoCreate.IsChecked ?? false;
            bool autoDelete = cbxAutoDelete.IsChecked ?? false;
            bool hotkeyEnabled = cbxHotkey.IsChecked ?? false;
            bool consume = cbxConsume.IsChecked ?? false;
            bool sound = cbxSound.IsChecked ?? false;

            Properties.Settings.Default["SelectedIndex"] = selectedIndex;
            Properties.Settings.Default["AutoCreate"] = autoCreate;
            Properties.Settings.Default["AutoDelete"] = autoDelete;
            Properties.Settings.Default["Hotkey"] = hotkeyEnabled;
            Properties.Settings.Default["Consume"] = consume;
            Properties.Settings.Default["Sound"] = sound;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// On Load
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            btnDebug.Visibility = Visibility.Visible;
            btnDebug.Click += BtnDebug_Click;
#else
            btnDebug.Visibility = Visibility.Hidden;
#endif
            /**
             * Keyboard hook and Model
             */
            Model = new Model();
            keyboardHook = new GlobalKeyboardHook();

            /**
             * Thread
             */
            RefreshThread = null;
            RefreshCancellationSource = null;

            /**
             * Init Hotkeys
             */
            hotkeys.Add(new Hotkey("HotkeyCreate", keyboardHook, tbxCreateHotkey, btnCreateHotkey, () =>
            {
                if (Model.Hooked && Model.Focused)
                {
                    Model.CreateWeapon();
                }
            }));
            hotkeys.Add(new Hotkey("HotkeyDelete", keyboardHook, tbxDeleteHotkey, btnDeleteHotkey, () =>
            {
                if (Model.Hooked && Model.Focused)
                {
                    Model.DeleteShield();
                }
            }));

            /**
             * Init UI
             */
            int selectedIndex = (int)Properties.Settings.Default["SelectedIndex"];
            Model.SetSelectedWeapon(Model.Weapons[selectedIndex]);
            cbxAutoCreate.IsChecked = (bool)Properties.Settings.Default["AutoCreate"];
            cbxAutoDelete.IsChecked = (bool)Properties.Settings.Default["AutoDelete"];
            cbxHotkey.IsChecked = (bool)Properties.Settings.Default["Hotkey"];
            cbxConsume.IsChecked = (bool)Properties.Settings.Default["Consume"];
            cbxSound.IsChecked = (bool)Properties.Settings.Default["Sound"];
            comboWeapons.Items.Clear();
            Model.Weapons.ForEach(w => comboWeapons.Items.Add(w));
            comboWeapons.SelectedIndex = selectedIndex;
            comboWeapons.SelectionChanged += ComboWeapons_SelectionChanged;
            btnCreate.IsEnabled = false;

            /**
             * Events
             */
            keyboardHook.KeyDownOrUp += KeyboardHook_KeyDownOrUp_ListenHotkey;
            Model.OnItemAcquired += Hook_OnItemAcquired;
            Model.OnHooked += Hook_OnHookedUnHook;
            Model.OnUnhooked += Hook_OnHookedUnHook;

            /**
             * Start the main thread
             */
            Start();
        }

        /// <summary>
        /// Form Closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Stop();
            SaveSettings();
        }

        /// <summary>
        /// Model Hook/Unhook event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hook_OnHookedUnHook(object sender, PropertyHook.PHEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                btnCreate.IsEnabled = Model.Hooked;
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
                bool sound = cbxSound.IsChecked ?? false;

                if (sound)
                {
                    Console.Beep();
                }
            }));
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

            if (!e.IsUp && hotkeyEnabled)
            {
                foreach (Hotkey hotkey in hotkeys)
                {
                    if (hotkey.Trigger(e.KeyCode) && consume)
                    {
                        e.Handled = true;
                    }
                }
            }
        }

        /// <summary>
        /// Selected Weapon Changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboWeapons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Model.SetSelectedWeapon((BlackKnightWeapon)comboWeapons.SelectedItem);
        }

        /// <summary>
        /// Manually create a weapon
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            Model.CreateWeapon();
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

        /// <summary>
        /// Build version
        /// </summary>
        /// <returns></returns>
        private Version getRunningVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        }

#if DEBUG
        /// <summary>
        /// Debug method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDebug_Click(object sender, RoutedEventArgs e)
        {
            // Try things here
            Model.Debug();
        }
#endif
    }
}
