using System;
using System.Threading;
using PropertyHook;

namespace Gadgetlemage.DarkSouls
{
    public class PrepareToDie : DarkSouls
    {
        /// <summary>
        /// Constants
        /// </summary>
        private const string BASE_PTR_AOB = "8B 0D ? ? ? ? 8B 7E 1C 8B 49 08 8B 46 20";
        private const string INVENTORY_DATA_AOB = "A1 ? ? ? ? 53 55 8B 6C 24 10 56 8B 70 08 32 DB 85 F6";
        private const string FLAGS_AOB = "56 8B F1 8B 46 1C 50 A1 ? ? ? ? 32 C9";
        private const string CHR_DATA_AOB = "83 EC 14 A1 ? ? ? ? 8B 48 04 8B 40 08 53 55 56 57 89 4C 24 1C 89 44 24 20 3B C8";
        private const uint INVENTORY_INDEX_START = 0x1B8;
        private const uint FUNC_ITEM_GET_PTR = 0xC0B6DA;

        /// <summary>
        /// Properties
        /// </summary>
        public PHPointer pBasePtr { get; private set; }
        public PHPointer pInventoryData { get; private set; }

        /// <summary>
        /// Constructor
        /// Needs to RescanAOB() for pointers to update
        /// </summary>
        /// <param name="process"></param>
        public PrepareToDie(PHook process) : base(process)
        {
            // Set pointers
            base.pFlags = Process.RegisterAbsoluteAOB(FLAGS_AOB, 8, 0, 0);
            base.pLoaded = Process.RegisterAbsoluteAOB(CHR_DATA_AOB, 4, 0, 0x4, 0x0);

            pBasePtr = Process.RegisterAbsoluteAOB(BASE_PTR_AOB, 2);
            pInventoryData = Process.RegisterAbsoluteAOB(INVENTORY_DATA_AOB, 1);

            Process.RescanAOB();
        }

        /// <summary>
        /// Creates the SelectedWeapon directly into the player's inventory
        /// </summary>
        public override void CreateWeapon(BlackKnightWeapon weapon)
        {
            if (Process.Hooked)
            {
                byte[] asm = (byte[])Assembly.PTDE.Clone();

                // Get the pointer to CharBasePtr
                IntPtr pointer = pBasePtr.Resolve();
                pointer = Process.CreateChildPointer(pBasePtr, 0, 8).Resolve();

                // Have to allocate first to rebase the code
                IntPtr memory = Process.Allocate((uint)asm.Length);
                uint funcPointer = (uint)(FUNC_ITEM_GET_PTR - (uint)memory);

                // Now we can write the rebased bytes
                byte[] bytes = BitConverter.GetBytes((ulong)pointer + INVENTORY_INDEX_START);
                Array.Copy(bytes, 0, asm, 0x1, 4);
                bytes = BitConverter.GetBytes(ItemCategory);
                Array.Copy(bytes, 0, asm, 0x6, 4);
                bytes = BitConverter.GetBytes(weapon.ID);
                Array.Copy(bytes, 0, asm, 0xB, 4);
                bytes = BitConverter.GetBytes(ItemQuantity);
                Array.Copy(bytes, 0, asm, 0x10, 4);
                bytes = BitConverter.GetBytes((ulong)funcPointer);
                Array.Copy(bytes, 0, asm, 0x22, 4);

                // Write, Execute and Free
                Kernel32.WriteBytes(Process.Handle, memory, asm);
                int result = Process.Execute(memory);
                Process.Free(memory);
            }
        }

        /// <summary>
        /// Returns the player's inventory
        /// </summary>
        /// <returns></returns>
        public override InventoryItem[] GetInventoryItems()
        {
            InventoryItem[] result = new InventoryItem[0];

            if (Process.Hooked)
            {
                result = new InventoryItem[2048];
                IntPtr pointer = pInventoryData.Resolve();
                byte[] bytes = Process.CreateChildPointer(pInventoryData, 0, 8, 0x2DC).ReadBytes(0, 2048 * 0x1C);

                for (int i = 0; i < 2048; i++)
                {
                    result[i] = new InventoryItem(bytes, i * 0x1C);
                }
            }

            return result;
        }

        /// <summary>
        /// Removes a weapon from the player's inventory
        /// </summary>
        /// <param name="weapon"></param>
        public override void DeleteItem(BlackKnightWeapon weapon)
        {
            InventoryItem[] result = new InventoryItem[0];

            if (Process.Hooked)
            {
                result = new InventoryItem[2048];
                IntPtr pointer = pInventoryData.Resolve();
                PHPointer pInventory = Process.CreateChildPointer(pInventoryData, 0, 8, 0x2DC);
                byte[] bytes = pInventory.ReadBytes(0, 2048 * 0x1C);

                for (int i = 0; i < 2048; i++)
                {
                    result[i] = new InventoryItem(bytes, i * 0x1C);
                    if (result[i].Category == weapon.Category && result[i].ID == weapon.ID)
                    {
                        pInventory.WriteBytes(i * 0x1C, new byte[0x1C]);
                    }
                }
            }
        }
    }
}
