﻿using System;
using System.Collections.Generic;
using System.Text;
using wojilu.Web.Context;
using wojilu.Apps.Content.Domain;
using wojilu.Web.Mvc;
using wojilu.Web.Controller.Content.Caching;
using wojilu.Common.AppBase.Interface;

namespace wojilu.Web.Controller.Content.Utils {

    public class clink {

        public static String toSection( int sectionId, MvcContext ctx ) {

            Link link = new Link( ctx );

            if (HtmlHelper.IsMakeHtml( ctx )) {
                return string.Format( "/html/list/{0}.html", sectionId );
            }
            else {
                return link.To( new SectionController().Show, sectionId );
            }
        }

        public static String toArchive( int sectionId, MvcContext ctx ) {

            Link link = new Link( ctx );

            if (HtmlHelper.IsMakeHtml( ctx )) {
                return string.Format( "/html/list/{0}_a.html", sectionId );
            }
            else {
                return link.To( new SectionController().Show, sectionId );
            }
        }

        public static String toPost( IAppData data, MvcContext ctx ) {

            if (HtmlHelper.IsMakeHtml( ctx )) {
                DateTime n = data.Created;
                return string.Format( "/html/{0}/{1}/{2}/{3}.html", n.Year, n.Month, n.Day, data.Id );
            }
            else {
                return alink.ToAppData( data );
            }
        }

        public static String toSidebar( MvcContext ctx ) {

            Link link = new Link( ctx );

            if (HtmlHelper.IsMakeHtml( ctx )) {
                return string.Format( "/html/sidebar/{0}.html", ctx.app.Id );
            }
            else {
                return link.To( new SidebarController().Index );
            }
        }


    }

}