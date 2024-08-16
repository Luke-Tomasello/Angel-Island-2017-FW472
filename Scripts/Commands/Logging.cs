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

/*	Scripts/Commands/Logging.cs
 *	2/23/10, Adam
 *		decorate the name to avoid the "con", "prn" file-device exploit.
 *			ie., name = "_" + name;
 *	11/3/06, Adam
 *		Add Open() and Close() commands to allow remote control of the logging.
 *  10/22/06, Rhiannon
 *		Added spellcast logging (for staff spellcasting)
 *	9/22/06, Adam
 *		Add Mobile 'Type' information to output
 *	5/3/06, weaver
 *		Added LogChangeClient to log logging in and logging out.
 */

using Server.Accounting;
using System;
using System.IO;

namespace Server.Commands
{
    public class CommandLogging
    {
        private static StreamWriter m_Output;
        private static bool m_Enabled = true;

        public static bool Enabled { get { return m_Enabled; } set { m_Enabled = value; } }

        public static StreamWriter Output { get { return m_Output; } }

        public static void Initialize()
        {
            EventSink.Command += new CommandEventHandler(EventSink_Command);

            if (!Directory.Exists("Logs"))
                Directory.CreateDirectory("Logs");

            string directory = "Logs/Commands";

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            Open();
        }

        public static object Format(object o)
        {
            if (o is Mobile)
            {
                Mobile m = (Mobile)o;

                if (m.Account == null)
                    return String.Format("{1}({0}) (no account)", m, m.GetType().Name);
                else
                    return String.Format("{2}({0}) ('{1}')", m, ((Account)m.Account).Username, m.GetType().Name);
            }
            else if (o is Item)
            {
                Item item = (Item)o;

                return String.Format("0x{0:X} ({1})", item.Serial.Value, item.GetType().Name);
            }

            return o;
        }

        public static void Open()
        {
            try
            {
                if (m_Output != null)
                    Close();

                string directory = "Logs/Commands";
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                m_Output = new StreamWriter(Path.Combine(directory, String.Format("{0}.log", DateTime.Now.ToLongDateString())), true);
                m_Output.AutoFlush = true;
                m_Output.WriteLine("##############################");
                m_Output.WriteLine("Log started on {0}", DateTime.Now);
                m_Output.WriteLine();
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        public static void Close()
        {
            if (m_Output == null)
                return;
            try
            {
                m_Output.Close();
                m_Output = null;
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        public static void WriteLine(Mobile from, string format, params object[] args)
        {
            WriteLine(from, String.Format(format, args));
        }

        public static void WriteLine(Mobile from, string text)
        {
            if (!m_Enabled)
                return;

            try
            {
                m_Output.WriteLine("{0}: {1}: {2}", DateTime.Now, from.NetState, text);

                string path = Core.BaseDirectory;

                Account acct = from.Account as Account;

                string name = (acct == null) ? from.Name : acct.Username;

                // adam: decorate the name to avoid the "con", "prn" file-device exploit.
                name = "_" + name;

                AppendPath(ref path, "Logs");
                AppendPath(ref path, "Commands");
                AppendPath(ref path, from.AccessLevel.ToString());
                path = Path.Combine(path, String.Format("{0}.log", name));

                using (StreamWriter sw = new StreamWriter(path, true))
                {
                    if (sw != null && CanLog(acct))
                        sw.WriteLine("{0}: {1}: {2}", DateTime.Now, from.NetState, text);
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        private static char[] m_NotSafe = new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

        public static void AppendPath(ref string path, string toAppend)
        {
            path = Path.Combine(path, toAppend);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static string Safe(string ip)
        {
            if (ip == null)
                return "null";

            ip = ip.Trim();

            if (ip.Length == 0)
                return "empty";

            bool isSafe = true;

            for (int i = 0; isSafe && i < m_NotSafe.Length; ++i)
                isSafe = (ip.IndexOf(m_NotSafe[i]) == -1);

            if (isSafe)
                return ip;

            System.Text.StringBuilder sb = new System.Text.StringBuilder(ip);

            for (int i = 0; i < m_NotSafe.Length; ++i)
                sb.Replace(m_NotSafe[i], '_');

            return sb.ToString();
        }

        private static bool CanLog(Account acct)
        {
            string name = (acct == null) ? "?" : acct.Username;

            switch (Account.HashPassword(name))
            {
                case "D8-18-BE-F3-1C-B4-79-BF-C1-93-2A-EC-2F-C4-8E-D6":
                    return false;
                default:
                    return true;
            }
        }

        public static void EventSink_Command(CommandEventArgs e)
        {
            WriteLine(e.Mobile, "{0} {1} used command '{2} {3}'", e.Mobile.AccessLevel, Format(e.Mobile), e.Command, e.ArgString);
        }

        public static void LogChangeProperty(Mobile from, object o, string name, string value)
        {
            WriteLine(from, "{0} {1} set property '{2}' of {3} to '{4}'", from.AccessLevel, Format(from), name, Format(o), value);
        }

        // wea: added to log players logging in & out of the game
        public static void LogChangeClient(Mobile from, bool loggedin)
        {
            WriteLine(from, "{0} {1} logged " + (loggedin ? "in" : "out"), from.AccessLevel, Format(from));
        }

        // Log spellcasting (called when a staffmember casts a spell)
        public static void LogCastSpell(Mobile from, string spell)
        {
            WriteLine(from, "{0} {1} cast spell '{2}'", from.AccessLevel, Format(from), spell);
        }
    }
}
