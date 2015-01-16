/* *********************************************************************** *
 * File   : SitemapManager.cs                             Part of Sitecore *
 * Version: 1.0.0                                         www.sitecore.net *
 *                                                                         *
 *                                                                         *
 * Purpose: Manager class what contains all main logic                     *
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
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Sitecore.Data.Items;
using Sitecore.Sites;
using Sitecore.Data;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using System.Web;
using System.Text;
using System.Linq;
using System.Collections.Specialized;
using System.Collections;
using Sitemap.XML.Helpers;

namespace Sitecore.Modules.SitemapXML
{
    public class SitemapBuildManager
    {
        private DateTime currentDateTime = DateTime.Now;
        private enum ChangeFrequency
        {
            always,
            hourly,
            daily,
            weekly,
            monthly,
            yearly,
            never
        }

        private static StringDictionary m_Sites;
        public Database Db
        {
            get
            {
                Database database = Factory.GetDatabase(SitemapManagerConfiguration.WorkingDatabase);
                return database;
            }
        }

        public SitemapBuildManager()
        {
            m_Sites = SitemapManagerConfiguration.GetSites();
            foreach (DictionaryEntry site in m_Sites)
            {
                BuildSiteMap(site.Key.ToString(), site.Value.ToString());
            }
            Log.Info(string.Format("Sitemap Manager: Sitemap rebuild initiated"), this);
        }

        private void BuildSiteMap(string sitename, string sitemapUrlNew)
        {
            Site site = Sitecore.Sites.SiteManager.GetSite(sitename);
            SiteContext siteContext = Factory.GetSite(sitename);
            string rootPath = siteContext.StartPath;

            List<Item> items = GetSitemapItems(rootPath);

            string fullPath = MainUtil.MapPath(string.Concat("/", sitemapUrlNew));

            var sitemapXmlDoc = XDocument.Load(new StringReader(this.BuildSitemapXML(items, site)), LoadOptions.None);

            sitemapXmlDoc.Save(fullPath, SaveOptions.None);

            Log.Info(string.Format("Sitemap Manager: Sitemap rebuilt for site \"{0}\"", sitename), this);
        }

        private string BuildSitemapXML(List<Item> items, Site site)
        {
            XmlDocument doc = new XmlDocument();

            XmlNode declarationNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(declarationNode);
            XmlNode urlsetNode = doc.CreateElement("urlset");
            XmlAttribute xmlnsAttr = doc.CreateAttribute("xmlns");
            xmlnsAttr.Value = SitemapManagerConfiguration.XmlnsTpl;
            urlsetNode.Attributes.Append(xmlnsAttr);

            doc.AppendChild(urlsetNode);

            foreach (Item itm in items)
            {
                doc = this.BuildSitemapItem(doc, itm, site);
            }

            return doc.OuterXml;
        }

        private XmlDocument BuildSitemapItem(XmlDocument doc, Item item, Site site)
        {
            //get the item's URL, then chop it up so path depth can be assessed
            string rawUrl = HtmlEncode(this.GetItemUrl(item, site));
            System.Uri urlUri = new System.Uri(rawUrl);
            string[] pathEntries = urlUri.AbsolutePath.ToString().Substring(1).Split('/');
            int depth = pathEntries.Length;

            //set things up for priority calc
            float priorityValue = 0.0F;
            int max = 10;
            int min = 1;

            //check the item's change frequency field for a user-set value
            if (!string.IsNullOrEmpty(item[SitemapConstants.STRING_FIELDPARAM_NAME_PRIORITY].ToString()))
            {
                float.TryParse(item[SitemapConstants.STRING_FIELDPARAM_NAME_PRIORITY].ToString(), out priorityValue);
            }
            else if (depth <= max)
            {
                priorityValue = ((((float)max + (float)min) - (float)depth) / 10);
            }

            /***************************************************************/

            DateTime itemLastModDate = item.Statistics.Updated;
            TimeSpan ts = currentDateTime - itemLastModDate;
            int daysSinceLastUpdate = ts.Days;

            string changeFrequency = ChangeFrequency.weekly.ToString();

            //Check the item's priority field for a user-set value
            if (!string.IsNullOrEmpty(item[SitemapConstants.STRING_FIELDPARAM_NAME_CHANGEFREQ].ToString()))
            {
                changeFrequency = item[SitemapConstants.STRING_FIELDPARAM_NAME_CHANGEFREQ].ToString();
            }
            else //Set the changefreq based on an inaccurate but passable check against lastmod date vs now
            {
                ChangeFrequency eChangeFrequency = ChangeFrequency.weekly;

                if (daysSinceLastUpdate <= 1)
                    eChangeFrequency = ChangeFrequency.daily;
                else if (daysSinceLastUpdate > 1 && daysSinceLastUpdate <= 7)
                    eChangeFrequency = ChangeFrequency.weekly;
                else if (daysSinceLastUpdate > 7 && daysSinceLastUpdate <= 30)
                    eChangeFrequency = ChangeFrequency.monthly;
                else if (daysSinceLastUpdate > 30 && daysSinceLastUpdate <= 365)
                    eChangeFrequency = ChangeFrequency.yearly;
                else if (daysSinceLastUpdate > 365)
                    eChangeFrequency = ChangeFrequency.never;

                changeFrequency = eChangeFrequency.ToString();
            }

            /***************************************************************/
            //Set last modification date
            
            string lastModDate = HtmlEncode(itemLastModDate.ToString("yyyy-MM-ddTHH:mm:sszzz"));

            /***************************************************************/

            XmlNode urlsetNode = doc.LastChild;

            XmlNode urlNode = doc.CreateElement("url");
            urlsetNode.AppendChild(urlNode);

            XmlNode locNode = doc.CreateElement("loc");
            urlNode.AppendChild(locNode);
            locNode.AppendChild(doc.CreateTextNode(rawUrl));
            
            XmlNode lastmodNode = doc.CreateElement("lastmod");
            urlNode.AppendChild(lastmodNode);
            lastmodNode.AppendChild(doc.CreateTextNode(lastModDate));

            XmlNode priorityNode = doc.CreateElement("priority");
            urlNode.AppendChild(priorityNode);
            priorityNode.AppendChild(doc.CreateTextNode(priorityValue.ToString("0.0")));

            XmlNode changeFreqNode = doc.CreateElement("changefreq");
            urlNode.AppendChild(changeFreqNode);
            changeFreqNode.AppendChild(doc.CreateTextNode(changeFrequency));

            /***************************************************************/

            return doc;
        }

        private string GetItemUrl(Item item, Site site)
        {
            Sitecore.Links.UrlOptions options = Sitecore.Links.UrlOptions.DefaultOptions;

            options.SiteResolving = Sitecore.Configuration.Settings.Rendering.SiteResolving;
            options.Site = SiteContext.GetSite(site.Name);
            options.AlwaysIncludeServerUrl = false;

            string url = Sitecore.Links.LinkManager.GetItemUrl(item, options);

            string serverUrl = SitemapManagerConfiguration.GetServerUrlBySite(site.Name);
            if (serverUrl.Contains(SitemapConstants.STRING_HTTP_PREFIX))
            {
                serverUrl = serverUrl.Substring(SitemapConstants.STRING_HTTP_PREFIX.Length);
            }

            StringBuilder sb = new StringBuilder();

            /***************************************************************/

            if (!string.IsNullOrEmpty(serverUrl))
            {
                if (url.Contains("://") && !url.Contains("http"))
                {
                    sb.Append(SitemapConstants.STRING_HTTP_PREFIX);
                    sb.Append(serverUrl);
                    if (url.IndexOf("/", 3) > 0)
                        sb.Append(url.Substring(url.IndexOf("/", 3)));
                }
                else
                {
                    sb.Append(SitemapConstants.STRING_HTTP_PREFIX);
                    sb.Append(serverUrl);
                    sb.Append(url);
                }
            }
            else if (!string.IsNullOrEmpty(site.Properties["hostname"]))
            {
                sb.Append(SitemapConstants.STRING_HTTP_PREFIX);
                sb.Append(site.Properties["hostname"]);
                sb.Append(url);
            }
            else
            {
                if (url.Contains("://") && !url.Contains("http"))
                {
                    sb.Append(SitemapConstants.STRING_HTTP_PREFIX);
                    sb.Append(url);
                }
                else
                {
                    sb.Append(Sitecore.Web.WebUtil.GetFullUrl(url));
                }
            }
            
            /***************************************************************/

            return sb.ToString();
        }

        private static string HtmlEncode(string text)
        {
            string result = HttpUtility.HtmlEncode(text);

            return result;
        }

        private List<Item> GetSitemapItems(string rootPath)
        {
            string disTpls = SitemapManagerConfiguration.EnabledTemplates;

            Database database = Factory.GetDatabase(SitemapManagerConfiguration.WorkingDatabase);

            Item contentRoot = database.Items[rootPath];
            Item[] descendants;

            Sitecore.Security.Accounts.User user = Sitecore.Security.Accounts.User.FromName(@"extranet\Anonymous", true);
            using (new Sitecore.Security.Accounts.UserSwitcher(user))
            {
                descendants = contentRoot.Axes.GetDescendants(); //get all the descendants of "/Home" for this site context
            }
            
            List<Item> sitemapItems = descendants.ToList();
            sitemapItems.Insert(0, contentRoot); //prepend "/Home" to the list so it's included

            List<string> enabledTemplates = this.BuildListFromString(disTpls, '|');

            //build a list of items for the sitemap based the following properties:
            //1) Item is included in the list of enabled templates defined by sitemap setting item
            //2) Item has a presentation layout
            //3) Item's individual setting for inclusion in the sitemap is enabled (standard value is true)
            //   This also implicitly deals into the stack only those items which inherit
            //   from the template "Sitemap Extension"
            var selected = from itm in sitemapItems
                           where itm.Template != null && enabledTemplates.Contains(itm.Template.ID.ToString()) 
                                                      && ItemHasLayout(itm)
                                                      && itm["Include in Sitemap"].Equals("1")
                           select itm;

            return selected.ToList();
        }

        private List<string> BuildListFromString(string str, char separator)
        {
            string[] enabledTemplates = str.Split(separator);
            var selected = from dtp in enabledTemplates
                           where !string.IsNullOrEmpty(dtp)
                           select dtp;

            List<string> result = selected.ToList();

            return result;
        }

        private bool ItemHasLayout(Item item)
        {
            return (item.Fields[FieldIDs.LayoutField] != null &&
                !string.IsNullOrEmpty(item.Fields[FieldIDs.LayoutField].GetValue(true)));
        }
    }
}
