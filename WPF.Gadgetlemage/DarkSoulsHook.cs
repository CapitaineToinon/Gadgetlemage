using PropertyHook;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WPF.Gadgetlemage
{
    public enum Version
    {
        DarkSouls,
        DarkSoulsRemastered,
    };

    public class DarkSoulsHook : PHook
    {
        #region Events
        /// <summary>
        /// Events
        /// </summary>
        public event EventHandler OnItemAcquired;
        #endregion

        #region Private properties
        private const string _ptdeProcessName = "DARKSOULS";
        private const string _remasteredName = "DARK SOULS™: REMASTERED";
        private const int _refreshInterval = 1000;
        private const int _minLifetime = 5000;
        private int itemCategory = 0;
        private int itemQuantity = 1;
        private static Func<Process, bool> processSelector = (p) =>
        {
            return (p.MainWindowTitle == _remasteredName) || (p.ProcessName == _ptdeProcessName);
        };
        #endregion

        #region Pointers
        /// <summary>
        /// Prepare to die
        /// </summary>
        private const string PTDEBasePtrAOB = "8B 0D ? ? ? ? 8B 7E 1C 8B 49 08 8B 46 20";
        private const string PTDEInventoryDataAOB = "A1 ? ? ? ? 53 55 8B 6C 24 10 56 8B 70 08 32 DB 85 F6";
        private const string PTDEFlagsAOB = "56 8B F1 8B 46 1C 50 A1 ? ? ? ? 32 C9";
        private PHPointer PTDEBasePtr;
        private PHPointer PTDEInventoryData;
        private PHPointer PTDEFlags;
        private uint PTDEInventoryIndexStart = 0x1B8;
        private uint PTDEFuncItemGetPtr = 0xC0B6DA;

        /// <summary>
        /// Remastered
        /// </summary>
        private const string RemasterBasePtrAOB = "48 8B 05 ? ? ? ? 48 85 C0 ? ? F3 0F 58 80 AC 00 00 00";
        private const string RemasterItemAddrAOB = "48 89 5C 24 18 89 54 24 10 55 56 57 41 54 41 55 41 56 41 57 48 8D 6C 24 F9";
        private const string RemasterInventoryDataAOB = "48 8B 05 ? ? ? ? 48 85 C0 ? ? F3 0F 58 80 AC 00 00 00";
        private const string RemasterFlagsAOB = "48 8B 0D ? ? ? ? 99 33 C2 45 33 C0 2B C2 8D 50 F6";
        public const string RemasterChrFollowCamAOB = "48 8B 0D ? ? ? ? E8 ? ? ? ? 48 8B 4E 68 48 8B 05 ? ? ? ? 48 89 48 60";
        private PHPointer RemasterBasePtr;
        private PHPointer RemasterItemAddr;
        private PHPointer RemasterInventoryData;
        private PHPointer RemasterFlags;
        private PHPointer RemasterChrFollowCam;
        #endregion

        #region Public properties
        /// <summary>
        /// All available weapons
        /// </summary>
        public List<Weapon> Weapons
        {
            get
            {
                return new List<Weapon>()
                {
                    /**
                     * - Item Category
                     * - Item ID
                     * - Item Name
                     * - Black Knight's Flag
                     */ 
                    new Weapon(0x0, 355000, "Black Knight Greatsword", 11010862),
                    new Weapon(0x0, 1105000, "Black Knight Halberd", 11200816),
                    new Weapon(0x0, 310000, "Black Knight Sword", 11010861),
                    new Weapon(0x0, 753000, "Black Knight Greataxe", 11300859),
                };
            }
        }

        /// <summary>
        /// Currently selected weapon
        /// Used by GetItem
        /// </summary>
        public Weapon SelectedWeapon { get; set; }

        /// <summary>
        /// If the Process is in focus
        /// </summary>
        public bool Focused => Hooked && User32.GetForegroundProcessID() == Process.Id;

        /// <summary>
        /// If the save is loaded
        /// </summary>
        public bool Loaded
        {
            get
            {
                return (Version == Version.DarkSoulsRemastered)
                    ? (RemasterChrFollowCam.Resolve() != IntPtr.Zero)
                    : true;
            }
        }

        /// <summary>
        /// Game version. Only remastered is 64 bits
        /// </summary>
        public Version Version
        {
            get
            {
                return (Is64Bit) ? Version.DarkSoulsRemastered : Version.DarkSouls;
            }
        }
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public DarkSoulsHook() : base(_refreshInterval, _minLifetime, processSelector)
        {
            SelectedWeapon = Weapons[0];

            // PTDE Pointers
            PTDEBasePtr = RegisterAbsoluteAOB(PTDEBasePtrAOB, 2);
            PTDEInventoryData = RegisterAbsoluteAOB(PTDEInventoryDataAOB, 1);
            PTDEFlags = RegisterAbsoluteAOB(PTDEFlagsAOB, 8, 0, 0);

            // Remastered Pointers
            RemasterBasePtr = RegisterRelativeAOB(RemasterBasePtrAOB, 3, 7);
            RemasterItemAddr = RegisterAbsoluteAOB(RemasterItemAddrAOB);
            RemasterFlags = RegisterRelativeAOB(RemasterFlagsAOB, 3, 7, 0, 0);
            RemasterChrFollowCam = RegisterRelativeAOB(RemasterChrFollowCamAOB, 3, 7, 0, 0x60, 0x60);

            RemasterInventoryData = RegisterRelativeAOB(RemasterInventoryDataAOB, 3, 7);
        }

        #region GetItem
        /// <summary>
        /// Main GetItem that will call the correct GetItem
        /// For PTDE or Remastered
        /// </summary>
        public void GetItem()
        {
            if (Version == Version.DarkSouls)
                GetItemPTDE();
            else
                GetItemRemastered();

            // Also set HasDropped to true in case
            // The player uses the hot before killing the knight
            // and has auto drop on
            SelectedWeapon.HasDropped = true;

            OnItemAcquired?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// GetItem for Remastered
        /// </summary>
        private void GetItemRemastered()
        {
            if (Hooked)
            {
                byte[] asm = (byte[])Assembly.REMASTERED.Clone();

                byte[] bytes = BitConverter.GetBytes(itemCategory);
                Array.Copy(bytes, 0, asm, 0x1, 4);
                bytes = BitConverter.GetBytes(itemQuantity);
                Array.Copy(bytes, 0, asm, 0x7, 4);
                bytes = BitConverter.GetBytes(SelectedWeapon.ID);
                Array.Copy(bytes, 0, asm, 0xD, 4);
                bytes = BitConverter.GetBytes((ulong)RemasterBasePtr.Resolve());
                Array.Copy(bytes, 0, asm, 0x19, 8);
                bytes = BitConverter.GetBytes((ulong)RemasterItemAddr.Resolve());
                Array.Copy(bytes, 0, asm, 0x46, 8);

                Execute(asm);
            }
        }

        /// <summary>
        /// GetItem for PTDE
        /// </summary>
        private void GetItemPTDE()
        {
            if (Hooked)
            {
                byte[] asm = (byte[])Assembly.PTDE.Clone();

                // Get the pointer to CharBasePtr
                IntPtr pointer = PTDEBasePtr.Resolve();
                pointer = CreateChildPointer(PTDEBasePtr, 0, 8).Resolve();

                // Have to allocate first to rebase the code
                IntPtr memory = Allocate((uint)asm.Length);
                uint FuncItemGetPtr = (uint)(PTDEFuncItemGetPtr - (uint)memory);

                // Now we can write the rebased bytes
                byte[] bytes = BitConverter.GetBytes((ulong)pointer + PTDEInventoryIndexStart);
                Array.Copy(bytes, 0, asm, 0x1, 4);
                bytes = BitConverter.GetBytes(itemCategory);
                Array.Copy(bytes, 0, asm, 0x6, 4);
                bytes = BitConverter.GetBytes(SelectedWeapon.ID);
                Array.Copy(bytes, 0, asm, 0xB, 4);
                bytes = BitConverter.GetBytes(itemQuantity);
                Array.Copy(bytes, 0, asm, 0x10, 4);
                bytes = BitConverter.GetBytes((ulong)FuncItemGetPtr);
                Array.Copy(bytes, 0, asm, 0x22, 4);

                // Write, Execute and Free
                Kernel32.WriteBytes(Handle, memory, asm);
                int result = Execute(memory);
                Free(memory);
            }
        }
        #endregion

        #region AlreadyOwnItem
        /// <summary>
        /// Automatically Get the item if needed
        /// </summary>
        public void AutomaticallyGetItem()
        {
            if (Hooked)
            {
                bool KightIsDead = ReadEventFlag(SelectedWeapon.Flag);
                bool alreadyOwns = AlreadyOwnItem();

                /**
                 * If the black knight is still alive, we reset the state of the Weapon
                 */
                if (!KightIsDead)
                {
                    SelectedWeapon.HasDropped = false;
                }
                // else the black knight is dead...
                else
                {
                    if (alreadyOwns)
                    {
                        if (!SelectedWeapon.HasDropped)
                        {
                            /**
                             * If the black knight is dead and the player has the weapon
                             * but we never gave it to him then it's a legit drop
                             */
                            SelectedWeapon.HasDropped = true;
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
                        if (!SelectedWeapon.HasDropped)
                        {
                            /**
                             * If the black knight is dead, the player doesn't have the
                             * weapon and we didn't already gave it, then we give it 
                             */
                            GetItem();
                            SelectedWeapon.HasDropped = true;
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
        /// Main AlreadyOwnItem that will call the correct GetInventory
        /// For PTDE or Remastered
        /// </summary>
        /// <returns>bool</returns>
        public bool AlreadyOwnItem()
        {
            // Depending on PTDE or Remastered
            InventoryItem[] items = (Version == Version.DarkSouls) ? GetInventoryPTDE() : GetInventoryRemastered();

            bool alreadyOwn = false;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].Category == SelectedWeapon.Category && items[i].ID == SelectedWeapon.ID)
                {
                    // Found the weapon in the inventory!
                    alreadyOwn = true;
                    break;
                }
            }

            return alreadyOwn;
        }

        /// <summary>
        /// GetInventory for Remastered
        /// </summary>
        private InventoryItem[] GetInventoryRemastered()
        {
            InventoryItem[] result = new InventoryItem[0];

            if (Hooked)
            {
                result = new InventoryItem[2048];
                IntPtr pointer = RemasterInventoryData.Resolve();
                byte[] bytes = CreateChildPointer(RemasterInventoryData, 0, 0x10, 0x3B8).ReadBytes(0, 2048 * 0x1C);

                for (int i = 0; i < 2048; i++)
                {
                    result[i] = new InventoryItem(bytes, i * 0x1C);
                }
            }

            return result;
        }

        /// <summary>
        /// GetInventory for PTDE
        /// </summary>
        private InventoryItem[] GetInventoryPTDE()
        {
            InventoryItem[] result = new InventoryItem[0];

            if (Hooked)
            {
                result = new InventoryItem[2048];
                IntPtr pointer = PTDEInventoryData.Resolve();
                byte[] bytes = CreateChildPointer(PTDEInventoryData, 0, 8, 0x2DC).ReadBytes(0, 2048 * 0x1C);

                for (int i = 0; i < 2048; i++)
                {
                    result[i] = new InventoryItem(bytes, i * 0x1C);
                }
            }

            return result;
        }
        #endregion

        #region Flags Methods
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
            return (Version == Version.DarkSouls)
                ? PTDEFlags.ReadFlag32(offset, mask)
                : RemasterFlags.ReadFlag32(offset, mask);
        }
        #endregion
    }
}
