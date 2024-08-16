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

/* ./Scripts/Mobiles/Animals/Cows/Bull.cs
 *	ChangeLog :
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 3 lines removed.
*/

namespace Server.Mobiles
{
    [CorpseName("a bull corpse")]
    public class Bull : BaseCreature
    {
        [Constructable]
        public Bull()
            : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            Name = "a bull";
            Body = Utility.RandomList(0xE8, 0xE9);
            BaseSoundID = 0x64;

            if (0.5 >= Utility.RandomDouble())
                Hue = 0x901;

            SetStr(77, 111);
            SetDex(56, 75);
            SetInt(47, 75);

            SetHits(50, 64);
            SetMana(0);

            SetDamage(4, 9);

            SetSkill(SkillName.MagicResist, 17.6, 25.0);
            SetSkill(SkillName.Tactics, 67.6, 85.0);
            SetSkill(SkillName.Wrestling, 40.1, 57.5);

            Fame = 600;
            Karma = 0;

            VirtualArmor = 28;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 71.1;
        }

        public override int Meat { get { return 10; } }
        public override int Hides { get { return 15; } }
        public override FoodType FavoriteFood { get { return FoodType.GrainsAndHay; } }
        public override PackInstinct PackInstinct { get { return PackInstinct.Bull; } }

        public Bull(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}
