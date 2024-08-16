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
    public class LargeStoneTableEastAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new LargeStoneTableEastDeed(); } }

        public override bool RetainDeedHue { get { return true; } }

        [Constructable]
        public LargeStoneTableEastAddon()
            : this(0)
        {
        }

        [Constructable]
        public LargeStoneTableEastAddon(int hue)
        {
            AddComponent(new AddonComponent(0x1202), 0, 0, 0);
            AddComponent(new AddonComponent(0x1203), 0, 1, 0);
            AddComponent(new AddonComponent(0x1201), 0, 2, 0);
            Hue = hue;
        }

        public LargeStoneTableEastAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class LargeStoneTableEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LargeStoneTableEastAddon(this.Hue); } }
        public override int LabelNumber { get { return 1044511; } } // large stone table (east)

        [Constructable]
        public LargeStoneTableEastDeed()
        {
        }

        public LargeStoneTableEastDeed(Serial serial)
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
