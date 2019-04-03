using System;

namespace WPF.Gadgetlemage
{
    public struct InventoryItem
    {
        public int Category { get; }

        public int ID { get; }

        public int Quantity { get; }

        public int Unk0C { get; }

        public int Unk10 { get; }

        public int Durability { get; }

        public int Unk18 { get; }

        public InventoryItem(byte[] bytes, int index)
        {
            Category = BitConverter.ToInt32(bytes, index + 0x00) >> 28;
            ID = BitConverter.ToInt32(bytes, index + 0x04);
            Quantity = BitConverter.ToInt32(bytes, index + 0x08);
            Unk0C = BitConverter.ToInt32(bytes, index + 0x0C);
            Unk10 = BitConverter.ToInt32(bytes, index + 0x10);
            Durability = BitConverter.ToInt32(bytes, index + 0x14);
            Unk18 = BitConverter.ToInt32(bytes, index + 0x18);
        }
    }
}
