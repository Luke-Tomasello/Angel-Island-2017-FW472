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

/* Scripts\Commands\Dupe.cs
 * CHANGELOG
 *	5/29/10/ Adam
 *		Make CopyProperties public
 *	5/7/10, Adam
 *		New [Dupe implementation that allows the area modifier, i.e., [area dupe
 *		Scripts/Commands/Abstracted/Commands/Commands.cs
 */

using Server.Items;
using Server.Targeting;
using System;
using System.Reflection;

namespace Server.Commands
{
    public class Dupe
    {
        public static void Initialize()
        {   // adam: see comment section
            //Server.Commands.Register("Dupe", AccessLevel.GameMaster, new CommandEventHandler(Dupe_OnCommand));
            Server.CommandSystem.Register("DupeInBag", AccessLevel.GameMaster, new CommandEventHandler(DupeInBag_OnCommand));
        }

        [Usage("Dupe [amount]")]
        [Description("Dupes a targeted item.")]
        private static void Dupe_OnCommand(CommandEventArgs e)
        {
            int amount = 1;
            if (e.Length >= 1)
                amount = e.GetInt32(0);
            e.Mobile.Target = new DupeTarget(false, amount > 0 ? amount : 1);
            e.Mobile.SendMessage("What do you wish to dupe?");
        }

        [Usage("DupeInBag <count>")]
        [Description("Dupes an item at it's current location (count) number of times.")]
        private static void DupeInBag_OnCommand(CommandEventArgs e)
        {
            int amount = 1;
            if (e.Length >= 1)
                amount = e.GetInt32(0);

            e.Mobile.Target = new DupeTarget(true, amount > 0 ? amount : 1);
            e.Mobile.SendMessage("What do you wish to dupe?");
        }

        private class DupeTarget : Target
        {
            private bool m_InBag;
            private int m_Amount;

            public DupeTarget(bool inbag, int amount)
                : base(15, false, TargetFlags.None)
            {
                m_InBag = inbag;
                m_Amount = amount;
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                bool done = false;
                if (!(targ is Item))
                {
                    from.SendMessage("You can only dupe items.");
                    return;
                }

                CommandLogging.WriteLine(from, "{0} {1} duping {2} (inBag={3}; amount={4})", from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(targ), m_InBag, m_Amount);

                Item copy = (Item)targ;
                Container pack;

                if (m_InBag)
                {
                    if (copy.Parent is Container)
                        pack = (Container)copy.Parent;
                    else if (copy.Parent is Mobile)
                        pack = ((Mobile)copy.Parent).Backpack;
                    else
                        pack = null;
                }
                else
                    pack = from.Backpack;

                Type t = copy.GetType();

                ConstructorInfo[] info = t.GetConstructors();

                foreach (ConstructorInfo c in info)
                {
                    //if ( !c.IsDefined( typeof( ConstructableAttribute ), false ) ) continue;

                    ParameterInfo[] paramInfo = c.GetParameters();

                    if (paramInfo.Length == 0)
                    {
                        object[] objParams = new object[0];

                        try
                        {
                            from.SendMessage("Duping {0}...", m_Amount);
                            for (int i = 0; i < m_Amount; i++)
                            {
                                object o = c.Invoke(objParams);

                                if (o != null && o is Item)
                                {
                                    Item newItem = (Item)o;
                                    CopyProperties(newItem, copy);//copy.Dupe( item, copy.Amount );
                                    newItem.Parent = null;

                                    if (pack != null)
                                        pack.DropItem(newItem);
                                    else
                                        newItem.MoveToWorld(from.Location, from.Map);
                                }
                            }
                            from.SendMessage("Done");
                            done = true;
                        }
                        catch
                        {
                            from.SendMessage("Error!");
                            return;
                        }
                    }
                }

                if (!done)
                {
                    from.SendMessage("Unable to dupe.  Item must have a 0 parameter constructor.");
                }
            }
        }

        public static void CopyProperties(Item dest, Item src)
        {
            PropertyInfo[] props = src.GetType().GetProperties();

            for (int i = 0; i < props.Length; i++)
            {
                try
                {
                    if (props[i].CanRead && props[i].CanWrite)
                    {
                        //Console.WriteLine( "Setting {0} = {1}", props[i].Name, props[i].GetValue( src, null ) );
                        props[i].SetValue(dest, props[i].GetValue(src, null), null);
                    }
                }
                catch
                {
                    //Console.WriteLine( "Denied" );
                }
            }
        }
    }
}
