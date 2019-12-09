using PropertyHook;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Gadgetlemage.DarkSouls;
using System.Windows;

namespace Gadgetlemage
{
    public class Model : PHook
    {
        /// <summary>
        /// Constants
        /// </summary>
        private const string PTDE_NAME = "DARKSOULS";
        private const string REMASTERED_NAME = "DARK SOULS™: REMASTERED";
        private const int UNHOOKED_INTERVAL = 1000;
        private const int HOOKED_INTERVAL = 33;
        private const int MIN_LIFE_SPAN = 5000;

        /// <summary>
        /// Events
        /// </summary>
        public event EventHandler OnItemAcquired;

        /// <summary>
        /// Process Selector
        /// </summary>
        private static Func<Process, bool> processSelector = (p) =>
        {
            return (p.MainWindowTitle == REMASTERED_NAME) || (p.ProcessName == PTDE_NAME);
        };

        /// <summary>
        /// Abstract DarkSouls class
        /// Could be null
        /// </summary>
        public DarkSouls.DarkSouls DarkSouls { get; set; }

        /// <summary>
        /// Whether or not the game was loaded on the previous loop
        /// </summary>
        public bool PreviousLoaded { get; set; }

        /// <summary>
        /// Whether or not the black knight deaths has already been processed
        /// </summary>
        public bool DeathProcessed { get; set; }

        /// <summary>
        /// List of all available black knight weapons
        /// </summary>
        public List<BlackKnightWeapon> Weapons { get; set; }

        /// <summary>
        /// Currently selected weapon
        /// Used by GetItem
        /// </summary>
        public BlackKnightWeapon SelectedWeapon { get; set; }

        /// <summary>
        /// Black Knight Shield
        /// </summary>
        public BlackKnightWeapon BlackKnightShield
        {
            get
            {
                return new BlackKnightWeapon(0x0, 1474000, "Black Knight Shield");
            }
        }

        new public int RefreshInterval
        {
            get
            {
                return (Hooked) ? HOOKED_INTERVAL : UNHOOKED_INTERVAL;
            }
        }

        /// <summary>
        /// If the process is ready (Hook and Dark Souls is not null)
        /// </summary>
        private bool Ready
        {
            get
            {
                return Hooked && DarkSouls != null;
            }
        }

        /// <summary>
        /// Returns if the player's game is current loaded (aka in game)
        /// </summary>
        public bool Loaded
        {
            get
            {
                return (Ready) ? DarkSouls.Loaded : false;
            }
        }

        /// <summary>
        /// Returns if the current process has focus
        /// </summary>
        public bool Focused
        {
            get
            {
                return (Ready) ? DarkSouls.Focused : false;
            }
        }

        /// <summary>
        /// Game version. Only remastered is 64 bits
        /// </summary>
        public GameVersion Version
        {
            get
            {
                return (Is64Bit) ? GameVersion.Remastered : GameVersion.PrepareToDie;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Model() : base(0, MIN_LIFE_SPAN, processSelector) // RefreshInterval at 0 because manually refreshed from the outside
        {
            Weapons = new List<BlackKnightWeapon>()
            {
                new BlackKnightWeapon(0x0, 355000, "Black Knight Greatsword", () =>
                {
                    return DarkSouls.ReadEventFlag(11010862);
                }),
                new BlackKnightWeapon(0x0, 1105000, "Black Knight Halberd", () =>
                {
                    return DarkSouls.ReadEventFlag(11200816);
                }),
                new BlackKnightWeapon(0x0, 310000, "Black Knight Sword", () =>
                {
                    return DarkSouls.ReadEventFlag(11010861);
                }),
                new BlackKnightWeapon(0x0, 753000, "Black Knight Greataxe", () =>
                {
                    return DarkSouls.ReadEventFlag(11300859);
                })
            };

            OnHooked += DarkSoulsProcess_OnHooked;
            OnUnhooked += DarkSoulsProcess_OnUnhooked;

            DeathProcessed = false;
            PreviousLoaded = false;
        }

        /// <summary>
        /// Hook event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DarkSoulsProcess_OnHooked(object sender, PHEventArgs e)
        {
            switch (Version)
            {
                case GameVersion.PrepareToDie:
                    DarkSouls = new PrepareToDie(this);
                    break;
                case GameVersion.Remastered:
                    DarkSouls = new Remastered(this);
                    break;
                default:
                    DarkSouls = null;
                    break;
            }
        }

        /// <summary>
        /// Unhook event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DarkSoulsProcess_OnUnhooked(object sender, PHEventArgs e)
        {
            DarkSouls = null;
        }

        /// <summary>
        /// Creates the currently selected weapon
        /// </summary>
        public void CreateWeapon()
        {
            if (Ready)
            {
                DarkSouls.CreateWeapon(SelectedWeapon);
                OnItemAcquired?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Delete the black knight shield
        /// </summary>
        public void DeleteShield()
        {
            if (Ready)
            {
                DarkSouls.DeleteItem(BlackKnightShield);
            }
        }

        /// <summary>
        /// Main update loop that will automatically create and delete black knight weapons if needed
        /// </summary>
        /// <param name="createWeapon">If it should automatically create the weapon</param>
        /// <param name="deleteShield">If it should automatically delete the shield</param>
        public void UpdateLoop(bool createWeapon, bool deleteShield)
        {
            bool CurrentLoaded = Loaded;

            if (CurrentLoaded)
            {
                bool IsBlackKnightDead = SelectedWeapon.IsConditionSatisfied();
                bool IsBlackKnightAlive = !IsBlackKnightDead;
                bool JustLoaded = !PreviousLoaded && CurrentLoaded;

                /**
                 * If the black knight is still alive, we reset the state of the Weapon and Shield
                 * Only checks after the player just loaded back in because it is the only
                 * reliable moment
                 */
                if (JustLoaded && IsBlackKnightAlive)
                {
                    DeathProcessed = false;
                }

                /**
                 * Only run logic when the black is dead and only run that logic once,
                 * until the black knight is alive again
                 */ 
                if (IsBlackKnightDead && !DeathProcessed)
                {
                    // weapon
                    if (createWeapon)
                    {
                        bool needsToCreate = !DarkSouls.FindBlackKnightWeapon(SelectedWeapon);

                        if (needsToCreate)
                        {
                            /**
                             * If the black knight is dead, the player doesn't have the
                             * weapon and we didn't already gave it, then we give it 
                             */
                            CreateWeapon();
                        }
                    }

                    // shield
                    if (deleteShield)
                    {
                        bool needsToDelete = DarkSouls.FindBlackKnightWeapon(BlackKnightShield);

                        if (needsToDelete)
                        {
                            /**
                             * If the black knight is dead and the player has the shield
                             * so we need to delete it
                             */
                            DeleteShield();
                        }
                    }
                    
                    // only execute this once per black knight death
                    DeathProcessed = true;
                }
            }

            PreviousLoaded = CurrentLoaded;
        }

        /// <summary>
        /// Change the currently selected weapon
        /// </summary>
        /// <param name="weapon">The new selected weapon</param>
        public void SetSelectedWeapon(BlackKnightWeapon weapon)
        {
            SelectedWeapon = weapon;
        }

#if DEBUG
        public void Debug()
        {
            bool alreadyOwns = DarkSouls.FindBlackKnightWeapon(SelectedWeapon);
        }
#endif
    }
}
