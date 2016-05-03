﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Steamworks;

namespace steam_idle
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {            
            /*
             * Dependencies
             * Steamworks.NET
             * https://github.com/rlabrecque/Steamworks.NET/tree/master/Standalone
             * 
             * CSteamworks.dll
             * steam_api.dll
             * https://github.com/rlabrecque/Steamworks.NET/tree/master/Plugins/x86
             */

            if (args == null || args.Length == 0)
                return;

            long appId = long.Parse(args[0]);
            String NL = Environment.NewLine;
            try
            {
                Environment.SetEnvironmentVariable("SteamAppId", appId.ToString());

                if (!SteamAPI.Init())                
                    return;                
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("{1}{0} {2}{0}", NL, ex.Message, ex.StackTrace), ex.GetType().FullName,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain(appId));
        }
    }
}