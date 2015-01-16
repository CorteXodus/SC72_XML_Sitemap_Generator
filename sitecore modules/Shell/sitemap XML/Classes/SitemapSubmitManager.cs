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
    public class SitemapSubmitManager
    {
        private static StringDictionary m_Sites;
        public Database Db
        {
            get
            {
                Database database = Factory.GetDatabase(SitemapManagerConfiguration.SettingsDatabase);
                return database;
            }
        }

        public SitemapSubmitManager()
        {
            m_Sites = SitemapManagerConfiguration.GetSites();
        }

        public bool SubmitSitemapToSearchenginesByHttp()
        {
            if (!SitemapManagerConfiguration.IsProductionEnvironment)
            {
                Log.Warn(string.Format("Sitemap Manager: Attempt to submit cancelled because configuration was not set for production"), this);
                return false;
            }

            bool result = false;

            Item sitemapConfigItem = Db.Items.GetItem(SitemapConstants.ITEM_ID_SITEMAPCONFIG);

            if (sitemapConfigItem != null)
            {
                string engines = sitemapConfigItem.Fields["Search engines"].Value;
                foreach (string id in engines.Split('|'))
                {
                    Item engine = Db.Items[id];
                    if (engine != null)
                    {
                        string engineHttpRequestString = engine.Fields["HttpRequestString"].Value;
                        
                        foreach (DictionaryEntry siteEntry in m_Sites)
                        {
                            string serverUrl = SitemapManagerConfiguration.GetServerUrlBySite(siteEntry.Key.ToString());
                            string sitemapFileName = siteEntry.Value.ToString();
                            this.SubmitEngine(engineHttpRequestString, sitemapFileName, serverUrl);
                        }

                    }
                }
                result = true;
            }
            return result;
        }
        private void SubmitEngine(string engine, string sitemapFileName, string serverUrl)
        {
            //Check if it is not localhost because search engines returns an error
            if (!serverUrl.Contains("http://localhost"))
            {
                string request = string.Concat( engine, string.Concat("http://", HtmlEncode(serverUrl)) + string.Concat("/", sitemapFileName) );

                System.Net.HttpWebRequest httpRequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(request);
                try
                {
                    System.Net.WebResponse webResponse = httpRequest.GetResponse();

                    System.Net.HttpWebResponse httpResponse = (System.Net.HttpWebResponse)webResponse;
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        Log.Error(string.Concat(string.Format("Sitemap Manager: Was unable to submit sitemap to \"{0}\":", engine),
                                                string.Format(" Received webresponse \"{0}\"", httpResponse.StatusCode.ToString())), this);
                    }
                    else
                    {
                        Log.Info(string.Format("Sitemap Manager: Submitted sitemap to \"{0}\"", engine), this);
                    }
                }
                catch
                {
                    Log.Warn(string.Format("Sitemap Manager: The search engine \"{0}\" has returned an error during the submit attempt ", request), this);
                }
            }
        }

        private static string HtmlEncode(string text)
        {
            string result = HttpUtility.HtmlEncode(text);

            return result;
        }
    }
}