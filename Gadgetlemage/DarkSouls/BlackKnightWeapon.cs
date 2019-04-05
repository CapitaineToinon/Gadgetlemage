using System;
using System.Collections.Generic;

namespace Gadgetlemage.DarkSouls
{
    public class BlackKnightWeapon
    {
        /// <summary>
        /// Max upgrade for Black Knight weapons (+5)
        /// </summary>
        private const int MAX_UPGRADE = 5;

        /// <summary>
        /// Weapon's properties
        /// </summary>
        public int Category { get; private set; }
        public int ID { get; private set; }
        public List<int> Upgrade { get; private set; }
        public string Name { get; private set; }
        public int Flag { get; private set; }
        
        /// <summary>
        /// The condition for if the weapon needs to drop or not
        /// </summary>
        public Func<bool> IsConditionSatisfied { get; set; }

        /// <summary>
        /// Delegate constructor
        /// </summary>
        public BlackKnightWeapon(int category, int id, string name) : this(category, id, name, () => false)
        {
            // Emptry
        }

        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="category">Weapon's category</param>
        /// <param name="id">Weapon's id</param>
        /// <param name="name">Weapon's name</param>
        /// <param name="createCondition">The condition for if the weapon needs to drop or not</param>
        public BlackKnightWeapon(int category, int id, string name, Func<bool> createCondition)
        {
            Category = category;
            ID = id;

            Upgrade = new List<int>(1 + MAX_UPGRADE)
            {
                ID,
            };
            for (int i = 1; i <= MAX_UPGRADE; i++)
            {
                Upgrade.Add(ID + i);
            }

            Name = name;

            IsConditionSatisfied = createCondition;
        }

        /// <summary>
        /// Weapon's name
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }
    }
}
