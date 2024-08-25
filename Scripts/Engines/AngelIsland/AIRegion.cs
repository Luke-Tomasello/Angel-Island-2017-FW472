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

#if OLD_REGIONS


/* Scripts/Regions/AngelIsland.cs
 * ChangeLog:
 *	05/07/09, plasma
 *		Upgraded to custom region
 *	5/2/04, mith
 *		Fixed the OnMobileAdded() so that players that login to AI with less than 5 STC aren't automagically kicked off the island.
 *
 *	4/26/04, mith
 *		Fixed a bug in OnMobileAdded(), we were checking Inamte flag when this has not been set before the Mobile is added to the region
 *		This was causing everyone to get kicked out. Now we check their Short-Term Murder counts.
 *
 *	4/14/04, mith
 *		modified OnMoveInto() to check Inmate flag and AccessLevel rather than number of counts.
 *
 *	4/13/04, mith
 *		Added code to prevent someone stranded at at sea (i.e. log out on boat, login second char, move boat)
 *		from popping onto AI if it's the nearest piece of land.
 *	
 * 3/26/04 Script changes by mith
 *		Added check in OnMoveInto() to verify that the person being blocked is a player. Otherwise, NPCs won't wander.
 *		Removed GuardedRegion inheritance.
 *		AllowHousing() copied from NoHousingRegion to prevent house placement.
 *		Added Initialization() function so we no longer need to create this region from the Town.cs file.
 *		Added check to disallow blues from enterring on foot. Still need to look into teleporters.
 *		Added experimental disabling of Help>Stuck menu, requires more testing.
 *
 *	3/21/04 Script created by mith, inherits from GuardedRegion.cs
 *		Gives message on Enter and Exit (more code needed to disallow boats, etc).
 *		Prevents casting of Teleport, Recall, Mark, and Gate.
 *		By inheriting from GuardedRegion, things like NoHousing are already implicit.
 *		Boats have been disabled via the BaseBoat.cs class, please see comments there.
 */
using System;
using Server;
using Server.SMTP;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Threading;
using System.Net.Mime;
using Server.Items;
using Server.Mobiles;
using Server.Misc;
using Server.Scripts.Commands;
using System.Reflection;
using System.Collections;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections.Generic;
using Server.Network;
using DiagELog = System.Diagnostics.EventLog;
using Server.Accounting;
using Server.Guilds;
using System.Net;
using System.Net.Sockets;
using Server.Targeting;
using Server.ContextMenus;
using Server.Gumps;
using Server.HuePickers;
using Server.Menus;
using Server.Prompts;
using System.Xml;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using System.CodeDom.Compiler;
using System.Security.Cryptography;
using Server.Commands;
using CPA = Server.CommandPropertyAttribute;
using Server.Regions;
using Server.Spells;
using Server.Engines.Quests.Haven;
using Server.Engines.Quests.Necro;
using Server.Multis;
using Server.Engines.BulkOrders;
using Server.Engines.RewardSystem;
using Server.Menus.ItemLists;
using Server.Menus.Questions;
using Server.Targets;
using Server.BountySystem;
using Server.Scripts.Gumps;
using Server.Spells.Seventh;
using Server.Spells.Fourth;
using Server.Engines.Plants;
using Server.Engines.CronScheduler;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using Server.Multis.Deeds;
using Server.Engines.PartySystem;
using Server.Factions;
using Server.Engines.Quests;
using Server.Spells.Fifth;
using Server.Spells.Sixth;
using Server.Engines.Harvest;
using Server.Engines.Quests.Hag;
using Server.Diagnostics;
using CV = Server.ClientVersion;
using Server.Spells.Third;
using Server.Engines;
using Server.Engines.ChampionSpawn;
using Server.Engines.IOBSystem;
using Mat = Server.Engines.BulkOrders.BulkMaterialType;
using Server.Engines.Craft;
using Server.Engines.Quests.Collector;
using System.Data.Odbc;
using CalcMoves = Server.Movement.Movement;
using MoveImpl = Server.Movement.MovementImpl;
using Server.PathAlgorithms;
using Server.PathAlgorithms.FastAStar;
using Server.PathAlgorithms.NavStar;
using Server.PathAlgorithms.SlowAStar;
using Server.Engines.ResourcePool;
using System.Drawing;
using System.Drawing.Imaging;
using Server.Township;
using AMA = Server.Items.ArmorMeditationAllowance;
using AMT = Server.Items.ArmorMaterialType;
using Server.Spells.First;
using Server.Spells.Second;
using System.Runtime.Serialization.Formatters.Binary;
using Haven = Server.Engines.Quests.Haven;
using Necro = Server.Engines.Quests.Necro;
using Custom.Gumps;
using ZLR.VM;
using Server.Multis.StaticHousing;
using Server.PathAlgorithms.Sector;
using Server.SkillHandlers;
using Server.Spells.NPC;
using System.Reflection.Emit;
using ZLR.IFF;
using ZLR.VM.Debugging;
using Server.Engines.Quests.Doom;
using Server.Engines.OldSchoolCraft;
using Server.Spells.Ninjitsu;
using Server.Text;
using Server.Ethics;
using Server.Factions.AI;

