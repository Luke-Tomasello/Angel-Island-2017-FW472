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

/* Scripts/Mobiles/Vendors/NPC/OrcMerchant.cs
 * ChangeLog
 *  10/18/04, Froste
 *      Modified Restock to use OnRestock() because it's fixed now
 *  10/17/04, Froste
 *      Created this modified version of Importer.cs
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	4/29/04, mith
 *		Modified Restock to use OnRestockReagents() to restock 100 of each item instead of only 20.
 */

using System;
using System.Collections;

namespace Server.Mobiles
{
    public class OrcMerchant : BaseVendor
    {
        public ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        //public override NpcGuild NpcGuild{ get{ return NpcGuild.MagesGuild; } }

        [Constructable]
        public OrcMerchant()
            : base("the orc merchant")
        {
            /*			SetSkill( SkillName.EvalInt, 65.0, 88.0 );
			 *			SetSkill( SkillName.Inscribe, 60.0, 83.0 );
			 *			SetSkill( SkillName.Magery, 64.0, 100.0 );
			 *			SetSkill( SkillName.Meditation, 60.0, 83.0 );
			 *			SetSkill( SkillName.MagicResist, 65.0, 88.0 );
			 *			SetSkill( SkillName.Wrestling, 36.0, 68.0 );
			 */
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBOrcMerchant());
        }

        /*		public override VendorShoeType ShoeType
		 *		{
		 *			get{ return Utility.RandomBool() ? VendorShoeType.Shoes : VendorShoeType.Sandals; }
		 *		}
		 */
        public override void InitBody()
        {
            Name = NameList.RandomName("orc");
            Body = 17;
            BaseSoundID = 0x45A;

            SetStr(96, 120);
            SetDex(81, 105);
            SetInt(36, 60);

            SpeechHue = Utility.RandomDyedHue();

            NameHue = CalcInvulNameHue();

        }

        public override void Restock()
        {
            base.LastRestock = DateTime.Now;

            IBuyItemInfo[] buyInfo = this.GetBuyInfo();

            foreach (IBuyItemInfo bii in buyInfo)
                bii.OnRestock(); // change bii.OnRestockReagents() to OnRestock()
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (from.InRange(this.Location, 2))
                return true;

            return base.HandlesOnSpeech(from);
        }

        /*      public override void OnSpeech(SpeechEventArgs e)
		 *    {
		 *        base.OnSpeech( e );
		 *         this.Say("Leave these halls before it is too late!");
		 *    }
		 */

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i] is ContextMenus.VendorBuyEntry)
                    list.RemoveAt(i--);
                else if (list[i] is ContextMenus.VendorSellEntry)
                    list.RemoveAt(i--);
            }
        }

        public OrcMerchant(Serial serial)
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

            NameHue = CalcInvulNameHue();
        }

    }
}
