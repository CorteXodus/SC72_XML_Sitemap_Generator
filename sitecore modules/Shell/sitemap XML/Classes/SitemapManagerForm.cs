/* *********************************************************************** *
 * File   : SitemapManagerForm.cs                         Part of Sitecore *
 * Version: 1.0.0                                         www.sitecore.net *
 *                                                                         *
 *                                                                         *
 * Purpose: Codebehind of ManagerForm                                      *
 *                                                                         *
 * Copyright (C) 1999-2009 by Sitecore A/S. All rights reserved.           *
 *                                                                         *
 * This work is the property of:                                           *
 *                                                                         *
 *        Sitecore A/S                                                     *
 *        Meldahlsgade 5, 4.                                               *
 *        1613 Copenhagen V.                                               *
 *        Denmark                                                          *
 *                                                                         *
 * This is a Sitecore published work under Sitecore's                      *
 * shared source license.                                                  *
 *                                                                         *
 * *********************************************************************** */
/* Modified and expanded by David R. Smith for Regis University 12-2014 */

using System;
using System.Diagnostics;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Diagnostics;
using System.Collections.Specialized;
using System.Text;

namespace Sitecore.Modules.SitemapXML
{
    public class SitemapManagerForm : Sitecore.Web.UI.Sheer.BaseForm
    {
        protected Button RefreshButton;
        protected Button SubmitToEnginesButton;
        protected Literal Message;
        private const string MSG_REFRESH = " - The sitemap file <b>\"{0}\"</b> has been refreshed ";
        private const string MSG_SUBMITSUCCESS = " - The sitemap file <b>\"{0}\"</b> has been registered with the specified search engines ";
        private const string MSG_SUBMITFAIL = " - One or more sitemap submissions failed: Check Sitecore logs for additional information ";
        private const string MSG_ELAPSED = "time elapsed: ";
        private const string MSG_BREAK = "<br />";
        private const string MSG_COMMA = ", ";
        private const string MSG_EMPTY = "";
        private const string PANEL_MAIN = "MainPanel";


        protected override void OnLoad(EventArgs args)
        {
            base.OnLoad(args);
            if (!Sitecore.Context.ClientPage.IsEvent)
            {
                RefreshButton.Click = "RefreshButtonClick";
                SubmitToEnginesButton.Click = "SubmitButtonClick";
            }
        }

        protected void RefreshButtonClick()
        {
            Stopwatch refreshStopwatch = new Stopwatch();
            refreshStopwatch.Start();
            var sh = new SitemapHandler();
            sh.RefreshSitemap(this, new EventArgs());
            StringDictionary sites = SitemapManagerConfiguration.GetSites();
            StringBuilder sb = new StringBuilder();
            foreach (string sitemapFile in sites.Values)
            {
                if (sb.Length > 0)
                    sb.Append(MSG_COMMA);
                sb.Append(sitemapFile);
            }

            DateTime currDateTime = DateTime.Now;
            string msgDateTime = currDateTime.ToString();

            string currentMessage = MSG_EMPTY;
            String previousMessage = Message.Text;

            refreshStopwatch.Stop();
            string refreshTime = MSG_COMMA + MSG_ELAPSED + refreshStopwatch.Elapsed.ToString("mm\\:ss");

            if (previousMessage != MSG_EMPTY)
            {
                currentMessage = string.Concat(string.Format(previousMessage + MSG_REFRESH + msgDateTime, sb.ToString()), refreshTime + MSG_BREAK);
            }
            else
            {
                currentMessage = string.Concat(string.Format(MSG_REFRESH + msgDateTime, sb.ToString()), refreshTime + MSG_BREAK);
            }

            Message.Text = currentMessage;

            RefreshPanel(PANEL_MAIN);
        }

        protected void SubmitButtonClick()
        {
            var sh = new SitemapHandler();

            DateTime currDateTime = DateTime.Now;
            string msgDateTime = currDateTime.ToString();

            string currentMessage = MSG_EMPTY;
            string previousMessage = Message.Text;
            
            if ( sh.SubmitSitemap(this, new EventArgs()) )
            { 
                StringDictionary sites = SitemapManagerConfiguration.GetSites();
                StringBuilder sb = new StringBuilder();
                foreach (string sitemapFile in sites.Values)
                {
                    if (sb.Length > 0)
                        sb.Append(MSG_COMMA);
                    sb.Append(sitemapFile);
                }

                if (previousMessage != MSG_EMPTY)
                {
                    currentMessage = string.Format(previousMessage + MSG_SUBMITSUCCESS + msgDateTime + MSG_BREAK, sb.ToString());
                }
                else
                {
                    currentMessage = string.Format(MSG_SUBMITSUCCESS + msgDateTime + MSG_BREAK, sb.ToString());
                }

                Message.Text = currentMessage;

                RefreshPanel(PANEL_MAIN);
            }
            else
            {
                if (previousMessage != MSG_EMPTY)
                {
                    currentMessage = previousMessage + MSG_SUBMITFAIL + msgDateTime + MSG_BREAK;
                }

                Message.Text = MSG_SUBMITFAIL + MSG_BREAK;

                RefreshPanel(PANEL_MAIN);
            }
        }

        private static void RefreshPanel(string panelName)
        {
            Sitecore.Web.UI.HtmlControls.Panel ctl = Sitecore.Context.ClientPage.FindControl(panelName) as
                Sitecore.Web.UI.HtmlControls.Panel;
            Assert.IsNotNull(ctl, "can't find panel");

            Sitecore.Context.ClientPage.ClientResponse.Refresh(ctl);
        }
    }
}
