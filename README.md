Sitecore XML Sitemap Generator
==================

This module generates an XML Sitemap file compliant with the schema defined by sitemaps.org and can also submit that sitemap to search engines defined in the module's settings. The sitemap generator and search engine submission are done on-demand via a simple two button sheer UI interface.

Much of the code for this project was derived from the "Sitemap XML" project provided by Sitecore's Shared Source, which can be found here: https://marketplace.sitecore.net/en/Modules/Sitemap_XML.aspx

Now, instead of being tied to publishing, the XML sitemap file is only generated on-demand, and submission to search engines is handled the same way. I’ve also added in some algorithmic approaches to determining change frequency and priority level nodes for URLs as well as an extension template which provides a way for users to manually define the following:

1) Checkbox to set whether the item is included in sitemap

2) Dropdown selector to manually set change frequency

3) Field to enter priority level along with a corresponding field validation rule.
