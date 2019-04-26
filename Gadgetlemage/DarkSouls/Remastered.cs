using System;
using PropertyHook;

namespace Gadgetlemage.DarkSouls
{
    public class Remastered : DarkSouls
    {
        /// <summary>
        /// Constants
        /// </summary>
        private const string BASE_PTR_AOB = "48 8B 05 ? ? ? ? 48 85 C0 ? ? F3 0F 58 80 AC 00 00 00";
        private const string ITEM_ADDR_AOB = "48 89 5C 24 18 89 54 24 10 55 56 57 41 54 41 55 41 56 41 57 48 8D 6C 24 F9";
        private const string INVENTORY_DATA_AOB = "48 8B 05 ? ? ? ? 48 85 C0 ? ? F3 0F 58 80 AC 00 00 00";
        private const string FLAGS_AOB = "48 8B 0D ? ? ? ? 99 33 C2 45 33 C0 2B C2 8D 50 F6";
        private const string CHR_FOLLOW_CAM_AOB = "48 8B 0D ? ? ? ? E8 ? ? ? ? 48 8B 4E 68 48 8B 05 ? ? ? ? 48 89 48 60";

        /// <summary>
        /// Properties
        /// </summary>
        public PHPointer pBasePtr { get; set; }
        public PHPointer pItemAddr { get; set; }
        public PHPointer pInventoryData { get; set; }

        /// <summary>
        /// Constructor
        /// Needs to RescanAOB() for pointers to update
        /// </summary>
        /// <param name="process"></param>
        public Remastered(PHook process) : base(process)
        {
            // Set pointers
            base.pFlags = Process.RegisterRelativeAOB(FLAGS_AOB, 3, 7, 0, 0);
            base.pLoaded = Process.RegisterRelativeAOB(CHR_FOLLOW_CAM_AOB, 3, 7, 0, 0x60, 0x60);

            pBasePtr = Process.RegisterRelativeAOB(BASE_PTR_AOB, 3, 7);
            pItemAddr = Process.RegisterAbsoluteAOB(ITEM_ADDR_AOB);
            pInventoryData = Process.RegisterRelativeAOB(INVENTORY_DATA_AOB, 3, 7);
            
            Process.RescanAOB();
        }

        /// <summary>
        /// Creates the SelectedWeapon directly into the player's inventory
        /// </summary>
        public override void CreateWeapon(BlackKnightWeapon weapon)
        {
            if (Process.Hooked)
            {
                byte[] asm = (byte[])Assembly.REMASTERED.Clone();

                byte[] bytes = BitConverter.GetBytes(ItemCategory);
                Array.Copy(bytes, 0, asm, 0x1, 4);
                bytes = BitConverter.GetBytes(ItemQuantity);
                Array.Copy(bytes, 0, asm, 0x7, 4);
                bytes = BitConverter.GetBytes(weapon.ID);
                Array.Copy(bytes, 0, asm, 0xD, 4);
                bytes = BitConverter.GetBytes((ulong)pBasePtr.Resolve());
                Array.Copy(bytes, 0, asm, 0x19, 8);
                bytes = BitConverter.GetBytes((ulong)pItemAddr.Resolve());
                Array.Copy(bytes, 0, asm, 0x46, 8);

                Process.Execute(asm);
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
                byte[] bytes = Process.CreateChildPointer(pInventoryData, 0, 0x10, 0x3B8).ReadBytes(0, 2048 * 0x1C);

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
                PHPointer pInventory = Process.CreateChildPointer(pInventoryData, 0, 0x10, 0x3B8);
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
