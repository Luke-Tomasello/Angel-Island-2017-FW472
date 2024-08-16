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

/* Scripts/Mobiles/Vendors/SBInfo/SBVarietyDealer.cs
 * ChangeLog
 *  10/14/04, Froste
 *      Changed the amount argument to GenericBuyInfo from 999 to 20 for reagents, so the argument means something in GenericBuy.cs
 *  
 */

using Server.Items;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class SBVarietyDealer : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBVarietyDealer()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Bandage), 5, 20, 0xE21, 0));

                Add(new GenericBuyInfo(typeof(BlankScroll), 5, 20, 0x0E34, 0));

                Add(new GenericBuyInfo(typeof(NightSightPotion), 15, 10, 0xF06, 0));
                Add(new GenericBuyInfo(typeof(AgilityPotion), 15, 10, 0xF08, 0));
                Add(new GenericBuyInfo(typeof(StrengthPotion), 15, 10, 0xF09, 0));
                Add(new GenericBuyInfo(typeof(RefreshPotion), 15, 10, 0xF0B, 0));
                Add(new GenericBuyInfo(typeof(LesserCurePotion), 15, 10, 0xF07, 0));
                Add(new GenericBuyInfo(typeof(LesserHealPotion), 15, 10, 0xF0C, 0));
                Add(new GenericBuyInfo(typeof(LesserPoisonPotion), 15, 10, 0xF0A, 0));
                Add(new GenericBuyInfo(typeof(LesserExplosionPotion), 21, 10, 0xF0D, 0));

                Add(new GenericBuyInfo(typeof(Bolt), 6, Utility.Random(30, 60), 0x1BFB, 0));
                Add(new GenericBuyInfo(typeof(Arrow), 3, Utility.Random(30, 60), 0xF3F, 0));

                Add(new GenericBuyInfo(typeof(BlackPearl), 5, 20, 0xF7A, 0));
                Add(new GenericBuyInfo(typeof(Bloodmoss), 5, 20, 0xF7B, 0));
                Add(new GenericBuyInfo(typeof(MandrakeRoot), 3, 20, 0xF86, 0));
                Add(new GenericBuyInfo(typeof(Garlic), 3, 20, 0xF84, 0));
                Add(new GenericBuyInfo(typeof(Ginseng), 3, 20, 0xF85, 0));
                Add(new GenericBuyInfo(typeof(Nightshade), 3, 20, 0xF88, 0));
                Add(new GenericBuyInfo(typeof(SpidersSilk), 3, 20, 0xF8D, 0));
                Add(new GenericBuyInfo(typeof(SulfurousAsh), 3, 20, 0xF8C, 0));

                Add(new GenericBuyInfo(typeof(BreadLoaf), 7, 10, 0x103B, 0));
                Add(new GenericBuyInfo(typeof(Backpack), 15, 20, 0x9B2, 0));

                Type[] types = Loot.RegularScrollTypes;

                int circles = 3;

                for (int i = 0; i < circles * 8 && i < types.Length; ++i)
                {
                    int itemID = 0x1F2E + i;

                    if (i == 6)
                        itemID = 0x1F2D;
                    else if (i > 6)
                        --itemID;

                    Add(new GenericBuyInfo(types[i], 12 + ((i / 8) * 10), 20, itemID, 0));
                }

                if (Core.AOS)
                {
                    Add(new GenericBuyInfo(typeof(BatWing), 3, 20, 0xF78, 0));
                    Add(new GenericBuyInfo(typeof(GraveDust), 3, 20, 0xF8F, 0));
                    Add(new GenericBuyInfo(typeof(DaemonBlood), 6, 20, 0xF7D, 0));
                    Add(new GenericBuyInfo(typeof(NoxCrystal), 6, 20, 0xF8E, 0));
                    Add(new GenericBuyInfo(typeof(PigIron), 5, 20, 0xF8A, 0));

                    Add(new GenericBuyInfo(typeof(NecromancerSpellbook), 115, 10, 0x2253, 0));
                }

                Add(new GenericBuyInfo(typeof(RecallRune), 15, 10, 0x1f14, 0));
                Add(new GenericBuyInfo(typeof(Spellbook), 18, 10, 0xEFA, 0));

                Add(new GenericBuyInfo("1041072", typeof(MagicWizardsHat), 11, 10, 0x1718, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (!Core.UOAI && !Core.UOAR && !Core.UOSP && !Core.UOMO)
                {   // cash buyback
                    Add(typeof(Bandage), 2);

                    Add(typeof(BlankScroll), 3);

                    Add(typeof(NightSightPotion), 7);
                    Add(typeof(AgilityPotion), 7);
                    Add(typeof(StrengthPotion), 7);
                    Add(typeof(RefreshPotion), 7);
                    Add(typeof(LesserCurePotion), 7);
                    Add(typeof(LesserHealPotion), 7);
                    Add(typeof(LesserPoisonPotion), 7);
                    Add(typeof(LesserExplosionPotion), 10);

                    Add(typeof(Bolt), 3);
                    Add(typeof(Arrow), 2);

                    Add(typeof(BlackPearl), 3);
                    Add(typeof(Bloodmoss), 3);
                    Add(typeof(MandrakeRoot), 2);
                    Add(typeof(Garlic), 2);
                    Add(typeof(Ginseng), 2);
                    Add(typeof(Nightshade), 2);
                    Add(typeof(SpidersSilk), 2);
                    Add(typeof(SulfurousAsh), 2);

                    Add(typeof(BreadLoaf), 3);
                    Add(typeof(Backpack), 7);
                    Add(typeof(RecallRune), 8);
                    Add(typeof(Spellbook), 9);
                    Add(typeof(BlankScroll), 3);
                }

                if (Core.AOS)
                {
                    Add(typeof(BatWing), 2);
                    Add(typeof(GraveDust), 2);
                    Add(typeof(DaemonBlood), 3);
                    Add(typeof(NoxCrystal), 3);
                    Add(typeof(PigIron), 3);
                }

                if (!Core.UOAI && !Core.UOAR && !Core.UOSP && !Core.UOMO)
                {   // cash buyback
                    Type[] types = Loot.RegularScrollTypes;

                    for (int i = 0; i < types.Length; ++i)
                        Add(types[i], 6 + ((i / 8) * 5));
                }
            }
        }
    }
}
