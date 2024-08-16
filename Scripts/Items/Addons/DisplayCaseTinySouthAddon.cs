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

/*   changelog.
 *   08/03/06,Rhiannon
 *		Initial creation
 *
 *
 */
/////////////////////////////////////////////////
//
// Automatically generated by the
// AddonGenerator script by Arya
//
/////////////////////////////////////////////////
namespace Server.Items
{
    public class DisplayCaseTinySouthAddon : BaseAddon
    {
        public override BaseAddonDeed Deed
        {
            get
            {
                return new DisplayCaseTinySouthAddonDeed();
            }
        }

        [Constructable]
        public DisplayCaseTinySouthAddon()
        {
            AddComponent(new AddonComponent(2826), 0, 0, 2);
            AddComponent(new AddonComponent(2828), 0, 0, 0);
            AddComponent(new AddonComponent(2828), 0, 0, 4);
            // AddonComponent ac = null;

        }

        public DisplayCaseTinySouthAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // Version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class DisplayCaseTinySouthAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new DisplayCaseTinySouthAddon();
            }
        }

        [Constructable]
        public DisplayCaseTinySouthAddonDeed()
        {
            Name = "tiny display case (south)";
        }

        public DisplayCaseTinySouthAddonDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // Version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
