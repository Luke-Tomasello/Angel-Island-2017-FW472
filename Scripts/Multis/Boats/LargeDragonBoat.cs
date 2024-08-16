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

namespace Server.Multis
{
    public class LargeDragonBoat : BaseBoat
    {
        public override int NorthID { get { return 0x4014; } }
        public override int EastID { get { return 0x4015; } }
        public override int SouthID { get { return 0x4016; } }
        public override int WestID { get { return 0x4017; } }

        public override int HoldDistance { get { return 5; } }
        public override int TillerManDistance { get { return -5; } }

        public override Point2D StarboardOffset { get { return new Point2D(2, -1); } }
        public override Point2D PortOffset { get { return new Point2D(-2, -1); } }

        public override Point3D MarkOffset { get { return new Point3D(0, 0, 3); } }

        public override BaseDockedBoat DockedBoat { get { return new LargeDockedDragonBoat(this); } }

        [Constructable]
        public LargeDragonBoat()
        {
        }

        public LargeDragonBoat(Serial serial)
            : base(serial)
        {
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }
    }

    public class LargeDragonBoatDeed : BaseBoatDeed
    {
        public override int LabelNumber { get { return 1041210; } }// large dragon ship deed
        public override BaseBoat Boat { get { return new LargeDragonBoat(); } }

        [Constructable]
        public LargeDragonBoatDeed()
            : base(0x4014, new Point3D(0, -1, 0))
        {
        }

        public LargeDragonBoatDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }
    }

    public class LargeDockedDragonBoat : BaseDockedBoat
    {
        public override BaseBoat Boat { get { return new LargeDragonBoat(); } }

        public LargeDockedDragonBoat(BaseBoat boat)
            : base(0x4014, new Point3D(0, -1, 0), boat)
        {
        }

        public LargeDockedDragonBoat(Serial serial)
            : base(serial)
        {
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }
    }
}
