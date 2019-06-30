using PropertyHook;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Gadgetlemage.DarkSouls;

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
        /// Whether or not the weapon has created
        /// </summary>
        public bool WeaponCreated { get; set; }

        /// <summary>
        /// Whether or not the shield was deleted
        /// </summary>
        public bool ShieldDeleted { get; set; }

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

            WeaponCreated = true;
            ShieldDeleted = true;
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
        /// Automatically delete the shield from the player's inventory
        /// </summary>
        public void AutomaticallyRemoveShield()
        {
            if (Ready)
            {
                bool alreadyOwns = DarkSouls.FindBlackKnightWeapon(BlackKnightShield);
                bool IsBlackKnightDead = SelectedWeapon.IsConditionSatisfied();

                /**
                 * If the black knight is still alive, we reset the state of the Weapon
                 */
                if (!IsBlackKnightDead)
                {
                    ShieldDeleted = false;
                }
                // else the black knight is dead...
                else
                {
                    if (alreadyOwns)
                    {
                        if (!ShieldDeleted)
                        {
                            /**
                             * If the black knight is dead and the player has the shield
                             * so we need to delete it
                             */
                            DeleteShield();
                            ShieldDeleted = true;
                        }
                        else
                        {
                            /**
                             * If the black knight is dead but the player still has the
                             * shield. Maybe another black knight dropped one so we don't
                             * do anything
                             */
                            // Nothing to do here
                        }
                    }
                    else
                    {
                        if (!ShieldDeleted)
                        {
                            /**
                             * The black knight is dead and we didn't delete the shield.
                             * Hence it didn't drop so we don't need to delete anything
                             */
                            ShieldDeleted = true;
                        }
                        else
                        {
                            /**
                             * The black knight is dead, the player doesn't have the shield
                             * and we already removed it from the inventory. Everything's
                             * good
                             */
                            // Nothing to do here
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Automatically Get the item if needed
        /// </summary>
        public void AutomaticallyCreateWeapon()
        {
            if (Ready)
            {
                bool IsBlackKnightDead = SelectedWeapon.IsConditionSatisfied();

                /**
                 * If the black knight is still alive, we reset the state of the Weapon
                 */
                if (!IsBlackKnightDead)
                {
                    WeaponCreated = false;
                }
                // else the black knight is dead...
                else
                {
                    bool alreadyOwns = DarkSouls.FindBlackKnightWeapon(SelectedWeapon);

                    if (alreadyOwns)
                    {
                        if (!WeaponCreated)
                        {
                            /**
                             * If the black knight is dead and the player has the weapon
                             * but we never gave it to him then it's a legit drop
                             */
                            WeaponCreated = true;
                        }
                        else
                        {
                            /**
                             * If the black knight is dead and the player has the weapon
                             * and we already gave it to him then it's all good and we
                             * don't do anything
                             */
                            // Nothing to do here
                        }
                    }
                    else
                    {
                        if (!WeaponCreated)
                        {
                            /**
                             * If the black knight is dead, the player doesn't have the
                             * weapon and we didn't already gave it, then we give it 
                             */
                            CreateWeapon();
                            WeaponCreated = true;
                        }
                        else
                        {
                            /**
                             * If the black knight is dead, the player doesn't have the
                             * weapon but we aleady gave it, then we don't do anything.
                             * Maybe the player intentionally removed the weapon from his
                             * inventory.
                             */
                            // Nothing to do here
                        }
                    }
                }
            }
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
