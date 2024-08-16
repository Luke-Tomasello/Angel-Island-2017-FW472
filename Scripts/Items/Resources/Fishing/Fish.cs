/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shard   : March 25, 2004
 *   portions copyright      : (C) 2004-2024 Tomasello Software LLC.
 *   email                   : luke@tomasello.com
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

namespace Server.Items
{
    public class Fish : Item, ICarvable
    {
        public void Carve(Mobile from, Item item)
        {
            base.ScissorHelper(from, new RawFishSteak(), 4);
        }

        [Constructable]
        public Fish()
            : this(1)
        {
        }

        [Constructable]
        public Fish(int amount)
            : base(Utility.Random(0x09CC, 4))
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new Fish(amount), amount);
        }

        public Fish(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}
