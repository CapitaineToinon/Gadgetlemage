namespace WPF.Gadgetlemage
{
    public class Weapon
    {
        public int Category { get; private set; }
        public int ID { get; private set; }
        public string Name { get; private set; }
        public int Flag { get; private set; }
        public bool HasDropped { get; set; }

        public Weapon(int category, int id, string name, int flag)
        {
            Category = category;
            ID = id;
            Name = name;
            Flag = flag;

            HasDropped = false;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