namespace Server.Regions
{
	public class  AngelIsland : CustomRegion
	{
		public AngelIsland(RegionControl rc, Map map)
			: base(rc, map)
		{
		}

		public override bool CanUseStuckMenu(Mobile m)
		{
			return false;
		}

		public RegionControl Controller
		{
			get
			{
				if (IsControllerGood())
				{
					return (RegionControl)this.m_Controller;
				}
				else
				{
					return null;
				}
			}
		}

		private bool IsControllerGood()
		{
			return (this.m_Controller != null && this.m_Controller is RegionControl);
		}

		public override bool OnBeginSpellCast(Mobile m, ISpell s)
		{
			if ((s is TeleportSpell || s is RecallSpell || s is GateTravelSpell || s is MarkSpell) && m.AccessLevel == AccessLevel.Player)
			{
				m.SendMessage("You cannot cast that spell here.");
				return false;
			}
			else
			{
				return base.OnBeginSpellCast(m, s);
			}
		}

		public override bool AllowHousing(Point3D p)
		{
			return false;
		}

		public override void OnEnter(Mobile m)
		{
			m.SendMessage("You are now on Angel Island.");
		}

		public override void OnExit(Mobile m)
		{
			m.SendMessage("You have left Angel Island.");
		}

		public override bool OnMoveInto(Mobile m, Direction d, Point3D newLocation, Point3D oldLocation)
		{
			// If someone attempting to enter the region is not an Inmate, we deny them entry.
			// The only way in is via the teleporter, which sets the Inmate flag true.
			if (m.Player)
				if (!((PlayerMobile)m).Inmate && m.AccessLevel == AccessLevel.Player)
					return false;

			return true;
		}

		public override void OnMobileAdd(Mobile m)
		{
			if (m.Player)
				if (m.ShortTermMurders < 5 && m.AccessLevel == AccessLevel.Player)
					if (!((PlayerMobile)m).Inmate && m.ShortTermMurders > 0)
						m.Location = new Point3D(373, 870, 0);
		}

	}


	public class  AngelIslandRegionStone : Server.Items.RegionControl
	{


		/// <summary>
		/// Initializes a new instance of the <see cref="KinCityRegionStone"/> class.
		/// </summary>
		[Constructable]
		public AngelIslandRegionStone()
			: base()
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="KinCityRegionStone"/> class.
		/// </summary>
		/// <param name="serial">The serial.</param>
		public AngelIslandRegionStone(Serial serial)
			: base(serial)
		{

		}

		/// <summary>
		/// Deletes this instance.
		/// </summary>
		public override void Delete()
		{
			try
			{

			}
			catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

			base.Delete();
		}

		public override CustomRegion CreateRegion(Server.Items.RegionControl rc, Map map)
		{
			return new AngelIsland(rc, map);
		}
		//	public void On

		/// <summary>
		/// Serializes the specified writer.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);

			writer.Write((int)0); // version

		}

		/// <summary>
		/// Deserializes the specified reader.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);

			int version = reader.ReadInt();

			switch (version)
			{
				case 0:
					break;
			}

		}

	}

}
#endif
