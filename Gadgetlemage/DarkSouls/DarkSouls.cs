using System;
using PropertyHook;

namespace Gadgetlemage.DarkSouls
{
    public abstract class DarkSouls
    {
        public int ItemCategory => 0x0;
        public int ItemQuantity => 1;

        /// <summary>
        /// The Game. Either Dark Souls or Remastered
        /// </summary>
        public PHook Process { get; set; }

        /// <summary>
        /// Loaded pointer
        /// </summary>
        public PHPointer pLoaded { get; set; }

        /// <summary>
        /// Pointers to flags
        /// </summary>
        public PHPointer pFlags { get; set; }

        /// <summary>
        /// Returns if the player's game is current loaded (aka in game)
        /// </summary>
        public bool Loaded
        {
            get
            {
                return pLoaded.Resolve() != IntPtr.Zero;
            }
        }


        /// <summary>
        /// Returns if the current process has focus
        /// </summary>
        public bool Focused
        {
            get
            {
                return Process.Hooked && User32.GetForegroundProcessID() == Process.Process.Id;
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public DarkSouls(PHook process)
        {
            this.Process = process;
        }

        /// <summary>
        /// Returns true if the SelectedWeapon is already in the player's inventory
        /// </summary>
        /// <returns>bool</returns>
        public bool ItemIsInPlayersInventory(BlackKnightWeapon weapon)
        {
            InventoryItem[] items = GetInventoryItems();

            bool alreadyOwn = false;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].Category == weapon.Category && weapon.Upgrade.Contains(items[i].ID))
                {
                    // Found the weapon in the inventory!
                    alreadyOwn = true;
                    break;
                }
            }

            return alreadyOwn;
        }

        /// <summary>
        /// Flag method that calcuates the offset according to the flag ID
        /// This is literally magic 
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        private int getEventFlagOffset(int ID, out uint mask)
        {
            string idString = ID.ToString("D8");
            if (idString.Length == 8)
            {
                string group = idString.Substring(0, 1);
                string area = idString.Substring(1, 3);
                int section = Int32.Parse(idString.Substring(4, 1));
                int number = Int32.Parse(idString.Substring(5, 3));

                if (Flags.Groups.ContainsKey(group) && Flags.Areas.ContainsKey(area))
                {
                    int offset = Flags.Groups[group];
                    offset += Flags.Areas[area] * 0x500;
                    offset += section * 128;
                    offset += (number - (number % 32)) / 8;

                    mask = 0x80000000 >> (number % 32);
                    return offset;
                }
            }
            throw new ArgumentException("Unknown event flag ID: " + ID);
        }

        /// <summary>
        /// ReadEventFlag method for PTDE and Remastered
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public bool ReadEventFlag(int ID)
        {
            int offset = getEventFlagOffset(ID, out uint mask);
            return pFlags.ReadFlag32(offset, mask);
        }

        /// <summary>
        /// Returns a list of the player's items
        /// </summary>
        /// <returns></returns>
        public abstract InventoryItem[] GetInventoryItems();

        /// <summary>
        /// Method that will create the weapon and put it into the player's inventory
        /// </summary>
        public abstract void CreateWeapon(BlackKnightWeapon weapon);

    }
}
