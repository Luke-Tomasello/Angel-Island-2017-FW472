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

/* Items/Deeds/DoorRekeyingContract.cs
 * ChangeLog:
 *  5/29/07, Adam
 *      Remove unused Credits property
 *	05/22/07, Adam
 *      first time checkin
 */

using Server.Multis;
using Server.Targeting;

namespace Server.Items
{
    public class DoorRekeyingContract : Item // Create the item class which is derived from the base item class
    {
        [Constructable]
        public DoorRekeyingContract()
            : base(0x14F0)
        {
            Weight = 1.0;
            Name = "a contract for door rekeying";
            LootType = LootType.Regular;
        }

        public DoorRekeyingContract(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteInt32((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt32();
        }

        public override bool DisplayLootType { get { return false; } }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.Backpack == null || !IsChildOf(from.Backpack)) // Make sure its in their pack
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                from.SendMessage("Please target the house door you wish to rekey.");
                from.Target = new DoorRekeyingContractTarget(this); // Call our target
            }
        }
    }

    public class DoorRekeyingContractTarget : Target
    {
        private DoorRekeyingContract m_Deed;

        public DoorRekeyingContractTarget(DoorRekeyingContract deed)
            : base(2, false, TargetFlags.None)
        {
            m_Deed = deed;
        }

        protected override void OnTarget(Mobile from, object target) // Override the protected OnTarget() for our feature
        {
            if (target is BaseDoor)
            {
                BaseDoor door = target as BaseDoor;
                BaseHouse h1 = BaseHouse.FindHouseAt(door);
                BaseHouse h2 = BaseHouse.FindHouseAt(from);
                if (h1 == null || h1 != h2)
                {
                    from.SendLocalizedMessage(502094); // You must be in your house to do this.
                    return;
                }
                else if (h1.IsOwner(from) == false)
                {
                    from.SendLocalizedMessage(501303); // Only the house owner may change the house locks.
                    return;
                }

                // don't remove old keys because you will endup removing the main house keys
                //  we need to single this door out somehow
                // Key.RemoveKeys( from, oldKeyValue );

                // make the keys
                uint keyValue = Key.RandomValue();
                Key packKey = new Key(KeyType.Gold);
                Key bankKey = new Key(KeyType.Gold);
                packKey.KeyValue = keyValue;
                bankKey.KeyValue = keyValue;
                BankBox box = from.BankBox;
                if (box == null || !box.TryDropItem(from, bankKey, false))
                    bankKey.Delete();
                from.AddToBackpack(packKey);

                // rekey door
                door.KeyValue = keyValue;

                from.SendMessage("The lock on this door has been changed, and new master key has been placed in your bank and your backpack.");
                m_Deed.Delete(); // Delete the deed                
            }
            else
            {
                from.SendMessage("That is not a door.");
            }
        }
    }
}
