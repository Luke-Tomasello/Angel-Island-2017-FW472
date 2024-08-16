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

/* Scripts/Gumps/AdminGump.cs
 * Changelog:
 * 3/15/16, Adam
 *		Reverse changes of 2/8/08
 *		Turn IPException code back on. This is because the IPException logic is per IP whereas the MaxAccountsPerIP
 *			functionality is global.
 *	3/1/11, Adam
 *		Replace "View all empty accounts" with "View all staff accounts"
 *		We don't really care about 'empty' since we have auto cleanup code that purges unused accounts.
 *		Also real estate is tight and no sense creating yet another pane if it's not needed.
 *	12/23/08, Adam
 *		Update to use RunUO 2.0 restart model
 *  7/22/08, Adam
 *      Add support for searching for IP addresses with Wild Cards, ie', 127.0.0.*
 *	7/8/08, Adam
 *		Add checks in AccountDetails_Activation for EmailAddress and ActivationKey being null.
 *		We've been getting crashes here and I've verified that EmailAddress == null on new account creation.
 *	7/7/08, Adam
 *		Replace exception logic for IPAddress.Parse(match) with normal error handling.
 *	2/18/08, Adam
 *		We now allow 3 accounts per household - IPException logic no longer needed
 *	9/2/07, Adam
 *		Move GetAccountInfo() from here to Account.cs 
 * 11/06/06, Pix
 *		Fixed Toggle Notification Emails functionality.
 * 10/15/06, Pix
 *		Added functionality for account.DoNotSendEmail.
 *  8/13/06, Rhiannon
 *		Fixed the switch statements to properly handle responses on the Administer-Access-Lockdown page.
 *  7/28/06, Rhiannon
 *		Fixed the Administer-Access-Lockdown page so all of the access levels display properly.
 *	7/28/06, Adam
 *		Remove OLD access levels
 *  7/24/06, Rhiannon
 *		Changed to use Mobile.GetHueForNameInList() instead of GetHueFor().
 *  07/22/06, Rhiannon
 *		Changed Owner text color to NPC yellow.
 *		Changed Fight Broker and Reporter text colors to their robe colors (valorite and agapite).
 *		Added Reporter and Fight Broker access levels in various places in the gump, 
 *		requiring some re-numbering of other buttons.
 *	03/14/06, Pix
 *		Put try-catch safeguard in constructor to guard against unknown crash.
 *	03/2/06, Pix
 *		Added resend account activation button.
 *		Changed "account list" to use "s around names so we can see leading and trailing spaces.
 *	01/1/05, Pix
 *		Fixed go-to account button on char info screen.
 *	12/30/05, Pig
 *		Added View Houses Button under client list options. Invokes ViewHouses command on selected player.
 *	12/15/05, Pix
 *		Made Account searching (Name/Email/IP) easier and less overloaded.
 *	12/05/05, Pix
 *		Added Activation Reset mechanism (for when email fails and need to reset password).
 *		Added IP Search functionality.
 *	7/01/05, Pix
 *		Added more idiotproofing to email search.
 *	6/28/05, Pix
 *		Added IP Details for IPException
 *	6/15/05, Pix
 *		Changed for auto-account, IPException, and Profile
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Accounting;
using Server.Commands;
using Server.Items;
using Server.Network;
using Server.Prompts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Server.Gumps
{
    public enum AdminGumpPage
    {
        Information,
        Administer,
        Clients,
        Accounts,
        Accounts_Shared,
        Firewall,
        IPException,
        IPException_IPDetails,
        Administer_WorldBuilding,
        Administer_Server,
        Administer_Access,
        Administer_Access_Lockdown,
        Administer_Commands,
        ClientInfo,
        AccountDetails,
        AccountDetails_Information,
        AccountDetails_Characters,
        AccountDetails_Access,
        AccountDetails_Access_ClientIPs,
        AccountDetails_Access_Restrictions,
        AccountDetails_Comments,
        AccountDetails_Tags,
        AccountDetails_ChangePassword,
        AccountDetails_ChangeAccess,
        FirewallInfo,
        AccountDetails_Activation,
        AccountDetails_EmailHistory
    }

    public class AdminGump : Gump
    {
        private Mobile m_From;
        private AdminGumpPage m_PageType;
        private ArrayList m_List;
        private int m_ListPage;
        private object m_State;

        private const int LabelColor = 0x7FFF;
        private const int SelectedColor = 0x421F;
        private const int DisabledColor = 0x4210;

        private const int LabelColor32 = 0xFFFFFF;
        private const int SelectedColor32 = 0x8080FF;
        private const int DisabledColor32 = 0x808080;

        private const int LabelHue = 0x480;
        private const int GreenHue = 0x40;
        private const int RedHue = 0x20;

        public void AddPageButton(int x, int y, int buttonID, string text, AdminGumpPage page, params AdminGumpPage[] subPages)
        {
            bool isSelection = (m_PageType == page);

            for (int i = 0; !isSelection && i < subPages.Length; ++i)
                isSelection = (m_PageType == subPages[i]);

            AddSelectedButton(x, y, buttonID, text, isSelection);
        }

        public void AddSelectedButton(int x, int y, int buttonID, string text, bool isSelection)
        {
            AddButton(x, y - 1, isSelection ? 4006 : 4005, 4007, buttonID, GumpButtonType.Reply, 0);
            AddHtml(x + 35, y, 200, 20, Color(text, isSelection ? SelectedColor32 : LabelColor32), false, false);
        }

        public void AddButtonLabeled(int x, int y, int buttonID, string text)
        {
            AddButton(x, y - 1, 4005, 4007, buttonID, GumpButtonType.Reply, 0);
            AddHtml(x + 35, y, 240, 20, Color(text, LabelColor32), false, false);
        }

        public string Center(string text)
        {
            return String.Format("<CENTER>{0}</CENTER>", text);
        }

        public string Color(string text, int color)
        {
            return String.Format("<BASEFONT COLOR=#{0:X6}>{1}</BASEFONT>", color, text);
        }

        public void AddBlackAlpha(int x, int y, int width, int height)
        {
            AddImageTiled(x, y, width, height, 2624);
            AddAlphaRegion(x, y, width, height);
        }

        public int GetButtonID(int type, int index)
        {
            return 1 + (index * 10) + type;
        }

        public static string FormatTimeSpan(TimeSpan ts)
        {
            return String.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}", ts.Days, ts.Hours % 24, ts.Minutes % 60, ts.Seconds % 60);
        }

        public static string FormatByteAmount(long totalBytes)
        {
            if (totalBytes > 1000000000)
                return String.Format("{0:F1} GB", (double)totalBytes / 1073741824);

            if (totalBytes > 1000000)
                return String.Format("{0:F1} MB", (double)totalBytes / 1048576);

            if (totalBytes > 1000)
                return String.Format("{0:F1} KB", (double)totalBytes / 1024);

            return String.Format("{0} Bytes", totalBytes);
        }

        public static void Initialize()
        {
            CommandSystem.Register("Admin", AccessLevel.Administrator, new CommandEventHandler(Admin_OnCommand));
        }

        [Usage("Admin")]
        [Description("Opens an interface providing server information and administration features including client, account, and firewall management.")]
        public static void Admin_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendGump(new AdminGump(e.Mobile, AdminGumpPage.Clients, 0, null, null, null));
        }

        //		public static int GetHueFor( Mobile m )
        //		{
        //			if ( m == null )
        //				return LabelHue;
        //
        //			switch ( m.AccessLevel )
        //			{
        //				case AccessLevel.Owner: return 0x35;
        //				case AccessLevel.Administrator: return 0x516;
        //				case AccessLevel.Seer: return 0x144;
        //				case AccessLevel.GameMaster: return 0x21;
        //				case AccessLevel.Counselor: return 0x2;
        //				case AccessLevel.FightBroker: return 0x8AB;
        //				case AccessLevel.Reporter: return 0x979;
        //				case AccessLevel.Player: default:
        //				{
        //					if ( m.Murderer )
        //						return 0x21;
        //					else if ( m.Criminal )
        //						return 0x3B1;
        //
        //					return 0x58;
        //				}
        //			}
        //		}

        //		private static string[] m_AccessLevelStrings = new string[]
        //			{
        //				"Player",
        //				"Reporter",
        //				"Fight Broker",
        //				"Counselor",
        //				"Game Master",
        //				"Seer",
        //				"Administrator",
        //				"Owner",
        //				"ReadOnly"
        //			};

        public static string FormatAccessLevel(AccessLevel level)
        {
            switch (level)
            {
                case AccessLevel.Ignore: return "Player";
                case AccessLevel.Player: return "Player";
                case AccessLevel.Reporter: return "Reporter";
                case AccessLevel.FightBroker: return "Fight Broker";
                case AccessLevel.Counselor: return "Counselor";
                case AccessLevel.GameMaster: return "Game Master";
                case AccessLevel.Seer: return "Seer";
                case AccessLevel.Administrator: return "Administrator";
                case AccessLevel.Owner: return "Owner";
                default: return "Unknown";
            }
        }

        public AdminGump(Mobile from, AdminGumpPage pageType, int listPage, ArrayList list, string notice, object state)
            : base(50, 40)
        {
            // 3/14/06 - safeguard put in against unknown crash
            // See comments for fix: 7/8/08, Adam
            try
            {
                from.CloseGump(typeof(AdminGump));

                m_From = from;
                m_PageType = pageType;
                m_ListPage = listPage;
                m_State = state;
                m_List = list;

                AddPage(0);

                AddBackground(0, 0, 420, 440, 5054);

                AddBlackAlpha(10, 10, 170, 100);
                AddBlackAlpha(190, 10, 220, 100);
                AddBlackAlpha(10, 120, 400, 260);
                AddBlackAlpha(10, 390, 400, 40);

                AddPageButton(10, 10, GetButtonID(0, 0), "INFORMATION", AdminGumpPage.Information);
                AddPageButton(10, 30, GetButtonID(0, 1), "ADMINISTER", AdminGumpPage.Administer, AdminGumpPage.Administer_Access, AdminGumpPage.Administer_Commands, AdminGumpPage.Administer_Server, AdminGumpPage.Administer_WorldBuilding, AdminGumpPage.Administer_Access_Lockdown);
                AddPageButton(10, 50, GetButtonID(0, 2), "CLIENT LIST", AdminGumpPage.Clients, AdminGumpPage.ClientInfo);
                AddPageButton(10, 70, GetButtonID(0, 3), "ACCOUNT LIST", AdminGumpPage.Accounts, AdminGumpPage.Accounts_Shared, AdminGumpPage.AccountDetails, AdminGumpPage.AccountDetails_Information, AdminGumpPage.AccountDetails_Characters, AdminGumpPage.AccountDetails_Access, AdminGumpPage.AccountDetails_Access_ClientIPs, AdminGumpPage.AccountDetails_Access_Restrictions, AdminGumpPage.AccountDetails_Comments, AdminGumpPage.AccountDetails_Tags, AdminGumpPage.AccountDetails_ChangeAccess, AdminGumpPage.AccountDetails_ChangePassword);
                AddPageButton(10, 90, GetButtonID(0, 4), "FIREWALL", AdminGumpPage.Firewall, AdminGumpPage.FirewallInfo);
                AddPageButton(110, 90, GetButtonID(0, 5), "IPEXC", AdminGumpPage.IPException);

                if (notice != null)
                    AddHtml(12, 392, 396, 36, Color(notice, LabelColor32), false, false);

                switch (pageType)
                {
                    case AdminGumpPage.Information:
                        {
                            int banned = 0;
                            int active = 0;

                            foreach (Account acct in Accounts.Table.Values)
                            {
                                if (acct.Banned)
                                    ++banned;
                                else
                                    ++active;
                            }

                            AddLabel(20, 130, LabelHue, "Active Accounts:");
                            AddLabel(150, 130, LabelHue, active.ToString());

                            AddLabel(20, 150, LabelHue, "Banned Accounts:");
                            AddLabel(150, 150, LabelHue, banned.ToString());

                            AddLabel(20, 170, LabelHue, "Firewalled:");
                            AddLabel(150, 170, LabelHue, Firewall.List.Count.ToString());

                            AddLabel(20, 190, LabelHue, "Clients:");
                            AddLabel(150, 190, LabelHue, NetState.Instances.Count.ToString());

                            AddLabel(20, 210, LabelHue, "Mobiles:");
                            AddLabel(150, 210, LabelHue, World.Mobiles.Count.ToString());

                            AddLabel(20, 230, LabelHue, "Mobile Scripts:");
                            AddLabel(150, 230, LabelHue, Core.ScriptMobiles.ToString());

                            AddLabel(20, 250, LabelHue, "Items:");
                            AddLabel(150, 250, LabelHue, World.Items.Count.ToString());

                            AddLabel(20, 270, LabelHue, "Item Scripts:");
                            AddLabel(150, 270, LabelHue, Core.ScriptItems.ToString());

                            AddLabel(20, 290, LabelHue, "Uptime:");
                            AddLabel(150, 290, LabelHue, FormatTimeSpan(DateTime.Now - Clock.ServerStart));

                            AddLabel(20, 310, LabelHue, "Memory:");
                            AddLabel(150, 310, LabelHue, FormatByteAmount(GC.GetTotalMemory(false)));

                            AddLabel(20, 330, LabelHue, "Framework:");
                            AddLabel(150, 330, LabelHue, Environment.Version.ToString());

                            AddLabel(20, 350, LabelHue, "Operating System: ");
                            AddLabel(150, 350, LabelHue, Environment.OSVersion.ToString());

                            /*string str;

							try{ str = FormatTimeSpan( Core.Process.TotalProcessorTime ); }
							catch{ str = "(unable to retrieve)"; }

							AddLabel( 20, 330, LabelHue, "Process Time:" );
							AddLabel( 250, 330, LabelHue, str );*/

                            /*try{ str = Core.Process.PriorityClass.ToString(); }
							catch{ str = "(unable to retrieve)"; }

							AddLabel( 20, 350, LabelHue, "Process Priority:" );
							AddLabel( 250, 350, LabelHue, str );*/

                            break;
                        }
                    case AdminGumpPage.Administer_WorldBuilding:
                        {

                            AddHtml(10, 125, 400, 20, Color(Center("Generating"), LabelColor32), false, false);

                            AddButtonLabeled(20, 150, GetButtonID(3, 100), "Documentation");
                            AddButtonLabeled(220, 150, GetButtonID(3, 107), "Rebuild Categorization");

                            AddButtonLabeled(20, 175, GetButtonID(3, 101), "Teleporters");
                            AddButtonLabeled(220, 175, GetButtonID(3, 102), "Moongates");

                            AddButtonLabeled(20, 200, GetButtonID(3, 103), "Vendors");
                            AddButtonLabeled(220, 200, GetButtonID(3, 106), "Decoration");

                            AddButtonLabeled(20, 225, GetButtonID(3, 104), "Doors");
                            AddButtonLabeled(220, 225, GetButtonID(3, 105), "Signs");

                            AddHtml(20, 275, 400, 30, Color(Center("Statics"), LabelColor32), false, false);

                            AddButtonLabeled(20, 300, GetButtonID(3, 110), "Freeze (Target)");
                            AddButtonLabeled(20, 325, GetButtonID(3, 111), "Freeze (World)");
                            AddButtonLabeled(20, 350, GetButtonID(3, 112), "Freeze (Map)");

                            AddButtonLabeled(220, 300, GetButtonID(3, 120), "Unfreeze (Target)");
                            AddButtonLabeled(220, 325, GetButtonID(3, 121), "Unfreeze (World)");
                            AddButtonLabeled(220, 350, GetButtonID(3, 122), "Unfreeze (Map)");

                            goto case AdminGumpPage.Administer;
                        }
                    case AdminGumpPage.Administer_Server:
                        {

                            AddHtml(10, 125, 400, 20, Color(Center("Server"), LabelColor32), false, false);

                            AddButtonLabeled(20, 150, GetButtonID(3, 200), "Save");

                            if (!Core.Service)
                            {
                                AddButtonLabeled(20, 180, GetButtonID(3, 201), "Shutdown (With Save)");
                                AddButtonLabeled(20, 200, GetButtonID(3, 202), "Shutdown (Without Save)");

                                AddButtonLabeled(20, 230, GetButtonID(3, 203), "Shutdown & Restart (With Save)");
                                AddButtonLabeled(20, 250, GetButtonID(3, 204), "Shutdown & Restart (Without Save)");
                            }
                            else
                            {
                                AddLabel(20, 215, LabelHue, "Shutdown/Restart not available.");
                            }

                            AddHtml(10, 295, 400, 20, Color(Center("Broadcast"), LabelColor32), false, false);

                            AddTextField(20, 320, 380, 20, 0);
                            AddButtonLabeled(20, 350, GetButtonID(3, 210), "To Everyone");
                            AddButtonLabeled(220, 350, GetButtonID(3, 211), "To Staff");

                            goto case AdminGumpPage.Administer;
                        }
                    case AdminGumpPage.Administer_Access_Lockdown:
                        {

                            AddHtml(10, 125, 400, 20, Color(Center("Server Lockdown"), LabelColor32), false, false);

                            AddHtml(20, 150, 380, 80, Color("When enabled, only clients with an access level equal to or greater than the specified lockdown level may access the server. After setting a lockdown level, use the <em>Purge Invalid Clients</em> button to disconnect those clients without access.", LabelColor32), false, false);

                            AccessLevel level = Misc.AccountHandler.LockdownLevel;
                            bool isLockedDown = (level > AccessLevel.Player);

                            AddSelectedButton(20, 230, GetButtonID(3, 500), "Not Locked Down", !isLockedDown);
                            AddSelectedButton(20, 260, GetButtonID(3, 507), "Owners", (isLockedDown && level <= AccessLevel.Owner));
                            AddSelectedButton(20, 280, GetButtonID(3, 506), "Administrators", (isLockedDown && level <= AccessLevel.Administrator));
                            AddSelectedButton(20, 300, GetButtonID(3, 505), "Seers", (isLockedDown && level <= AccessLevel.Seer));
                            AddSelectedButton(20, 320, GetButtonID(3, 504), "Game Masters", (isLockedDown && level <= AccessLevel.GameMaster));
                            AddSelectedButton(220, 260, GetButtonID(3, 503), "Counselors", (isLockedDown && level <= AccessLevel.Counselor));
                            AddSelectedButton(220, 280, GetButtonID(3, 502), "Fight Brokers", (isLockedDown && level <= AccessLevel.FightBroker));
                            AddSelectedButton(220, 300, GetButtonID(3, 501), "Reporters", (isLockedDown && level <= AccessLevel.Reporter));

                            AddButtonLabeled(20, 350, GetButtonID(3, 510), "Purge Invalid Clients");

                            goto case AdminGumpPage.Administer;
                        }
                    case AdminGumpPage.Administer_Access:
                        {

                            AddHtml(10, 125, 400, 20, Color(Center("Access"), LabelColor32), false, false);

                            AddHtml(10, 155, 400, 20, Color(Center("Connectivity"), LabelColor32), false, false);

                            AddButtonLabeled(20, 180, GetButtonID(3, 300), "Kick");
                            AddButtonLabeled(220, 180, GetButtonID(3, 301), "Ban");

                            AddButtonLabeled(20, 210, GetButtonID(3, 302), "Firewall");
                            AddButtonLabeled(220, 210, GetButtonID(3, 303), "Lockdown");

                            AddHtml(10, 245, 400, 20, Color(Center("Staff"), LabelColor32), false, false);

                            AddButtonLabeled(20, 270, GetButtonID(3, 310), "Make Player");
                            AddButtonLabeled(20, 290, GetButtonID(3, 311), "Make Reporter");
                            AddButtonLabeled(20, 310, GetButtonID(3, 312), "Make FightBroker");
                            AddButtonLabeled(20, 330, GetButtonID(3, 313), "Make Counselor");
                            AddButtonLabeled(220, 270, GetButtonID(3, 314), "Make Game Master");
                            AddButtonLabeled(220, 290, GetButtonID(3, 315), "Make Seer");
                            AddButtonLabeled(220, 310, GetButtonID(3, 316), "Make Administrator");

                            goto case AdminGumpPage.Administer;
                        }
                    case AdminGumpPage.Administer_Commands:
                        {

                            AddHtml(10, 125, 400, 20, Color(Center("Commands"), LabelColor32), false, false);

                            AddButtonLabeled(20, 150, GetButtonID(3, 400), "Add");
                            AddButtonLabeled(220, 150, GetButtonID(3, 401), "Remove");

                            AddButtonLabeled(20, 170, GetButtonID(3, 402), "Dupe");
                            AddButtonLabeled(220, 170, GetButtonID(3, 403), "Dupe in bag");

                            AddButtonLabeled(20, 200, GetButtonID(3, 404), "Properties");
                            AddButtonLabeled(220, 200, GetButtonID(3, 405), "Skills");

                            AddButtonLabeled(20, 230, GetButtonID(3, 406), "Mortal");
                            AddButtonLabeled(220, 230, GetButtonID(3, 407), "Immortal");

                            AddButtonLabeled(20, 250, GetButtonID(3, 408), "Squelch");
                            AddButtonLabeled(220, 250, GetButtonID(3, 409), "Unsquelch");

                            AddButtonLabeled(20, 270, GetButtonID(3, 410), "Freeze");
                            AddButtonLabeled(220, 270, GetButtonID(3, 411), "Unfreeze");

                            AddButtonLabeled(20, 290, GetButtonID(3, 412), "Hide");
                            AddButtonLabeled(220, 290, GetButtonID(3, 413), "Unhide");

                            AddButtonLabeled(20, 310, GetButtonID(3, 414), "Kill");
                            AddButtonLabeled(220, 310, GetButtonID(3, 415), "Resurrect");

                            AddButtonLabeled(20, 330, GetButtonID(3, 416), "Move");
                            AddButtonLabeled(220, 330, GetButtonID(3, 417), "Wipe");

                            AddButtonLabeled(20, 350, GetButtonID(3, 418), "Teleport");
                            AddButtonLabeled(220, 350, GetButtonID(3, 419), "Teleport (Multiple)");

                            goto case AdminGumpPage.Administer;
                        }
                    case AdminGumpPage.Administer:
                        {

                            AddPageButton(200, 20, GetButtonID(3, 0), "World Building", AdminGumpPage.Administer_WorldBuilding);
                            AddPageButton(200, 40, GetButtonID(3, 1), "Server", AdminGumpPage.Administer_Server);
                            AddPageButton(200, 60, GetButtonID(3, 2), "Access", AdminGumpPage.Administer_Access, AdminGumpPage.Administer_Access_Lockdown);
                            AddPageButton(200, 80, GetButtonID(3, 3), "Commands", AdminGumpPage.Administer_Commands);

                            break;
                        }
                    case AdminGumpPage.Clients:
                        {

                            if (m_List == null)
                            {
                                m_List = new ArrayList(NetState.Instances);
                                m_List.Sort(NetStateComparer.Instance);
                            }

                            AddClientHeader();

                            AddLabelCropped(12, 120, 81, 20, LabelHue, "Name");
                            AddLabelCropped(95, 120, 81, 20, LabelHue, "Account");
                            AddLabelCropped(178, 120, 81, 20, LabelHue, "Access Level");
                            AddLabelCropped(273, 120, 109, 20, LabelHue, "IP Address");

                            if (listPage > 0)
                                AddButton(375, 122, 0x15E3, 0x15E7, GetButtonID(1, 0), GumpButtonType.Reply, 0);
                            else
                                AddImage(375, 122, 0x25EA);

                            if ((listPage + 1) * 12 < m_List.Count)
                                AddButton(392, 122, 0x15E1, 0x15E5, GetButtonID(1, 1), GumpButtonType.Reply, 0);
                            else
                                AddImage(392, 122, 0x25E6);

                            if (m_List.Count == 0)
                                AddLabel(12, 140, LabelHue, "There are no clients to display.");

                            for (int i = 0, index = (listPage * 12); i < 12 && index >= 0 && index < m_List.Count; ++i, ++index)
                            {
                                NetState ns = m_List[index] as NetState;

                                if (ns == null)
                                    continue;

                                Mobile m = ns.Mobile;

                                if (m != null && m.Hidden && m.AccessLevel == AccessLevel.Ignore)
                                    continue;

                                Account a = ns.Account as Account;
                                int offset = 140 + (i * 20);

                                if (m == null)
                                {
                                    if (Admin.AdminNetwork.IsAuth(ns))
                                        AddLabelCropped(12, offset, 81, 20, LabelHue, "(remote admin)");
                                    else
                                        AddLabelCropped(12, offset, 81, 20, LabelHue, "(logging in)");
                                }
                                else
                                {

                                    AddLabelCropped(12, offset, 81, 20, m.GetHueForNameInList(), m.Name);
                                }
                                AddLabelCropped(95, offset, 81, 20, LabelHue, a == null ? "(no account)" : a.Username);
                                AddLabelCropped(178, offset, 81, 20, LabelHue, m == null ? (a != null ? FormatAccessLevel(a.AccessLevel) : "") : FormatAccessLevel(m.AccessLevel));
                                AddLabelCropped(273, offset, 109, 20, LabelHue, ns.ToString());

                                if (a != null || m != null)
                                    AddButton(380, offset - 1, 0xFA5, 0xFA7, GetButtonID(4, index + 2), GumpButtonType.Reply, 0);
                            }

                            break;
                        }
                    case AdminGumpPage.ClientInfo:
                        {

                            Mobile m = state as Mobile;

                            if (m == null)
                                break;

                            AddClientHeader();

                            AddHtml(10, 125, 400, 20, Color(Center("Information"), LabelColor32), false, false);

                            int y = 146;

                            AddLabel(20, y, LabelHue, "Name:");
                            AddLabel(200, y, m.GetHueForNameInList(), m.Name);
                            y += 20;

                            Account a = m.Account as Account;

                            AddLabel(20, y, LabelHue, "Account:");
                            AddLabel(200, y, (a != null && a.Banned) ? RedHue : LabelHue, a == null ? "(no account)" : a.Username);
                            AddButton(380, y, 0xFA5, 0xFA7, GetButtonID(7, 15), GumpButtonType.Reply, 0);
                            y += 20;

                            NetState ns = m.NetState;

                            if (ns == null)
                            {
                                AddLabel(20, y, LabelHue, "Address:");
                                AddLabel(200, y, RedHue, "Offline");
                                y += 20;

                                AddLabel(20, y, LabelHue, "Location:");
                                AddLabel(200, y, LabelHue, String.Format("{0} [{1}]", m.Location, m.Map));
                                y += 44;
                            }
                            else
                            {

                                AddLabel(20, y, LabelHue, "Address:");
                                AddLabel(200, y, GreenHue, ns.ToString());
                                y += 20;

                                ClientVersion v = ns.Version;

                                AddLabel(20, y, LabelHue, "Version:");
                                AddLabel(200, y, LabelHue, v == null ? "(null)" : v.ToString());
                                y += 20;

                                AddLabel(20, y, LabelHue, "Location:");
                                AddLabel(200, y, LabelHue, String.Format("{0} [{1}]", m.Location, m.Map));
                                y += 24;
                            }

                            AddButtonLabeled(20, y, GetButtonID(7, 0), "Go to");
                            AddButtonLabeled(150, y, GetButtonID(7, 1), "Get");
                            AddButtonLabeled(280, y, GetButtonID(7, 14), "View Houses");
                            y += 20;

                            AddButtonLabeled(20, y, GetButtonID(7, 2), "Kick");
                            AddButtonLabeled(150, y, GetButtonID(7, 3), "Ban");
                            y += 20;

                            AddButtonLabeled(20, y, GetButtonID(7, 4), "Properties");
                            AddButtonLabeled(150, y, GetButtonID(7, 5), "Skills");
                            y += 20;

                            AddButtonLabeled(20, y, GetButtonID(7, 6), "Mortal");
                            AddButtonLabeled(150, y, GetButtonID(7, 7), "Immortal");
                            y += 20;

                            AddButtonLabeled(20, y, GetButtonID(7, 8), "Squelch");
                            AddButtonLabeled(150, y, GetButtonID(7, 9), "Unsquelch");
                            y += 20;

                            /*AddButtonLabeled(  20, y, GetButtonID( 7, 10 ), "Hide" );
							AddButtonLabeled( 200, y, GetButtonID( 7, 11 ), "Unhide" );
							y += 20;*/

                            AddButtonLabeled(20, y, GetButtonID(7, 12), "Kill");
                            AddButtonLabeled(150, y, GetButtonID(7, 13), "Resurrect");
                            y += 20;

                            break;
                        }
                    case AdminGumpPage.Accounts_Shared:
                        {

                            if (m_List == null)
                                m_List = GetAllSharedAccounts();

                            AddLabelCropped(12, 120, 60, 20, LabelHue, "Count");
                            AddLabelCropped(72, 120, 120, 20, LabelHue, "Address");
                            AddLabelCropped(192, 120, 180, 20, LabelHue, "Accounts");

                            if (listPage > 0)
                                AddButton(375, 122, 0x15E3, 0x15E7, GetButtonID(1, 0), GumpButtonType.Reply, 0);
                            else
                                AddImage(375, 122, 0x25EA);

                            if ((listPage + 1) * 12 < m_List.Count)
                                AddButton(392, 122, 0x15E1, 0x15E5, GetButtonID(1, 1), GumpButtonType.Reply, 0);
                            else
                                AddImage(392, 122, 0x25E6);

                            if (m_List.Count == 0)
                                AddLabel(12, 140, LabelHue, "There are no accounts to display.");

                            StringBuilder sb = new StringBuilder();

                            for (int i = 0, index = (listPage * 12); i < 12 && index >= 0 && index < m_List.Count; ++i, ++index)
                            {

                                DictionaryEntry de = (DictionaryEntry)m_List[index];

                                IPAddress ipAddr = (IPAddress)de.Key;
                                ArrayList accts = (ArrayList)de.Value;

                                int offset = 140 + (i * 20);

                                AddLabelCropped(12, offset, 60, 20, LabelHue, accts.Count.ToString());
                                AddLabelCropped(72, offset, 120, 20, LabelHue, ipAddr.ToString());

                                if (sb.Length > 0)
                                    sb.Length = 0;

                                for (int j = 0; j < accts.Count; ++j)
                                {
                                    if (j > 0)
                                        sb.Append(", ");

                                    if (j < 4)
                                    {
                                        Account acct = (Account)accts[j];

                                        sb.Append(acct.Username);
                                    }
                                    else
                                    {
                                        sb.Append("...");
                                        break;
                                    }
                                }

                                AddLabelCropped(192, offset, 180, 20, LabelHue, sb.ToString());

                                AddButton(380, offset - 1, 0xFA5, 0xFA7, GetButtonID(5, index + 55), GumpButtonType.Reply, 0);
                            }

                            break;
                        }
                    case AdminGumpPage.Accounts:
                        {

                            if (m_List == null)
                            {
                                m_List = new ArrayList(Accounts.Table.Values);
                                m_List.Sort(AccountComparer.Instance);
                            }

                            ArrayList rads = (state as ArrayList);

                            AddAccountHeader();

                            if (rads == null)
                                AddLabelCropped(12, 120, 120, 20, LabelHue, "Name");
                            else
                                AddLabelCropped(32, 120, 100, 20, LabelHue, "Name");

                            AddLabelCropped(132, 120, 120, 20, LabelHue, "Access Level");
                            AddLabelCropped(252, 120, 120, 20, LabelHue, "Status");

                            if (listPage > 0)
                                AddButton(375, 122, 0x15E3, 0x15E7, GetButtonID(1, 0), GumpButtonType.Reply, 0);
                            else
                                AddImage(375, 122, 0x25EA);

                            if ((listPage + 1) * 12 < m_List.Count)
                                AddButton(392, 122, 0x15E1, 0x15E5, GetButtonID(1, 1), GumpButtonType.Reply, 0);
                            else
                                AddImage(392, 122, 0x25E6);

                            if (m_List.Count == 0)
                                AddLabel(12, 140, LabelHue, "There are no accounts to display.");

                            if (rads != null && notice == null)
                            {
                                AddButtonLabeled(10, 390, GetButtonID(5, 29), "Ban marked");
                                AddButtonLabeled(10, 410, GetButtonID(5, 30), "Delete marked");

                                AddButtonLabeled(210, 400, GetButtonID(5, 31), "Mark all");
                            }

                            for (int i = 0, index = (listPage * 12); i < 12 && index >= 0 && index < m_List.Count; ++i, ++index)
                            {

                                Account a = m_List[index] as Account;

                                if (a == null)
                                    continue;

                                int offset = 140 + (i * 20);

                                AccessLevel accessLevel;
                                bool online;

                                a.GetAccountInfo(out accessLevel, out online);

                                if (rads == null)
                                {
                                    AddLabelCropped(12, offset, 120, 20, LabelHue, "\"" + a.Username + "\"");
                                }
                                else
                                {
                                    AddCheck(10, offset, 0xD2, 0xD3, rads.Contains(a), index);
                                    AddLabelCropped(32, offset, 100, 20, LabelHue, "\"" + a.Username + "\"");
                                }

                                AddLabelCropped(132, offset, 120, 20, LabelHue, FormatAccessLevel(accessLevel));

                                if (online)
                                    AddLabelCropped(252, offset, 120, 20, GreenHue, "Online");
                                else if (a.Banned)
                                    AddLabelCropped(252, offset, 120, 20, RedHue, "Banned");
                                else
                                    AddLabelCropped(252, offset, 120, 20, RedHue, "Offline");

                                AddButton(380, offset - 1, 0xFA5, 0xFA7, GetButtonID(5, index + 55), GumpButtonType.Reply, 0);
                            }

                            break;
                        }
                    case AdminGumpPage.AccountDetails:
                        {

                            AddPageButton(190, 10, GetButtonID(5, 0), "Information", AdminGumpPage.AccountDetails_Information, AdminGumpPage.AccountDetails_ChangeAccess, AdminGumpPage.AccountDetails_ChangePassword);
                            AddPageButton(190, 30, GetButtonID(5, 1), "Characters", AdminGumpPage.AccountDetails_Characters);
                            AddPageButton(190, 50, GetButtonID(5, 13), "Access", AdminGumpPage.AccountDetails_Access, AdminGumpPage.AccountDetails_Access_ClientIPs, AdminGumpPage.AccountDetails_Access_Restrictions);
                            AddPageButton(190, 70, GetButtonID(5, 2), "Comments", AdminGumpPage.AccountDetails_Comments);
                            AddPageButton(190, 90, GetButtonID(5, 3), "Tags", AdminGumpPage.AccountDetails_Tags);
                            AddPageButton(290, 10, GetButtonID(5, 36), "Activation", AdminGumpPage.AccountDetails_Activation);
                            AddPageButton(290, 30, GetButtonID(5, 37), "Email History", AdminGumpPage.AccountDetails_EmailHistory);
                            break;
                        }
                    case AdminGumpPage.AccountDetails_Activation:
                        {

                            Account a = state as Account;

                            if (a == null)
                                break;

                            AddHtml(10, 125, 400, 20, Color(Center("Account Activation"), LabelColor32), false, false);

                            AddLabel(20, 150, LabelHue, "Username:");
                            AddLabel(200, 150, LabelHue, "\"" + a.Username + "\"");

                            AddLabel(20, 170, LabelHue, "Email:");
                            AddLabel(200, 170, LabelHue, a.EmailAddress);

                            AddLabel(20, 190, LabelHue, "Activated:");
                            AddLabel(200, 190, LabelHue, a.AccountActivated ? "YES" : "NO");

                            AddLabel(20, 210, LabelHue, "Activation Key:");
                            AddLabel(200, 210, LabelHue, a.ActivationKey);

                            AddLabel(20, 230, LabelHue, "Notification Emails:");
                            AddLabel(200, 230, LabelHue, a.DoNotSendEmail ? "OFF" : "ON");

                            //Adam: 7/8/08 - add null checks .. this was crashing
                            if (a.EmailAddress != null && a.EmailAddress.Length > 5 &&
                                a.ActivationKey != null && a.ActivationKey.Length > 6 &&
                                a.AccountActivated == false)
                            {
                                AddButtonLabeled(20, 280, GetButtonID(5, 38), "Reset Activation");
                                AddButtonLabeled(20, 320, GetButtonID(5, 39), "Resend Activation");
                            }

                            if (a.DoNotSendEmail) //Toggle Notification Emails
                            {
                                AddButtonLabeled(20, 340, GetButtonID(5, 41), "Turn Notification Emails ON");
                            }
                            else
                            {
                                AddButtonLabeled(20, 340, GetButtonID(5, 41), "Turn Notification Emails OFF");
                            }

                            goto case AdminGumpPage.AccountDetails;
                        }
                    case AdminGumpPage.AccountDetails_EmailHistory:
                        {
                            Account a = state as Account;

                            if (a == null)
                                break;

                            AddHtml(10, 125, 400, 20, Color(Center("Email History"), LabelColor32), false, false);

                            AddLabel(20, 150, LabelHue, "Username:");
                            AddLabel(200, 150, LabelHue, a.Username);

                            StringBuilder sb = new StringBuilder();

                            if (a.EmailHistory.Length == 0)
                                sb.Append("There is no email history for this account.");

                            for (int i = 0; i < a.EmailHistory.Length; ++i)
                            {
                                if (i > 0)
                                    sb.Append("<BR><BR>");

                                String email = a.EmailHistory[i];

                                sb.AppendFormat("[{0}]", email);
                            }

                            AddHtml(20, 180, 380, 190, sb.ToString(), true, true);

                            goto case AdminGumpPage.AccountDetails;
                        }
                    case AdminGumpPage.AccountDetails_ChangePassword:
                        {
                            Account a = state as Account;

                            if (a == null)
                                break;

                            AddHtml(10, 125, 400, 20, Color(Center("Change Password"), LabelColor32), false, false);

                            AddLabel(20, 150, LabelHue, "Username:");
                            AddLabel(200, 150, LabelHue, a.Username);

                            AddLabel(20, 180, LabelHue, "Password:");
                            AddTextField(200, 180, 160, 20, 0);

                            AddLabel(20, 210, LabelHue, "Confirm:");
                            AddTextField(200, 210, 160, 20, 1);

                            AddButtonLabeled(20, 240, GetButtonID(5, 12), "Submit Change");

                            goto case AdminGumpPage.AccountDetails;
                        }
                    case AdminGumpPage.AccountDetails_ChangeAccess:
                        {
                            Account a = state as Account;

                            if (a == null)
                                break;

                            AddHtml(10, 125, 400, 20, Color(Center("Change Access Level"), LabelColor32), false, false);

                            AddLabel(20, 150, LabelHue, "Username:");
                            AddLabel(200, 150, LabelHue, a.Username);

                            AddLabel(20, 170, LabelHue, "Current Level:");
                            AddLabel(200, 170, LabelHue, FormatAccessLevel(a.AccessLevel));

                            AddButtonLabeled(20, 200, GetButtonID(5, 20), "Player");
                            AddButtonLabeled(20, 220, GetButtonID(5, 21), "Reporter");
                            AddButtonLabeled(20, 240, GetButtonID(5, 22), "Fight Broker");
                            AddButtonLabeled(20, 260, GetButtonID(5, 23), "Counselor");
                            AddButtonLabeled(20, 280, GetButtonID(5, 24), "Game Master");
                            AddButtonLabeled(20, 300, GetButtonID(5, 25), "Seer");
                            AddButtonLabeled(20, 320, GetButtonID(5, 26), "Administrator");

                            goto case AdminGumpPage.AccountDetails;
                        }
                    case AdminGumpPage.AccountDetails_Information:
                        {
                            Account a = state as Account;

                            if (a == null)
                                break;

                            int charCount = 0;

                            for (int i = 0; i < 5; ++i)
                            {
                                if (a[i] != null)
                                    ++charCount;
                            }

                            AddHtml(10, 125, 400, 20, Color(Center("Information"), LabelColor32), false, false);

                            AddLabel(20, 150, LabelHue, "Username:");
                            AddLabel(200, 150, LabelHue, a.Username);

                            AddLabel(20, 170, LabelHue, "Access Level:");
                            AddLabel(200, 170, LabelHue, FormatAccessLevel(a.AccessLevel));

                            AddLabel(20, 190, LabelHue, "Status:");
                            AddLabel(200, 190, a.Banned ? RedHue : GreenHue, a.Banned ? "Banned" : "Active");

                            DateTime banTime;
                            TimeSpan banDuration;

                            if (a.Banned && a.GetBanTags(out banTime, out banDuration))
                            {
                                if (banDuration == TimeSpan.MaxValue)
                                {
                                    AddLabel(250, 190, LabelHue, "(Infinite)");
                                }
                                else if (banDuration == TimeSpan.Zero)
                                {
                                    AddLabel(250, 190, LabelHue, "(Zero)");
                                }
                                else
                                {
                                    TimeSpan remaining = (DateTime.Now - banTime);

                                    if (remaining < TimeSpan.Zero)
                                        remaining = TimeSpan.Zero;
                                    else if (remaining > banDuration)
                                        remaining = banDuration;

                                    double remMinutes = remaining.TotalMinutes;
                                    double totMinutes = banDuration.TotalMinutes;

                                    double perc = remMinutes / totMinutes;

                                    AddLabel(250, 190, LabelHue, String.Format("{0} [{1:F0}%]", FormatTimeSpan(banDuration), perc * 100));
                                }
                            }
                            else if (a.Banned)
                            {
                                AddLabel(250, 190, LabelHue, "(Unspecified)");
                            }

                            AddLabel(20, 210, LabelHue, "Created:");
                            AddLabel(200, 210, LabelHue, a.Created.ToString());

                            AddLabel(20, 230, LabelHue, "Last Login:");
                            AddLabel(200, 230, LabelHue, a.LastLogin.ToString());

                            AddLabel(20, 250, LabelHue, "Character Count:");
                            AddLabel(200, 250, LabelHue, charCount.ToString());

                            AddLabel(20, 270, LabelHue, "Comment Count:");
                            AddLabel(200, 270, LabelHue, a.Comments.Count.ToString());

                            AddLabel(20, 290, LabelHue, "Tag Count:");
                            AddLabel(200, 290, LabelHue, a.Tags.Count.ToString());

                            AddButtonLabeled(20, 320, GetButtonID(5, 8), "Change Password");
                            AddButtonLabeled(200, 320, GetButtonID(5, 9), "Change Access Level");

                            if (!a.Banned)
                                AddButtonLabeled(20, 350, GetButtonID(5, 10), "Ban Account");
                            else
                                AddButtonLabeled(20, 350, GetButtonID(5, 11), "Unban Account");

                            AddButtonLabeled(200, 350, GetButtonID(5, 27), "Delete Account");

                            goto case AdminGumpPage.AccountDetails;
                        }
                    case AdminGumpPage.AccountDetails_Access:
                        {
                            Account a = state as Account;

                            if (a == null)
                                break;

                            AddHtml(10, 125, 400, 20, Color(Center("Access"), LabelColor32), false, false);

                            AddPageButton(20, 150, GetButtonID(5, 14), "View client addresses", AdminGumpPage.AccountDetails_Access_ClientIPs);
                            AddPageButton(20, 170, GetButtonID(5, 15), "Manage restrictions", AdminGumpPage.AccountDetails_Access_Restrictions);

                            goto case AdminGumpPage.AccountDetails;
                        }
                    case AdminGumpPage.AccountDetails_Access_ClientIPs:
                        {
                            Account a = state as Account;

                            if (a == null)
                                break;

                            if (m_List == null)
                                m_List = new ArrayList(a.LoginIPs);

                            AddHtml(10, 195, 400, 20, Color(Center("Client Addresses"), LabelColor32), false, false);

                            AddButtonLabeled(200, 225, GetButtonID(5, 16), "View all shared accounts");
                            AddButtonLabeled(200, 245, GetButtonID(5, 17), "Ban all shared accounts");
                            AddButtonLabeled(200, 265, GetButtonID(5, 18), "Firewall all addresses");

                            AddHtml(195, 295, 210, 80, Color("List of IP addresses which have accessed this account.", LabelColor32), false, false);

                            AddImageTiled(15, 219, 176, 156, 0xBBC);
                            AddBlackAlpha(16, 220, 174, 154);

                            AddHtml(18, 221, 114, 20, Color("IP Address", LabelColor32), false, false);

                            if (listPage > 0)
                                AddButton(154, 223, 0x15E3, 0x15E7, GetButtonID(1, 0), GumpButtonType.Reply, 0);
                            else
                                AddImage(154, 223, 0x25EA);

                            if ((listPage + 1) * 6 < m_List.Count)
                                AddButton(171, 223, 0x15E1, 0x15E5, GetButtonID(1, 1), GumpButtonType.Reply, 0);
                            else
                                AddImage(171, 223, 0x25E6);

                            if (m_List.Count == 0)
                                AddHtml(18, 243, 170, 60, Color("This account has not yet been accessed.", LabelColor32), false, false);

                            for (int i = 0, index = (listPage * 6); i < 6 && index >= 0 && index < m_List.Count; ++i, ++index)
                            {
                                AddHtml(18, 243 + (i * 22), 114, 20, Color(m_List[index].ToString(), LabelColor32), false, false);
                                AddButton(130, 242 + (i * 22), 0xFA2, 0xFA4, GetButtonID(8, index), GumpButtonType.Reply, 0);
                                AddButton(160, 242 + (i * 22), 0xFA8, 0xFAA, GetButtonID(9, index), GumpButtonType.Reply, 0);
                            }

                            goto case AdminGumpPage.AccountDetails_Access;
                        }
                    case AdminGumpPage.AccountDetails_Access_Restrictions:
                        {
                            Account a = state as Account;

                            if (a == null)
                                break;

                            if (m_List == null)
                                m_List = new ArrayList(a.IPRestrictions);

                            AddHtml(10, 195, 400, 20, Color(Center("Address Restrictions"), LabelColor32), false, false);

                            AddTextField(200, 225, 120, 20, 0);

                            AddButtonLabeled(330, 225, GetButtonID(5, 19), "Add");

                            AddHtml(195, 255, 210, 120, Color("Any clients connecting from an address not in this list will be rejected. Or, if the list is empty, any client may connect.", LabelColor32), false, false);

                            AddImageTiled(15, 219, 176, 156, 0xBBC);
                            AddBlackAlpha(16, 220, 174, 154);

                            AddHtml(18, 221, 114, 20, Color("IP Address", LabelColor32), false, false);

                            if (listPage > 0)
                                AddButton(154, 223, 0x15E3, 0x15E7, GetButtonID(1, 0), GumpButtonType.Reply, 0);
                            else
                                AddImage(154, 223, 0x25EA);

                            if ((listPage + 1) * 6 < m_List.Count)
                                AddButton(171, 223, 0x15E1, 0x15E5, GetButtonID(1, 1), GumpButtonType.Reply, 0);
                            else
                                AddImage(171, 223, 0x25E6);

                            if (m_List.Count == 0)
                                AddHtml(18, 243, 170, 60, Color("There are no addresses in this list.", LabelColor32), false, false);

                            for (int i = 0, index = (listPage * 6); i < 6 && index >= 0 && index < m_List.Count; ++i, ++index)
                            {
                                AddHtml(18, 243 + (i * 22), 114, 20, Color(m_List[index].ToString(), LabelColor32), false, false);
                                AddButton(160, 242 + (i * 22), 0xFB1, 0xFB3, GetButtonID(8, index), GumpButtonType.Reply, 0);
                            }

                            goto case AdminGumpPage.AccountDetails_Access;
                        }
                    case AdminGumpPage.AccountDetails_Characters:
                        {
                            Account a = state as Account;

                            if (a == null)
                                break;

                            AddHtml(10, 125, 400, 20, Color(Center("Characters"), LabelColor32), false, false);

                            AddLabelCropped(12, 150, 120, 20, LabelHue, "Name");
                            AddLabelCropped(132, 150, 120, 20, LabelHue, "Access Level");
                            AddLabelCropped(252, 150, 120, 20, LabelHue, "Status");

                            int index = 0;

                            for (int i = 0; i < 5; ++i)
                            {
                                Mobile m = a[i];

                                if (m == null)
                                    continue;

                                int offset = 170 + (index * 20);

                                AddLabelCropped(12, offset, 120, 20, m.GetHueForNameInList(), m.Name);
                                AddLabelCropped(132, offset, 120, 20, LabelHue, FormatAccessLevel(m.AccessLevel));

                                if (m.NetState != null)
                                    AddLabelCropped(252, offset, 120, 20, GreenHue, "Online");
                                else
                                    AddLabelCropped(252, offset, 120, 20, RedHue, "Offline");

                                AddButton(380, offset - 1, 0xFA5, 0xFA7, GetButtonID(5, i + 50), GumpButtonType.Reply, 0);

                                ++index;
                            }

                            if (index == 0)
                                AddLabel(12, 170, LabelHue, "The character list is empty.");

                            goto case AdminGumpPage.AccountDetails;
                        }
                    case AdminGumpPage.AccountDetails_Comments:
                        {
                            Account a = state as Account;

                            if (a == null)
                                break;

                            AddHtml(10, 125, 400, 20, Color(Center("Comments"), LabelColor32), false, false);

                            AddButtonLabeled(20, 150, GetButtonID(5, 4), "Add Comment");

                            StringBuilder sb = new StringBuilder();

                            if (a.Comments.Count == 0)
                                sb.Append("There are no comments for this account.");

                            for (int i = 0; i < a.Comments.Count; ++i)
                            {
                                if (i > 0)
                                    sb.Append("<BR><BR>");

                                AccountComment c = (AccountComment)a.Comments[i];

                                sb.AppendFormat("[{0}]<BR>{1}", c.AddedBy, c.Content);
                            }

                            AddHtml(20, 180, 380, 190, sb.ToString(), true, true);

                            goto case AdminGumpPage.AccountDetails;
                        }
                    case AdminGumpPage.AccountDetails_Tags:
                        {
                            Account a = state as Account;

                            if (a == null)
                                break;

                            AddHtml(10, 125, 400, 20, Color(Center("Tags"), LabelColor32), false, false);

                            AddButtonLabeled(20, 150, GetButtonID(5, 5), "Add Tag");
                            AddButtonLabeled(150, 150, GetButtonID(5, 35), "Remove Tag");

                            StringBuilder sb = new StringBuilder();

                            if (a.Tags.Count == 0)
                                sb.Append("There are no tags for this account.");

                            for (int i = 0; i < a.Tags.Count; ++i)
                            {
                                if (i > 0)
                                    sb.Append("<BR>");

                                AccountTag tag = (AccountTag)a.Tags[i];

                                sb.AppendFormat("{0} = {1}", tag.Name, tag.Value);
                            }

                            AddHtml(20, 180, 380, 190, sb.ToString(), true, true);

                            goto case AdminGumpPage.AccountDetails;
                        }
                    case AdminGumpPage.Firewall:
                        {
                            AddFirewallHeader();

                            if (m_List == null)
                                m_List = new ArrayList(Firewall.List);

                            AddLabelCropped(12, 120, 358, 20, LabelHue, "IP Address");

                            if (listPage > 0)
                                AddButton(375, 122, 0x15E3, 0x15E7, GetButtonID(1, 0), GumpButtonType.Reply, 0);
                            else
                                AddImage(375, 122, 0x25EA);

                            if ((listPage + 1) * 12 < m_List.Count)
                                AddButton(392, 122, 0x15E1, 0x15E5, GetButtonID(1, 1), GumpButtonType.Reply, 0);
                            else
                                AddImage(392, 122, 0x25E6);

                            if (m_List.Count == 0)
                                AddLabel(12, 140, LabelHue, "The firewall list is empty.");

                            for (int i = 0, index = (listPage * 12); i < 12 && index >= 0 && index < m_List.Count; ++i, ++index)
                            {
                                object obj = m_List[index];

                                if (!(obj is IPAddress) && !(obj is String))
                                    break;

                                int offset = 140 + (i * 20);

                                AddLabelCropped(12, offset, 358, 20, LabelHue, obj.ToString());
                                AddButton(380, offset - 1, 0xFA5, 0xFA7, GetButtonID(6, index + 4), GumpButtonType.Reply, 0);
                            }

                            break;
                        }
                    case AdminGumpPage.FirewallInfo:
                        {
                            AddFirewallHeader();

                            if (!(state is IPAddress) && !(state is String))
                                break;

                            AddHtml(10, 125, 400, 20, Color(Center(state.ToString()), LabelColor32), false, false);

                            AddButtonLabeled(20, 150, GetButtonID(6, 3), "Remove");

                            AddHtml(10, 175, 400, 20, Color(Center("Potentially Effected Accounts"), LabelColor32), false, false);

                            if (m_List == null)
                            {
                                m_List = new ArrayList();

                                string pattern = state as String;
                                IPAddress addr = (state is IPAddress ? (IPAddress)state : IPAddress.Any);

                                foreach (Account acct in Accounts.Table.Values)
                                {
                                    IPAddress[] loginList = acct.LoginIPs;

                                    bool contains = false;

                                    for (int i = 0; !contains && i < loginList.Length; ++i)
                                        contains = (pattern == null ? loginList[i].Equals(addr) : Utility.IPMatch(pattern, loginList[i]));

                                    if (contains)
                                        m_List.Add(acct);
                                }

                                m_List.Sort(AccountComparer.Instance);
                            }

                            if (listPage > 0)
                                AddButton(375, 177, 0x15E3, 0x15E7, GetButtonID(1, 0), GumpButtonType.Reply, 0);
                            else
                                AddImage(375, 177, 0x25EA);

                            if ((listPage + 1) * 12 < m_List.Count)
                                AddButton(392, 177, 0x15E1, 0x15E5, GetButtonID(1, 1), GumpButtonType.Reply, 0);
                            else
                                AddImage(392, 177, 0x25E6);

                            if (m_List.Count == 0)
                                AddLabelCropped(12, 200, 398, 20, LabelHue, "No accounts found.");

                            for (int i = 0, index = (listPage * 9); i < 9 && index >= 0 && index < m_List.Count; ++i, ++index)
                            {
                                Account a = m_List[index] as Account;

                                if (a == null)
                                    continue;

                                int offset = 200 + (i * 20);

                                AccessLevel accessLevel;
                                bool online;

                                a.GetAccountInfo(out accessLevel, out online);

                                AddLabelCropped(12, offset, 120, 20, LabelHue, a.Username);
                                AddLabelCropped(132, offset, 120, 20, LabelHue, FormatAccessLevel(accessLevel));

                                if (online)
                                    AddLabelCropped(252, offset, 120, 20, GreenHue, "Online");
                                else
                                    AddLabelCropped(252, offset, 120, 20, RedHue, "Offline");

                                AddButton(380, offset - 1, 0xFA5, 0xFA7, GetButtonID(5, index + 55), GumpButtonType.Reply, 0);
                            }

                            break;
                        }
                    case AdminGumpPage.IPException:
                        {
                            if (m_List == null)
                            {
                                m_List = new ArrayList(IPException.Table.Keys);
                                m_List.Sort();
                            }

                            AddIPExceptionHeader();

                            AddLabelCropped(12, 120, 181, 40, LabelHue, "IP Address");
                            AddLabelCropped(235, 120, 81, 40, LabelHue, "Allowed");
                            if (listPage > 0)
                                AddButton(375, 122, 0x15E3, 0x15E7, GetButtonID(1, 0), GumpButtonType.Reply, 0);
                            else
                                AddImage(375, 122, 0x25EA);

                            if ((listPage + 1) * 12 < m_List.Count)
                                AddButton(392, 122, 0x15E1, 0x15E5, GetButtonID(1, 1), GumpButtonType.Reply, 0);
                            else
                                AddImage(392, 122, 0x25E6);

                            if (m_List.Count == 0)
                                AddLabel(12, 140, LabelHue, "There are no ip exceptions to display.");

                            for (int i = 0, index = (listPage * 12); i < 12 && index >= 0 && index < m_List.Count; ++i, ++index)
                            {
                                string ipaddress = m_List[index] as String;

                                if (ipaddress == null)
                                    continue;

                                int count = 1;
                                try
                                {
                                    count = (int)IPException.Table[ipaddress];
                                }
                                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

                                int offset = 140 + (i * 20);

                                AddLabelCropped(12, offset, 181, 40, LabelHue, ipaddress);
                                AddLabelCropped(235, offset, 81, 40, LabelHue, count.ToString());

                                AddButton(380, offset - 1, 0xFA5, 0xFA7, GetButtonID(2, index + 3), GumpButtonType.Reply, 0);
                            }


                            break;
                        }
                    case AdminGumpPage.IPException_IPDetails:
                        {
                            string ipAddress = state as String;

                            AddIPExceptionHeader();

                            if (ipAddress == null)
                            {
                                break;
                            }

                            AddLabelCropped(12, 120, 181, 40, LabelHue, "Account");
                            AddLabelCropped(185, 120, 81, 40, LabelHue, "AccessLevel");
                            AddLabelCropped(285, 120, 50, 40, LabelHue, "Status");

                            int count = Server.Accounting.Accounts.Table.Values.Count;
                            int matchingLoginIPs = 0;
                            int i = 0;
                            m_List = new ArrayList();
                            foreach (Account acct in Accounts.Table.Values)
                            {
                                int offset = 150 + (i * 20);
                                if (acct != null)
                                {
                                    for (int j = 0; j < acct.LoginIPs.Length; j++)
                                    {
                                        if (acct.LoginIPs[j].ToString() == ipAddress)
                                        {
                                            m_List.Add(acct);
                                            AddLabelCropped(12, offset, 181, 40, LabelHue, acct.Username);
                                            AddLabelCropped(185, offset, 81, 40, LabelHue, acct.AccessLevel.ToString());
                                            AddLabelCropped(285, offset, 50, 40, acct.Banned ? RedHue : GreenHue, acct.Banned ? "Banned" : "Active");

                                            AddButton(380, offset - 1, 0xFA5, 0xFA7, GetButtonID(2, i + 3), GumpButtonType.Reply, 0);

                                            i++;
                                            matchingLoginIPs++;

                                            break;
                                        }
                                    }
                                }
                            }


                            break;
                        }
                }
            }
            // we are still seeing thie crash once in a while .. start collecting info;
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(new ApplicationException(String.Format("pageType:{0},", pageType), ex)));
            }
        }

        public void AddTextField(int x, int y, int width, int height, int index)
        {
            AddBackground(x - 2, y - 2, width + 4, height + 4, 0x2486);
            AddTextEntry(x + 2, y + 2, width - 4, height - 4, 0, index, "");
        }

        public void AddClientHeader()
        {
            AddTextField(200, 20, 200, 20, 0);
            AddButtonLabeled(200, 50, GetButtonID(4, 0), "Search For Name");
            AddButtonLabeled(200, 80, GetButtonID(4, 1), "Search For IP Address");
        }

        public void AddAccountHeader()
        {
            AddPage(1);

            AddLabel(200, 20, LabelHue, "Name:");
            AddTextField(250, 20, 150, 20, 0);

            AddLabel(200, 50, LabelHue, "Pass:");
            AddTextField(250, 50, 150, 20, 1);

            AddButtonLabeled(200, 80, GetButtonID(5, 6), "Add");

            AddButton(384, 84, 0x15E1, 0x15E5, 0, GumpButtonType.Page, 2);

            AddPage(2); //Name OR Email search

            AddLabel(200, 20, LabelHue, "Name:");
            AddTextField(250, 20, 150, 20, 2);

            AddLabel(200, 50, LabelHue, "Email:");
            AddTextField(250, 50, 150, 20, 3);
            AddButtonLabeled(290, 80, GetButtonID(5, 7), "Search");

            AddButton(384, 84, 0x15E1, 0x15E5, 0, GumpButtonType.Page, 3);

            AddPage(3);  //Search IP

            AddLabel(200, 20, LabelHue, "IP:");
            AddTextField(250, 20, 150, 20, 4);

            AddButtonLabeled(290, 80, GetButtonID(5, 40), "Search");

            AddButton(384, 84, 0x15E1, 0x15E5, 0, GumpButtonType.Page, 4);

            AddPage(4);

            AddButtonLabeled(200, 20, GetButtonID(5, 33), "View All: Inactive");
            AddButtonLabeled(200, 40, GetButtonID(5, 34), "View All: Banned");
            AddButtonLabeled(200, 60, GetButtonID(5, 28), "View All: Shared");
            AddButtonLabeled(200, 80, GetButtonID(5, 32), "View All: Staff");

            AddButton(384, 84, 0x15E1, 0x15E5, 0, GumpButtonType.Page, 1);

            AddPage(0);
        }

        public void AddFirewallHeader()
        {
            AddTextField(200, 20, 200, 20, 0);
            AddButtonLabeled(320, 50, GetButtonID(6, 0), "Search");
            AddButtonLabeled(200, 50, GetButtonID(6, 1), "Add (Input)");
            AddButtonLabeled(200, 80, GetButtonID(6, 2), "Add (Target)");
        }

        public void AddIPExceptionHeader()
        {
            AddLabel(200, 20, LabelHue, "IP:");
            AddTextField(250, 20, 150, 20, 0);

            AddLabel(200, 50, LabelHue, "Count:");
            AddTextField(250, 50, 150, 20, 1);

            AddButtonLabeled(200, 80, GetButtonID(2, 1), "Add");
            AddButtonLabeled(290, 80, GetButtonID(2, 2), "Remove");
        }

        private static ArrayList GetAllSharedAccounts()
        {
            Hashtable table = new Hashtable();
            ArrayList list;

            foreach (Account acct in Accounts.Table.Values)
            {
                IPAddress[] theirAddresses = acct.LoginIPs;

                for (int i = 0; i < theirAddresses.Length; ++i)
                {
                    list = (ArrayList)table[theirAddresses[i]];

                    if (list == null)
                        table[theirAddresses[i]] = list = new ArrayList();

                    list.Add(acct);
                }
            }

            list = new ArrayList(table);

            for (int i = 0; i < list.Count; ++i)
            {
                DictionaryEntry de = (DictionaryEntry)list[i];
                ArrayList accts = (ArrayList)de.Value;

                if (accts.Count == 1)
                    list.RemoveAt(i--);
                else
                    accts.Sort(AccountComparer.Instance);
            }

            list.Sort(SharedAccountComparer.Instance);

            return list;
        }

        private class SharedAccountComparer : IComparer
        {
            public static readonly IComparer Instance = new SharedAccountComparer();

            public SharedAccountComparer()
            {
            }

            public int Compare(object x, object y)
            {
                DictionaryEntry a = (DictionaryEntry)x;
                DictionaryEntry b = (DictionaryEntry)y;

                ArrayList aList = (ArrayList)a.Value;
                ArrayList bList = (ArrayList)b.Value;

                return bList.Count - aList.Count;
            }
        }


        private static ArrayList GetSharedAccounts(IPAddress ipAddress)
        {
            ArrayList list = new ArrayList();

            foreach (Account acct in Accounts.Table.Values)
            {
                IPAddress[] theirAddresses = acct.LoginIPs;
                bool contains = false;

                for (int i = 0; !contains && i < theirAddresses.Length; ++i)
                    contains = ipAddress.Equals(theirAddresses[i]);

                if (contains)
                    list.Add(acct);
            }

            list.Sort(AccountComparer.Instance);
            return list;
        }

        private static ArrayList GetSharedAccounts(IPAddress[] ipAddresses)
        {
            ArrayList list = new ArrayList();

            foreach (Account acct in Accounts.Table.Values)
            {
                IPAddress[] theirAddresses = acct.LoginIPs;
                bool contains = false;

                for (int i = 0; !contains && i < theirAddresses.Length; ++i)
                {
                    IPAddress check = theirAddresses[i];

                    for (int j = 0; !contains && j < ipAddresses.Length; ++j)
                        contains = check.Equals(ipAddresses[j]);
                }

                if (contains)
                    list.Add(acct);
            }

            list.Sort(AccountComparer.Instance);
            return list;
        }

        public static void BanShared_Callback(Mobile from, bool okay, object state)
        {
            if (from.AccessLevel < AccessLevel.Administrator)
                return;

            string notice;
            ArrayList list = null;

            if (okay)
            {
                Account a = (Account)state;
                list = GetSharedAccounts(a.LoginIPs);

                for (int i = 0; i < list.Count; ++i)
                {
                    ((Account)list[i]).SetUnspecifiedBan(from);
                    ((Account)list[i]).Banned = true;
                }

                notice = "All addresses in the list have been banned.";
            }
            else
            {
                notice = "You have chosen not to ban all shared accounts.";
            }

            from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Access_ClientIPs, 0, null, notice, state));

            if (okay)
                from.SendGump(new BanDurationGump(list));
        }

        public static void AccountDelete_Callback(Mobile from, bool okay, object state)
        {
            if (from.AccessLevel < AccessLevel.Administrator)
                return;

            if (okay)
            {
                Account a = (Account)state;

                a.Delete();

                from.SendGump(new AdminGump(from, AdminGumpPage.Accounts, 0, null, String.Format("{0} : The account has been deleted.", a.Username), null));
            }
            else
            {
                from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Information, 0, null, "You have chosen not to delete the account.", state));
            }
        }

        public static void ResendGump_Callback(Mobile from, object state)
        {
            if (from.AccessLevel < AccessLevel.Administrator)
                return;

            object[] states = (object[])state;
            ArrayList list = (ArrayList)states[0];
            ArrayList rads = (ArrayList)states[1];
            int page = (int)states[2];

            from.SendGump(new AdminGump(from, AdminGumpPage.Accounts, page, list, null, rads));
        }

        public static void Marked_Callback(Mobile from, bool okay, object state)
        {
            if (from.AccessLevel < AccessLevel.Administrator)
                return;

            object[] states = (object[])state;
            bool ban = (bool)states[0];
            ArrayList list = (ArrayList)states[1];
            ArrayList rads = (ArrayList)states[2];
            int page = (int)states[3];

            if (okay)
            {
                for (int i = 0; i < rads.Count; ++i)
                {
                    Account acct = (Account)rads[i];

                    if (ban)
                    {
                        acct.SetUnspecifiedBan(from);
                        acct.Banned = true;
                    }
                    else
                    {
                        acct.Delete();
                        rads.RemoveAt(i--);
                        list.Remove(acct);
                    }
                }

                from.SendGump(new NoticeGump(1060637, 30720, String.Format("You have {0} the account{1}.", ban ? "banned" : "deleted", rads.Count == 1 ? "" : "s"), 0xFFC000, 420, 280, new NoticeGumpCallback(ResendGump_Callback), new object[] { list, rads, ban ? page : 0 }));

                if (ban)
                    from.SendGump(new BanDurationGump(list));
            }
            else
            {
                from.SendGump(new NoticeGump(1060637, 30720, String.Format("You have chosen not to {0} the account{1}.", ban ? "ban" : "delete", rads.Count == 1 ? "" : "s"), 0xFFC000, 420, 280, new NoticeGumpCallback(ResendGump_Callback), new object[] { list, rads, page }));
            }
        }

        public static void FirewallShared_Callback(Mobile from, bool okay, object state)
        {
            if (from.AccessLevel < AccessLevel.Administrator)
                return;

            string notice;

            if (okay)
            {
                Account a = (Account)state;

                for (int i = 0; i < a.LoginIPs.Length; ++i)
                    Firewall.Add(a.LoginIPs[i]);

                notice = "All addresses in the list have been firewalled.";
            }
            else
            {
                notice = "You have chosen not to firewall all addresses.";
            }

            from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Access_ClientIPs, 0, null, notice, state));
        }

        public static void Firewall_Callback(Mobile from, bool okay, object state)
        {
            if (from.AccessLevel < AccessLevel.Administrator)
                return;

            object[] states = (object[])state;

            Account a = (Account)states[0];
            object toFirewall = states[1];

            string notice;

            if (okay)
            {
                Firewall.Add(toFirewall);

                notice = String.Format("{0} : Added to firewall.", toFirewall);
            }
            else
            {
                notice = "You have chosen not to firewall the address.";
            }

            from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Access_ClientIPs, 0, null, notice, a));
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            int val = info.ButtonID - 1;

            if (val < 0)
                return;

            Mobile from = m_From;

            if (from.AccessLevel < AccessLevel.Administrator)
                return;

            if (m_PageType == AdminGumpPage.Accounts)
            {
                ArrayList list = m_List;
                ArrayList rads = m_State as ArrayList;

                if (list != null && rads != null)
                {
                    for (int i = 0, v = m_ListPage * 12; i < 12 && v < list.Count; ++i, ++v)
                    {
                        object obj = list[v];

                        if (info.IsSwitched(v))
                        {
                            if (!rads.Contains(obj))
                                rads.Add(obj);
                        }
                        else if (rads.Contains(obj))
                        {
                            rads.Remove(obj);
                        }
                    }
                }
            }

            int type = val % 10;
            int index = val / 10;

            switch (type)
            {
                case 0:
                    {
                        AdminGumpPage page;

                        switch (index)
                        {
                            case 0: page = AdminGumpPage.Information; break;
                            case 1: page = AdminGumpPage.Administer; break;
                            case 2: page = AdminGumpPage.Clients; break;
                            case 3: page = AdminGumpPage.Accounts; break;
                            case 4: page = AdminGumpPage.Firewall; break;
                            case 5: page = AdminGumpPage.IPException; break;
                            default: return;
                        }

                        from.SendGump(new AdminGump(from, page, 0, null, null, null));
                        break;
                    }
                case 1:
                    {
                        switch (index)
                        {
                            case 0:
                                {
                                    if (m_List != null && m_ListPage > 0)
                                        from.SendGump(new AdminGump(from, m_PageType, m_ListPage - 1, m_List, null, m_State));

                                    break;
                                }
                            case 1:
                                {
                                    if (m_List != null /*&& (m_ListPage + 1) * 12 < m_List.Count*/ )
                                        from.SendGump(new AdminGump(from, m_PageType, m_ListPage + 1, m_List, null, m_State));

                                    break;
                                }
                        }

                        break;
                    }
                case 2: //IPException
                    {
                        switch (index)
                        {
                            case 1: //add
                                {
                                    try
                                    {
                                        TextRelay relay = info.GetTextEntry(0);
                                        TextRelay relay1 = info.GetTextEntry(1);

                                        string ip = (relay == null ? null : relay.Text.Trim());
                                        string strCount = (relay1 == null ? null : relay1.Text.Trim());

                                        if (ip == null || strCount == null)
                                        {
                                            from.SendMessage("IP or Count not entered");
                                        }
                                        else
                                        {
                                            int count = 1;
                                            try { count = Int32.Parse(strCount); }
                                            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                                            from.SendMessage("Adding {0} to IPException with a count of {1}", ip, count);
                                            Server.Accounting.IPException.AddException(ip, count);
                                        }
                                    }
                                    catch (Exception ipexc)
                                    {
                                        LogHelper.LogException(ipexc);
                                        Console.WriteLine("Exception in adding IPException: " + ipexc.Message);
                                    }
                                    from.SendGump(new AdminGump(from, m_PageType, 0, null, null, null));
                                    break;
                                }
                            case 2: //remove
                                {
                                    try
                                    {
                                        TextRelay relay = info.GetTextEntry(0);

                                        string ip = (relay == null ? null : relay.Text.Trim());

                                        if (ip != null)
                                        {
                                            from.SendMessage("Removing {0} from IPException", ip);
                                            Server.Accounting.IPException.Table.Remove(ip);
                                        }
                                        else
                                        {
                                            from.SendMessage("IP not entered");
                                        }
                                    }
                                    catch (Exception ipexc)
                                    {
                                        LogHelper.LogException(ipexc);
                                        Console.WriteLine("Exception in removing IPException: " + ipexc.Message);
                                    }
                                    from.SendGump(new AdminGump(from, m_PageType, 0, null, null, null));
                                    break;
                                }
                            default:
                                {
                                    //This is for "more info" on the IPException
                                    index -= 3;

                                    if (m_PageType == AdminGumpPage.IPException_IPDetails)
                                    {
                                        from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Information, 0, null, null, m_List[index]));
                                    }
                                    else
                                    {
                                        if (m_List != null && index >= 0 && index < m_List.Count)
                                        {
                                            string ipaddress = m_List[index] as String;
                                            from.SendMessage("Address selected is: " + ipaddress);
                                            from.SendGump(new AdminGump(from, AdminGumpPage.IPException_IPDetails, 0, null, null, ipaddress));
                                        }
                                    }

                                    break;
                                }
                        }

                        break;
                    }
                case 3:
                    {
                        string notice = null;
                        AdminGumpPage page = AdminGumpPage.Administer;

                        if (index >= 500)
                            page = AdminGumpPage.Administer_Access_Lockdown;
                        else if (index >= 400)
                            page = AdminGumpPage.Administer_Commands;
                        else if (index >= 300)
                            page = AdminGumpPage.Administer_Access;
                        else if (index >= 200)
                            page = AdminGumpPage.Administer_Server;
                        else if (index >= 100)
                            page = AdminGumpPage.Administer_WorldBuilding;

                        switch (index)
                        {
                            case 0: page = AdminGumpPage.Administer_WorldBuilding; break;
                            case 1: page = AdminGumpPage.Administer_Server; break;
                            case 2: page = AdminGumpPage.Administer_Access; break;
                            case 3: page = AdminGumpPage.Administer_Commands; break;

                            case 100: InvokeCommand("DocGen"); notice = "Documentation has been generated."; break;
                            case 101: InvokeCommand("TelGen"); notice = "Teleporters have been generated."; break;
                            case 102: InvokeCommand("MoonGen"); notice = "Moongates have been generated."; break;
                            case 103: InvokeCommand("UOAMVendors"); notice = "Vendor spawners have been generated."; break;
                            case 104: InvokeCommand("DoorGen"); notice = "Doors have been generated."; break;
                            case 105: InvokeCommand("SignGen"); notice = "Signs have been generated."; break;
                            case 106: InvokeCommand("Decorate"); notice = "Decoration has been generated."; break;
                            case 107: InvokeCommand("RebuildCategorization"); notice = "Categorization menu has been regenerated. The server should be restarted."; break;

                            case 110: InvokeCommand("Freeze"); notice = "Target bounding points."; break;
                            case 120: InvokeCommand("Unfreeze"); notice = "Target bounding points."; break;

                            case 200: InvokeCommand("Save"); notice = "The world has been saved."; break;
                            case 201: Shutdown(false, true); break;
                            case 202: Shutdown(false, false); break;
                            case 203: Shutdown(true, true); break;
                            case 204: Shutdown(true, false); break;
                            case 210:
                            case 211:
                                {
                                    TextRelay relay = info.GetTextEntry(0);
                                    string text = (relay == null ? null : relay.Text.Trim());

                                    if (text == null || text.Length == 0)
                                    {
                                        notice = "You must enter text to broadcast it.";
                                    }
                                    else
                                    {
                                        notice = "Your message has been broadcasted.";
                                        InvokeCommand(String.Format("{0} {1}", index == 210 ? "BC" : "SM", text));
                                    }

                                    break;
                                }

                            case 300: InvokeCommand("Kick"); notice = "Target the player to kick."; break;
                            case 301: InvokeCommand("Ban"); notice = "Target the player to ban."; break;
                            case 302: InvokeCommand("Firewall"); notice = "Target the player to firewall."; break;

                            case 303: page = AdminGumpPage.Administer_Access_Lockdown; break;

                            case 310: InvokeCommand("Set AccessLevel Player"); notice = "Target the player to change their access level. (Player)"; break;
                            case 311: InvokeCommand("Set AccessLevel Reporter"); notice = "Target the player to change their access level. (Reporter)"; break;
                            case 312: InvokeCommand("Set AccessLevel FightBroker"); notice = "Target the player to change their access level. (Fight Broker)"; break;
                            case 313: InvokeCommand("Set AccessLevel Counselor"); notice = "Target the player to change their access level. (Counselor)"; break;
                            case 314: InvokeCommand("Set AccessLevel GameMaster"); notice = "Target the player to change their access level. (Game Master)"; break;
                            case 315: InvokeCommand("Set AccessLevel Seer"); notice = "Target the player to change their access level. (Seer)"; break;
                            case 316: InvokeCommand("Set AccessLevel Administrator"); notice = "Target the player to change their access level. (Administrator)"; break;
                            case 317: InvokeCommand("Set AccessLevel Owner"); notice = "Target the player to change their access level. (Owner)"; break;


                            case 400: notice = "Enter search terms to add objects."; break;
                            case 401: InvokeCommand("Remove"); notice = "Target the item or mobile to remove."; break;
                            case 402: InvokeCommand("Dupe"); notice = "Target the item to dupe."; break;
                            case 403: InvokeCommand("DupeInBag"); notice = "Target the item to dupe. The item will be duped at it's current location."; break;
                            case 404: InvokeCommand("Props"); notice = "Target the item or mobile to inspect."; break;
                            case 405: InvokeCommand("Skills"); notice = "Target a mobile to view their skills."; break;
                            case 406: InvokeCommand("Set Blessed False"); notice = "Target the mobile to make mortal."; break;
                            case 407: InvokeCommand("Set Blessed True"); notice = "Target the mobile to make immortal."; break;
                            case 408: InvokeCommand("Set Squelched True"); notice = "Target the mobile to squelch."; break;
                            case 409: InvokeCommand("Set Squelched False"); notice = "Target the mobile to unsquelch."; break;
                            case 410: InvokeCommand("Set Frozen True"); notice = "Target the mobile to freeze."; break;
                            case 411: InvokeCommand("Set Frozen False"); notice = "Target the mobile to unfreeze."; break;
                            case 412: InvokeCommand("Set Hidden True"); notice = "Target the mobile to hide."; break;
                            case 413: InvokeCommand("Set Hidden False"); notice = "Target the mobile to unhide."; break;
                            case 414: InvokeCommand("Kill"); notice = "Target the mobile to kill."; break;
                            case 415: InvokeCommand("Resurrect"); notice = "Target the mobile to resurrect."; break;
                            case 416: InvokeCommand("Move"); notice = "Target the item or mobile to move."; break;
                            case 417: InvokeCommand("Wipe"); notice = "Target bounding points."; break;
                            case 418: InvokeCommand("Tele"); notice = "Choose your destination."; break;
                            case 419: InvokeCommand("Multi Tele"); notice = "Choose your destination."; break;

                            case 500:
                                {
                                    Misc.AccountHandler.LockdownLevel = AccessLevel.Player;
                                    notice = "The server is now accessible to everyone.";
                                    break;
                                }
                            case 501: notice = LockDown(AccessLevel.Reporter); break;
                            case 502: notice = LockDown(AccessLevel.FightBroker); break;
                            case 503: notice = LockDown(AccessLevel.Counselor); break;
                            case 504: notice = LockDown(AccessLevel.GameMaster); break;
                            case 505: notice = LockDown(AccessLevel.Seer); break;
                            case 506: notice = LockDown(AccessLevel.Administrator); break;
                            case 507: notice = LockDown(AccessLevel.Owner); break;

                            case 510:
                                {
                                    AccessLevel level = Misc.AccountHandler.LockdownLevel;

                                    if (level > AccessLevel.Player)
                                    {
                                        //ArrayList clients = NetState.Instances;
                                        List<NetState> clients = NetState.Instances;
                                        int count = 0;

                                        for (int i = 0; i < clients.Count; ++i)
                                        {
                                            NetState ns = clients[i];
                                            Account a = ns.Account as Account;

                                            if (a == null)
                                                continue;

                                            bool hasAccess = false;

                                            if (a.AccessLevel >= level)
                                            {
                                                hasAccess = true;
                                            }
                                            else
                                            {
                                                for (int j = 0; !hasAccess && j < 5; ++j)
                                                {
                                                    Mobile m = (Mobile)a[j];

                                                    if (m != null && m.AccessLevel >= level)
                                                        hasAccess = true;
                                                }
                                            }

                                            if (!hasAccess)
                                            {
                                                ns.Dispose();
                                                ++count;
                                            }
                                        }

                                        if (count == 0)
                                            notice = "Nobody without access was found to disconnect.";
                                        else
                                            notice = String.Format("Number of players disconnected: {0}", count);
                                    }
                                    else
                                    {
                                        notice = "The server is not currently locked down.";
                                    }

                                    break;
                                }
                        }

                        from.SendGump(new AdminGump(from, page, 0, null, notice, null));

                        switch (index)
                        {
                            case 400: InvokeCommand("Add"); break;
                            case 111: InvokeCommand("FreezeWorld"); break;
                            case 112: InvokeCommand("FreezeMap"); break;
                            case 121: InvokeCommand("UnfreezeWorld"); break;
                            case 122: InvokeCommand("UnfreezeMap"); break;
                        }

                        break;
                    }
                case 4:
                    {
                        switch (index)
                        {
                            case 0:
                            case 1:
                                {
                                    bool forName = (index == 0);

                                    ArrayList results = new ArrayList();

                                    TextRelay matchEntry = info.GetTextEntry(0);
                                    string match = (matchEntry == null ? null : matchEntry.Text.Trim().ToLower());
                                    string notice = null;

                                    if (match == null || match.Length == 0)
                                    {
                                        notice = String.Format("You must enter {0} to search.", forName ? "a name" : "an ip address");
                                    }
                                    else if (forName == false && Utility.IsValidIP(match) == false)
                                    {
                                        notice = String.Format("Bad IPAddress format.");
                                    }
                                    else
                                    {
                                        //ArrayList instances = NetState.Instances;
                                        List<NetState> instances = NetState.Instances;

                                        for (int i = 0; i < instances.Count; ++i)
                                        {
                                            NetState ns = instances[i];

                                            bool isMatch;

                                            if (forName)
                                            {
                                                Mobile m = ns.Mobile;
                                                Account a = ns.Account as Account;

                                                isMatch = (m != null && m.Name.ToLower().IndexOf(match) >= 0)
                                                    || (a != null && a.Username.ToLower().IndexOf(match) >= 0);
                                            }
                                            else
                                            {   // allow wild cards
                                                isMatch = Utility.IPMatch(match, ns.Address);
                                            }

                                            if (isMatch)
                                                results.Add(ns);
                                        }

                                        results.Sort(NetStateComparer.Instance);
                                    }

                                    if (results.Count == 1)
                                    {
                                        NetState ns = (NetState)results[0];
                                        object state = ns.Mobile;

                                        if (state == null)
                                            state = ns.Account;

                                        if (state is Mobile)
                                            from.SendGump(new AdminGump(from, AdminGumpPage.ClientInfo, 0, null, "One match found.", state));
                                        else if (state is Account)
                                            from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Information, 0, null, "One match found.", state));
                                        else
                                            from.SendGump(new AdminGump(from, AdminGumpPage.Clients, 0, results, "One match found.", null));
                                    }
                                    else
                                    {
                                        from.SendGump(new AdminGump(from, AdminGumpPage.Clients, 0, results, notice == null ? (results.Count == 0 ? "Nothing matched your search terms." : null) : notice, null));
                                    }

                                    break;
                                }
                            default:
                                {
                                    index -= 2;

                                    if (m_List != null && index >= 0 && index < m_List.Count)
                                    {
                                        NetState ns = m_List[index] as NetState;

                                        if (ns == null)
                                            break;

                                        Mobile m = ns.Mobile;
                                        Account a = ns.Account as Account;

                                        if (m != null)
                                            from.SendGump(new AdminGump(from, AdminGumpPage.ClientInfo, 0, null, null, m));
                                        else if (a != null)
                                            from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Information, 0, null, null, a));
                                    }

                                    break;
                                }
                        }

                        break;
                    }
                case 5:
                    {
                        switch (index)
                        {
                            case 0: from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Information, 0, null, null, m_State)); break;
                            case 1: from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Characters, 0, null, null, m_State)); break;
                            case 2: from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Comments, 0, null, null, m_State)); break;
                            case 3: from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Tags, 0, null, null, m_State)); break;
                            case 13: from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Access, 0, null, null, m_State)); break;
                            case 14: from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Access_ClientIPs, 0, null, null, m_State)); break;
                            case 15: from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Access_Restrictions, 0, null, null, m_State)); break;
                            case 36: from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Activation, 0, null, null, m_State)); break;
                            case 37: from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_EmailHistory, 0, null, null, m_State)); break;
                            case 4: from.Prompt = new AddCommentPrompt(m_State as Account); from.SendMessage("Enter the new account comment."); break;
                            case 5: from.Prompt = new AddTagNamePrompt(m_State as Account); from.SendMessage("Enter the new tag name."); break;
                            case 35: from.Prompt = new RemoveTagPrompt(m_State as Account); from.SendMessage("Enter the tag name to delete."); break;
                            case 6:
                                {
                                    TextRelay unEntry = info.GetTextEntry(0);
                                    TextRelay pwEntry = info.GetTextEntry(1);

                                    string un = (unEntry == null ? null : unEntry.Text.Trim());
                                    string pw = (pwEntry == null ? null : pwEntry.Text.Trim());

                                    Account dispAccount = null;
                                    string notice;

                                    if (un == null || un.Length == 0)
                                    {
                                        notice = "You must enter a username to add an account.";
                                    }
                                    else if (pw == null || pw.Length == 0)
                                    {
                                        notice = "You must enter a password to add an account.";
                                    }
                                    else
                                    {
                                        Account account = Accounts.GetAccount(un);

                                        if (account != null)
                                        {
                                            notice = "There is already an account with that username.";
                                        }
                                        else
                                        {
                                            dispAccount = Accounts.AddAccount(un, pw);
                                            notice = String.Format("{0} : Account added.", un);
                                            CommandLogging.WriteLine(from, "{0} {1} adding new account: {2}", from.AccessLevel, CommandLogging.Format(from), un);
                                        }
                                    }

                                    from.SendGump(new AdminGump(from, dispAccount != null ? AdminGumpPage.AccountDetails_Information : m_PageType, m_ListPage, m_List, notice, dispAccount != null ? dispAccount : m_State));
                                    break;
                                }
                            case 7:
                                {
                                    //Pix: now searches for username OR email address

                                    ArrayList results = new ArrayList();

                                    TextRelay matchEntry = info.GetTextEntry(2);

                                    string match = (matchEntry == null ? null : matchEntry.Text.Trim().ToLower());
                                    string notice = null;

                                    if (match == null || match.Length == 0)
                                    {
                                        try
                                        {
                                            //notice = "You must enter a username to search.";
                                            TextRelay emailEntry = info.GetTextEntry(3);

                                            string emailmatch = (emailEntry == null ? null : emailEntry.Text.Trim().ToLower());
                                            if (emailmatch == null || emailmatch.Length == 0)
                                            {
                                                notice = "You must enter a username or email to search.";
                                            }
                                            else
                                            {
                                                foreach (Account check in Accounts.Table.Values)
                                                {
                                                    if (check != null)
                                                    {
                                                        if (check.EmailAddress != null
                                                            && check.EmailAddress.Length > 0)
                                                        {
                                                            if (check.EmailAddress.ToLower().IndexOf(emailmatch) >= 0)
                                                                results.Add(check);
                                                        }
                                                    }
                                                }

                                                results.Sort(AccountComparer.Instance);
                                            }
                                        }
                                        catch (Exception exception)
                                        {
                                            LogHelper.LogException(exception);
                                            Console.WriteLine("<!><!><!><!><!><!><!><!><!><!>");
                                            Console.WriteLine("Exception in email search: " + exception.Message);
                                            Console.WriteLine(exception.StackTrace);
                                            Console.WriteLine("<!><!><!><!><!><!><!><!><!><!>");
                                        }
                                    }
                                    else
                                    {
                                        foreach (Account check in Accounts.Table.Values)
                                        {
                                            if (check.Username.ToLower().IndexOf(match) >= 0)
                                                results.Add(check);
                                        }

                                        results.Sort(AccountComparer.Instance);
                                    }

                                    if (results.Count == 1)
                                        from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Information, 0, null, "One match found.", results[0]));
                                    else
                                        from.SendGump(new AdminGump(from, AdminGumpPage.Accounts, 0, results, notice == null ? (results.Count == 0 ? "Nothing matched your search terms." : null) : notice, new ArrayList()));

                                    break;
                                }
                            case 8: from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_ChangePassword, 0, null, null, m_State)); break;
                            case 9: from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_ChangeAccess, 0, null, null, m_State)); break;
                            case 10:
                            case 11:
                                {
                                    Account a = m_State as Account;

                                    if (a == null)
                                        break;

                                    a.SetUnspecifiedBan(from);
                                    a.Banned = (index == 10);
                                    CommandLogging.WriteLine(from, "{0} {1} {3} account {2}", from.AccessLevel, CommandLogging.Format(from), a.Username, a.Banned ? "banning" : "unbanning");
                                    from.SendGump(new AdminGump(from, m_PageType, m_ListPage, m_List, String.Format("The account has been {0}.", a.Banned ? "banned" : "unbanned"), m_State));

                                    if (index == 10)
                                        from.SendGump(new BanDurationGump(a));

                                    break;
                                }
                            case 12:
                                {
                                    Account a = m_State as Account;

                                    if (a == null)
                                        break;

                                    TextRelay passwordEntry = info.GetTextEntry(0);
                                    TextRelay confirmEntry = info.GetTextEntry(1);

                                    string password = (passwordEntry == null ? null : passwordEntry.Text.Trim());
                                    string confirm = (confirmEntry == null ? null : confirmEntry.Text.Trim());

                                    string notice;
                                    AdminGumpPage page = AdminGumpPage.AccountDetails_ChangePassword;

                                    if (password == null || password.Length == 0)
                                    {
                                        notice = "You must enter the password.";
                                    }
                                    else if (confirm != password)
                                    {
                                        notice = "You must confirm the password. That field must precisely match the password field.";
                                    }
                                    else
                                    {
                                        notice = "The password has been changed.";
                                        a.SetPassword(password);
                                        page = AdminGumpPage.AccountDetails_Information;
                                        CommandLogging.WriteLine(from, "{0} {1} changing password of account {2}", from.AccessLevel, CommandLogging.Format(from), a.Username);
                                    }

                                    from.SendGump(new AdminGump(from, page, 0, null, notice, m_State));

                                    break;
                                }
                            case 16: // view shared
                                {
                                    Account a = m_State as Account;

                                    if (a == null)
                                        break;

                                    ArrayList list = GetSharedAccounts(a.LoginIPs);

                                    if (list.Count > 1 || (list.Count == 1 && !list.Contains(a)))
                                    {
                                        from.SendGump(new AdminGump(from, AdminGumpPage.Accounts, 0, list, null, new ArrayList()));
                                    }
                                    else if (a.LoginIPs.Length > 0)
                                    {
                                        from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Access_ClientIPs, 0, null, "There are no other accounts which share an address with this one.", m_State));
                                    }
                                    else
                                    {
                                        from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Access_ClientIPs, 0, null, "This account has not yet been accessed.", m_State));
                                    }

                                    break;
                                }
                            case 17: // ban shared
                                {
                                    Account a = m_State as Account;

                                    if (a == null)
                                        break;

                                    ArrayList list = GetSharedAccounts(a.LoginIPs);

                                    if (list.Count > 0)
                                    {
                                        StringBuilder sb = new StringBuilder();

                                        sb.AppendFormat("You are about to ban {0} account{1}. Do you wish to continue?", list.Count, list.Count != 1 ? "s" : "");

                                        for (int i = 0; i < list.Count; ++i)
                                            sb.AppendFormat("<br>- {0}", ((Account)list[i]).Username);

                                        from.SendGump(new WarningGump(1060635, 30720, sb.ToString(), 0xFFC000, 420, 400, new WarningGumpCallback(BanShared_Callback), a));
                                    }
                                    else if (a.LoginIPs.Length > 0)
                                    {
                                        from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Access_ClientIPs, 0, null, "There are no accounts which share an address with this one.", m_State));
                                    }
                                    else
                                    {
                                        from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Access_ClientIPs, 0, null, "This account has not yet been accessed.", m_State));
                                    }

                                    break;
                                }
                            case 18: // firewall all
                                {
                                    Account a = m_State as Account;

                                    if (a == null)
                                        break;

                                    if (a.LoginIPs.Length > 0)
                                    {
                                        from.SendGump(new WarningGump(1060635, 30720, String.Format("You are about to firewall {0} address{1}. Do you wish to continue?", a.LoginIPs.Length, a.LoginIPs.Length != 1 ? "s" : ""), 0xFFC000, 420, 400, new WarningGumpCallback(FirewallShared_Callback), a));
                                    }
                                    else
                                    {
                                        from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Access_ClientIPs, 0, null, "This account has not yet been accessed.", m_State));
                                    }

                                    break;
                                }
                            case 19: // add
                                {
                                    Account a = m_State as Account;

                                    if (a == null)
                                        break;

                                    TextRelay entry = info.GetTextEntry(0);
                                    string ip = (entry == null ? null : entry.Text.Trim());

                                    string notice;

                                    if (ip == null || ip.Length == 0)
                                    {
                                        notice = "You must enter an address to add.";
                                    }
                                    else
                                    {
                                        string[] list = a.IPRestrictions;

                                        bool contains = false;
                                        for (int i = 0; !contains && i < list.Length; ++i)
                                            contains = (list[i] == ip);

                                        if (contains)
                                        {
                                            notice = "That address is already contained in the list.";
                                        }
                                        else
                                        {
                                            string[] newList = new string[list.Length + 1];

                                            for (int i = 0; i < list.Length; ++i)
                                                newList[i] = list[i];

                                            newList[list.Length] = ip;

                                            a.IPRestrictions = newList;

                                            notice = String.Format("{0} : Added to restriction list.", ip);
                                        }
                                    }

                                    from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Access_Restrictions, 0, null, notice, m_State));

                                    break;
                                }
                            case 20:
                            case 21:
                            case 22:
                            case 23:
                            case 24:
                            case 25:
                            case 26:
                                {
                                    Account a = m_State as Account;

                                    if (a == null)
                                        break;

                                    AccessLevel newLevel;

                                    switch (index)
                                    {
                                        default:
                                        case 20: newLevel = AccessLevel.Player; break;
                                        case 21: newLevel = AccessLevel.Reporter; break;
                                        case 22: newLevel = AccessLevel.FightBroker; break;
                                        case 23: newLevel = AccessLevel.Counselor; break;
                                        case 24: newLevel = AccessLevel.GameMaster; break;
                                        case 25: newLevel = AccessLevel.Seer; break;
                                        case 26: newLevel = AccessLevel.Administrator; break;
                                    }

                                    a.AccessLevel = newLevel;

                                    CommandLogging.WriteLine(from, "{0} {1} changing access level of account {2} to {3}", from.AccessLevel, CommandLogging.Format(from), a.Username, a.AccessLevel);
                                    from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Information, 0, null, "The access level has been changed.", m_State));

                                    break;
                                }
                            case 27:
                                {
                                    Account a = m_State as Account;

                                    if (a == null)
                                        break;

                                    from.SendGump(new WarningGump(1060635, 30720, String.Format("<center>Account of {0}</center><br>You are about to <em><basefont color=red>permanently delete</basefont></em> the account. Likewise, all characters on the account will be deleted, including equiped, inventory, and banked items. Any houses tied to the account will be demolished.<br><br>Do you wish to continue?", a.Username), 0xFFC000, 420, 280, new WarningGumpCallback(AccountDelete_Callback), m_State));
                                    break;
                                }
                            case 28: // View all shared accounts
                                {
                                    from.SendGump(new AdminGump(from, AdminGumpPage.Accounts_Shared, 0, null, null, null));
                                    break;
                                }
                            case 29: // Ban marked
                                {
                                    ArrayList list = m_List;
                                    ArrayList rads = m_State as ArrayList;

                                    if (list == null || rads == null)
                                        break;

                                    if (rads.Count > 0)
                                        from.SendGump(new WarningGump(1060635, 30720, String.Format("You are about to ban {0} marked account{1}. Be cautioned, the only way to reverse this is by hand--manually unbanning each account.<br><br>Do you wish to continue?", rads.Count, rads.Count == 1 ? "" : "s"), 0xFFC000, 420, 280, new WarningGumpCallback(Marked_Callback), new object[] { true, list, rads, m_ListPage }));
                                    else
                                        from.SendGump(new NoticeGump(1060637, 30720, "You have not yet marked any accounts. Place a check mark next to the accounts you wish to ban and then try again.", 0xFFC000, 420, 280, new NoticeGumpCallback(ResendGump_Callback), new object[] { list, rads, m_ListPage }));

                                    break;
                                }
                            case 30: // Delete marked
                                {
                                    ArrayList list = m_List;
                                    ArrayList rads = m_State as ArrayList;

                                    if (list == null || rads == null)
                                        break;

                                    if (rads.Count > 0)
                                        from.SendGump(new WarningGump(1060635, 30720, String.Format("You are about to <em><basefont color=red>permanently delete</basefont></em> {0} marked account{1}. Likewise, all characters on the account{1} will be deleted, including equiped, inventory, and banked items. Any houses tied to the account{1} will be demolished.<br><br>Do you wish to continue?", rads.Count, rads.Count == 1 ? "" : "s"), 0xFFC000, 420, 280, new WarningGumpCallback(Marked_Callback), new object[] { false, list, rads, m_ListPage }));
                                    else
                                        from.SendGump(new NoticeGump(1060637, 30720, "You have not yet marked any accounts. Place a check mark next to the accounts you wish to ban and then try again.", 0xFFC000, 420, 280, new NoticeGumpCallback(ResendGump_Callback), new object[] { list, rads, m_ListPage }));

                                    break;
                                }
                            case 31: // Mark all
                                {
                                    ArrayList list = m_List;
                                    ArrayList rads = m_State as ArrayList;

                                    if (list == null || rads == null)
                                        break;

                                    from.SendGump(new AdminGump(from, AdminGumpPage.Accounts, m_ListPage, m_List, null, new ArrayList(list)));

                                    break;
                                }
                            case 32: // View all staff accounts
                                {
                                    ArrayList results = new ArrayList();

                                    foreach (Account acct in Accounts.Table.Values)
                                    {
                                        for (int i = 0; i < 5; ++i)
                                            if (acct[i] != null)
                                                if (acct[i].AccessLevel > AccessLevel.Player && acct[i].AccessLevel <= AccessLevel.Owner)
                                                    results.Add(acct);
                                    }

                                    if (results.Count == 1)
                                        from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Information, 0, null, "One match found.", results[0]));
                                    else
                                        from.SendGump(new AdminGump(from, AdminGumpPage.Accounts, 0, results, (results.Count == 0 ? "Nothing matched your search terms." : null), new ArrayList()));

                                    break;
                                }
                            case 33: // View all inactive accounts
                                {
                                    ArrayList results = new ArrayList();

                                    DateTime minTime = DateTime.Now - TimeSpan.FromDays(30.0);

                                    foreach (Account acct in Accounts.Table.Values)
                                    {
                                        if (acct.LastLogin <= minTime)
                                            results.Add(acct);
                                    }

                                    if (results.Count == 1)
                                        from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Information, 0, null, "One match found.", results[0]));
                                    else
                                        from.SendGump(new AdminGump(from, AdminGumpPage.Accounts, 0, results, (results.Count == 0 ? "Nothing matched your search terms." : null), new ArrayList()));

                                    break;
                                }
                            case 34: // View all banned accounts
                                {
                                    ArrayList results = new ArrayList();

                                    foreach (Account acct in Accounts.Table.Values)
                                    {
                                        if (acct.Banned)
                                            results.Add(acct);
                                    }

                                    if (results.Count == 1)
                                        from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Information, 0, null, "One match found.", results[0]));
                                    else
                                        from.SendGump(new AdminGump(from, AdminGumpPage.Accounts, 0, results, (results.Count == 0 ? "Nothing matched your search terms." : null), new ArrayList()));

                                    break;
                                }
                            case 38: //reset activation
                                {
                                    Account a = m_State as Account;

                                    if (a == null)
                                        break;

                                    a.ActivationKey = "";
                                    a.AccountActivated = false;

                                    string note = string.Format("Activation reset for account {0}.", a.Username);

                                    from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Activation, 0, null, note, m_State));
                                    break;
                                }
                            case 39: //resend activation
                                {
                                    Account a = m_State as Account;

                                    if (a == null)
                                    {
                                        from.SendMessage("Account problem (a==null).");
                                    }
                                    else
                                    {
                                        if (a.AccountActivated)
                                        {
                                            from.SendMessage("Account is already activated.");
                                        }
                                        else if (a.EmailAddress == null || a.EmailAddress.Length < 4)
                                        {
                                            from.SendMessage("Account doesn't have email address.");
                                        }
                                        else if (a.ActivationKey == null || a.ActivationKey.Length <= 0)
                                        {
                                            from.SendMessage("Account doesn't have activation key.");
                                        }
                                        else
                                        {
                                            if (ProfileGump.SendActivationEmail(a.EmailAddress, a.ActivationKey, true))
                                            {
                                                from.SendMessage("Activation email resent.");
                                            }
                                            else
                                            {
                                                from.SendMessage("Error sending email!");
                                            }
                                        }
                                    }

                                    from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Activation, 0, null, null, m_State));
                                    break;
                                }
                            case 40: //search IP
                                {
                                    ArrayList results = new ArrayList();

                                    TextRelay matchEntry = info.GetTextEntry(4);

                                    string match = (matchEntry == null ? null : matchEntry.Text.Trim().ToLower());
                                    string notice = null;

                                    if (match == null || match.Length == 0)
                                    {
                                    }
                                    else
                                    {
                                        try
                                        {
                                            if (Utility.IsValidIP(match) == false)
                                            {
                                                from.SendMessage("Bad IPAddress format.");
                                            }
                                            else
                                            {
                                                foreach (Account check in Accounts.Table.Values)
                                                {
                                                    for (int i = 0; i < check.LoginIPs.Length; i++)
                                                    {
                                                        if (Utility.IPMatch(match, check.LoginIPs[i]))
                                                        {
                                                            results.Add(check);
                                                        }
                                                    }

                                                    results.Sort(AccountComparer.Instance);
                                                }
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            LogHelper.LogException(e);
                                            System.Console.WriteLine("Exception in IP Searching: " + e.Message);
                                        }
                                    }

                                    if (results.Count == 1)
                                        from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Information, 0, null, "One match found.", results[0]));
                                    else
                                        from.SendGump(new AdminGump(from, AdminGumpPage.Accounts, 0, results, notice == null ? (results.Count == 0 ? "Nothing matched your search terms." : null) : notice, new ArrayList()));

                                    break;
                                }
                            case 41: //Toggle Notification Emails
                                {
                                    Account a = m_State as Account;

                                    if (a == null)
                                    {
                                        from.SendMessage("Account problem (a==null).");
                                    }
                                    else
                                    {
                                        if (a.DoNotSendEmail)
                                        {
                                            a.DoNotSendEmail = false;
                                        }
                                        else
                                        {
                                            a.DoNotSendEmail = true;
                                        }
                                    }

                                    from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Activation, 0, null, null, m_State));
                                    break;
                                }
                            default:
                                {
                                    index -= 50;

                                    if (index >= 0 && index < 5)
                                    {
                                        Account a = m_State as Account;

                                        if (a == null)
                                            break;

                                        Mobile m = a[index];

                                        if (m != null)
                                            from.SendGump(new AdminGump(from, AdminGumpPage.ClientInfo, 0, null, null, m));
                                    }
                                    else
                                    {
                                        index -= 5;

                                        if (m_List != null && index >= 0 && index < m_List.Count)
                                        {
                                            if (m_List[index] is Account)
                                                from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Information, 0, null, null, m_List[index]));
                                            else if (m_List[index] is DictionaryEntry)
                                                from.SendGump(new AdminGump(from, AdminGumpPage.Accounts, 0, (ArrayList)(((DictionaryEntry)m_List[index]).Value), null, new ArrayList()));
                                        }
                                    }

                                    break;
                                }
                        }

                        break;
                    }
                case 6:
                    {
                        switch (index)
                        {
                            case 0:
                                {
                                    TextRelay matchEntry = info.GetTextEntry(0);
                                    string match = (matchEntry == null ? null : matchEntry.Text.Trim());

                                    string notice = null;
                                    ArrayList results = new ArrayList();

                                    if (match == null || match.Length == 0)
                                    {
                                        notice = "You must enter a username to search.";
                                    }
                                    else
                                    {
                                        for (int i = 0; i < Firewall.List.Count; ++i)
                                        {
                                            string check = Firewall.List[i].ToString();

                                            if (check.IndexOf(match) >= 0)
                                                results.Add(Firewall.List[i]);
                                        }
                                    }

                                    if (results.Count == 1)
                                        from.SendGump(new AdminGump(from, AdminGumpPage.FirewallInfo, 0, null, "One match found.", results[0]));
                                    else if (results.Count > 1)
                                        from.SendGump(new AdminGump(from, AdminGumpPage.Firewall, 0, results, String.Format("Search results for : {0}", match), m_State));
                                    else
                                        from.SendGump(new AdminGump(from, m_PageType, m_ListPage, m_List, notice == null ? "Nothing matched your search terms." : notice, m_State));

                                    break;
                                }
                            case 1:
                                {
                                    TextRelay relay = info.GetTextEntry(0);
                                    string text = (relay == null ? null : relay.Text.Trim());

                                    if (text == null || text.Length == 0)
                                    {
                                        from.SendGump(new AdminGump(from, m_PageType, m_ListPage, m_List, "You must enter an address or pattern to add.", m_State));
                                    }
                                    else if (!Utility.IsValidIP(text))
                                    {
                                        from.SendGump(new AdminGump(from, m_PageType, m_ListPage, m_List, "That is not a valid address or pattern.", m_State));
                                    }
                                    else
                                    {
                                        object toAdd;

                                        try { toAdd = IPAddress.Parse(text); }
                                        catch { toAdd = text; }

                                        CommandLogging.WriteLine(from, "{0} {1} firewalling {2}", from.AccessLevel, CommandLogging.Format(from), toAdd);

                                        Firewall.Add(toAdd);
                                        from.SendGump(new AdminGump(from, AdminGumpPage.FirewallInfo, 0, null, String.Format("{0} : Added to firewall.", toAdd), toAdd));
                                    }

                                    break;
                                }
                            case 2:
                                {
                                    InvokeCommand("Firewall");
                                    from.SendGump(new AdminGump(from, m_PageType, m_ListPage, m_List, "Target the player to firewall.", m_State));
                                    break;
                                }
                            case 3:
                                {
                                    if (m_State is IPAddress || m_State is String)
                                    {
                                        CommandLogging.WriteLine(from, "{0} {1} removing {2} from firewall list", from.AccessLevel, CommandLogging.Format(from), m_State);

                                        Firewall.List.Remove(m_State);
                                        Firewall.Save();
                                        from.SendGump(new AdminGump(from, AdminGumpPage.Firewall, 0, null, String.Format("{0} : Removed from firewall.", m_State), null));
                                    }

                                    break;
                                }
                            default:
                                {
                                    index -= 4;

                                    if (m_List != null && index >= 0 && index < m_List.Count)
                                        from.SendGump(new AdminGump(from, AdminGumpPage.FirewallInfo, 0, null, null, m_List[index]));

                                    break;
                                }
                        }

                        break;
                    }
                case 7:
                    {
                        Mobile m = m_State as Mobile;

                        if (m == null)
                            break;

                        string notice = null;
                        bool sendGump = true;

                        switch (index)
                        {
                            case 0:
                                {
                                    Map map = m.Map;
                                    Point3D loc = m.Location;

                                    if (map == null || map == Map.Internal)
                                    {
                                        map = m.LogoutMap;
                                        loc = m.LogoutLocation;
                                    }

                                    if (map != null && map != Map.Internal)
                                    {
                                        from.MoveToWorld(loc, map);
                                        notice = "You have been teleported to their location.";
                                    }

                                    break;
                                }
                            case 1:
                                {
                                    m.MoveToWorld(from.Location, from.Map);
                                    notice = "They have been teleported to your location.";
                                    break;
                                }
                            case 2:
                                {
                                    NetState ns = m.NetState;

                                    if (ns != null)
                                    {
                                        CommandLogging.WriteLine(from, "{0} {1} {2} {3}", from.AccessLevel, CommandLogging.Format(from), "kicking", CommandLogging.Format(m));
                                        ns.Dispose();
                                        notice = "They have been kicked.";
                                    }
                                    else
                                    {
                                        notice = "They are already disconnected.";
                                    }

                                    break;
                                }
                            case 3:
                                {
                                    Account a = m.Account as Account;

                                    if (a != null)
                                    {
                                        CommandLogging.WriteLine(from, "{0} {1} {2} {3}", from.AccessLevel, CommandLogging.Format(from), "banning", CommandLogging.Format(m));
                                        a.Banned = true;

                                        NetState ns = m.NetState;

                                        if (ns != null)
                                            ns.Dispose();

                                        notice = "They have been banned.";
                                    }

                                    break;
                                }
                            case 6:
                                {
                                    Properties.SetValue(from, m, "Blessed", "False");
                                    notice = "They are now mortal.";
                                    break;
                                }
                            case 7:
                                {
                                    Properties.SetValue(from, m, "Blessed", "True");
                                    notice = "They are now immortal.";
                                    break;
                                }
                            case 8:
                                {
                                    Properties.SetValue(from, m, "Squelched", "True");
                                    notice = "They are now squelched.";
                                    break;
                                }
                            case 9:
                                {
                                    Properties.SetValue(from, m, "Squelched", "False");
                                    notice = "They are now unsquelched.";
                                    break;
                                }
                            case 10:
                                {
                                    Properties.SetValue(from, m, "Hidden", "True");
                                    notice = "They are now hidden.";
                                    break;
                                }
                            case 11:
                                {
                                    Properties.SetValue(from, m, "Hidden", "False");
                                    notice = "They are now unhidden.";
                                    break;
                                }
                            case 12:
                                {
                                    CommandLogging.WriteLine(from, "{0} {1} killing {2}", from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(m));
                                    m.Kill();
                                    notice = "They have been killed.";
                                    break;
                                }
                            case 13:
                                {
                                    CommandLogging.WriteLine(from, "{0} {1} resurrecting {2}", from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(m));
                                    m.Resurrect();
                                    notice = "They have been resurrected.";
                                    break;
                                }
                            case 14:
                                {
                                    CommandLogging.WriteLine(from, "{0} {1} Viewing Houses of {2}", from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(m));
                                    from.SendGump(new ViewHousesGump(from, ViewHousesGump.GetHouses((m)), null));
                                    //InvokeCommand( "ViewHouses" ); 
                                    //notice = "Target the mobile to unsquelch."; 
                                    break;
                                }
                            case 15:
                                {
                                    from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Information, 0, null, null, m.Account));
                                    sendGump = false;
                                    break;
                                }
                        }

                        if (sendGump)
                            from.SendGump(new AdminGump(from, AdminGumpPage.ClientInfo, 0, null, notice, m_State));

                        switch (index)
                        {
                            case 3:
                                {
                                    Account a = m.Account as Account;

                                    if (a != null)
                                        from.SendGump(new BanDurationGump(a));

                                    break;
                                }
                            case 4:
                                {
                                    from.SendGump(new PropertiesGump(from, m));
                                    break;
                                }
                            case 5:
                                {
                                    from.SendGump(new Server.Scripts.Gumps.SkillsGump(from, m));
                                    break;
                                }
                        }

                        break;
                    }
                case 8:
                    {
                        if (m_List != null && index >= 0 && index < m_List.Count)
                        {
                            Account a = m_State as Account;

                            if (a == null)
                                break;

                            if (m_PageType == AdminGumpPage.AccountDetails_Access_ClientIPs)
                            {
                                from.SendGump(new WarningGump(1060635, 30720, String.Format("You are about to firewall {0}. All connection attempts from a matching IP will be refused. Are you sure?", m_List[index]), 0xFFC000, 420, 280, new WarningGumpCallback(Firewall_Callback), new object[] { a, m_List[index] }));
                            }
                            else if (m_PageType == AdminGumpPage.AccountDetails_Access_Restrictions)
                            {
                                ArrayList list = new ArrayList(a.IPRestrictions);

                                list.Remove(m_List[index]);

                                a.IPRestrictions = (string[])list.ToArray(typeof(string));

                                from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Access_Restrictions, 0, null, String.Format("{0} : Removed from list.", m_List[index]), a));
                            }
                        }

                        break;
                    }
                case 9:
                    {
                        if (m_List != null && index >= 0 && index < m_List.Count)
                        {
                            if (m_PageType == AdminGumpPage.AccountDetails_Access_ClientIPs)
                            {
                                object obj = m_List[index];

                                if (!(obj is IPAddress))
                                    break;

                                Account a = m_State as Account;

                                if (a == null)
                                    break;

                                ArrayList list = GetSharedAccounts((IPAddress)obj);

                                if (list.Count > 1 || (list.Count == 1 && !list.Contains(a)))
                                    from.SendGump(new AdminGump(from, AdminGumpPage.Accounts, 0, list, null, new ArrayList()));
                                else
                                    from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Access_ClientIPs, 0, null, "There are no other accounts which share that address.", m_State));
                            }
                        }

                        break;
                    }
            }
        }

        private void Shutdown(bool restart, bool save)
        {
            CommandLogging.WriteLine(m_From, "{0} {1} shuting down server (Restart: {2}) (Save: {3})", m_From.AccessLevel, CommandLogging.Format(m_From), restart, save);

            if (save)
                InvokeCommand("Save");

            Core.Kill(restart);
        }

        private void InvokeCommand(string ip)
        {
            CommandSystem.Handle(m_From, String.Format("{0}{1}", Server.CommandSystem.CommandPrefix, ip));
        }

        private String LockDown(AccessLevel al)
        {
            switch (al)
            {
                case AccessLevel.Reporter: Misc.AccountHandler.LockdownLevel = AccessLevel.Reporter; break;
                case AccessLevel.FightBroker: Misc.AccountHandler.LockdownLevel = AccessLevel.FightBroker; break;
                case AccessLevel.Counselor: Misc.AccountHandler.LockdownLevel = AccessLevel.Counselor; break;
                case AccessLevel.GameMaster: Misc.AccountHandler.LockdownLevel = AccessLevel.GameMaster; break;
                case AccessLevel.Seer: Misc.AccountHandler.LockdownLevel = AccessLevel.Seer; break;
                case AccessLevel.Administrator: Misc.AccountHandler.LockdownLevel = AccessLevel.Administrator; break;
                case AccessLevel.Owner: Misc.AccountHandler.LockdownLevel = AccessLevel.Owner; break;
            }
            return "The lockdown level has been changed.";
        }

        private class AddCommentPrompt : Prompt
        {
            private Account m_Account;

            public AddCommentPrompt(Account acct)
            {
                m_Account = acct;
            }

            public override void OnCancel(Mobile from)
            {
                from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Comments, 0, null, "Request to add comment was canceled.", m_Account));
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (m_Account != null)
                {
                    m_Account.Comments.Add(new AccountComment(from.Name, text));
                    from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Comments, 0, null, "Comment added.", m_Account));
                }
            }
        }

        private class AddTagNamePrompt : Prompt
        {
            private Account m_Account;

            public AddTagNamePrompt(Account acct)
            {
                m_Account = acct;
            }

            public override void OnCancel(Mobile from)
            {
                from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Tags, 0, null, "Request to add tag was canceled.", m_Account));
            }

            public override void OnResponse(Mobile from, string text)
            {
                from.Prompt = new AddTagValuePrompt(m_Account, text);
                from.SendMessage("Enter the new tag value.");
            }
        }

        private class AddTagValuePrompt : Prompt
        {
            private Account m_Account;
            private string m_Name;

            public AddTagValuePrompt(Account acct, string name)
            {
                m_Account = acct;
                m_Name = name;
            }

            public override void OnCancel(Mobile from)
            {
                from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Tags, 0, null, "Request to add tag was canceled.", m_Account));
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (m_Account != null)
                {
                    if (m_Account.GetTag(m_Name) != null)
                        m_Account.SetTag(m_Name, text);
                    else
                        m_Account.AddTag(m_Name, text);

                    from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Tags, 0, null, "Tag added.", m_Account));
                }
            }
        }

        private class RemoveTagPrompt : Prompt
        {
            private Account m_Account;

            public RemoveTagPrompt(Account acct)
            {
                m_Account = acct;
            }

            public override void OnCancel(Mobile from)
            {
                from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Tags, 0, null, "Request to remove tag was cancelled.", m_Account));
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (m_Account != null)
                {
                    if (m_Account.GetTag(text) != null)
                        m_Account.RemoveTag(text);
                    else
                        from.SendMessage("That tag doesn't exist.");
                }

                from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Tags, 0, null, "Tag removed.", m_Account));
            }
        }

        private class NetStateComparer : IComparer
        {
            public static readonly IComparer Instance = new NetStateComparer();

            public NetStateComparer()
            {
            }

            public int Compare(object x, object y)
            {
                if (x == null && y == null)
                    return 0;
                else if (x == null)
                    return -1;
                else if (y == null)
                    return 1;

                NetState a = x as NetState;
                NetState b = y as NetState;

                if (a == null || b == null)
                    throw new ArgumentException();

                Mobile aMob = a.Mobile;
                Mobile bMob = b.Mobile;

                if (aMob == null && bMob == null)
                    return 0;
                else if (aMob == null)
                    return 1;
                else if (bMob == null)
                    return -1;

                if (aMob.AccessLevel > bMob.AccessLevel)
                    return -1;
                else if (aMob.AccessLevel < bMob.AccessLevel)
                    return 1;
                else
                    return Insensitive.Compare(aMob.Name, bMob.Name);
            }
        }

        private class AccountComparer : IComparer
        {
            public static readonly IComparer Instance = new AccountComparer();

            public AccountComparer()
            {
            }

            public int Compare(object x, object y)
            {
                if (x == null && y == null)
                    return 0;
                else if (x == null)
                    return -1;
                else if (y == null)
                    return 1;

                Account a = x as Account;
                Account b = y as Account;

                if (a == null || b == null)
                    throw new ArgumentException();

                AccessLevel aLevel, bLevel;
                bool aOnline, bOnline;

                a.GetAccountInfo(out aLevel, out aOnline);
                b.GetAccountInfo(out bLevel, out bOnline);

                if (aOnline && !bOnline)
                    return -1;
                else if (bOnline && !aOnline)
                    return 1;
                else if (aLevel > bLevel)
                    return -1;
                else if (aLevel < bLevel)
                    return 1;
                else
                    return Insensitive.Compare(a.Username, b.Username);
            }
        }
    }
}
