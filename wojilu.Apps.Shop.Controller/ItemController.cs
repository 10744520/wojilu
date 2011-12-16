/*
 * Copyright (c) 2010, www.wojilu.com. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Text;

using wojilu.DI;
using wojilu.Web.Mvc;
using wojilu.Web.Mvc.Attr;
using wojilu.Apps.Shop.Domain;
using wojilu.Apps.Shop.Interface;
using wojilu.Apps.Shop.Service;
using wojilu.Web.Controller.Shop.Section;
using wojilu.Web.Context;
using wojilu.Web.Controller.Common;
using wojilu.Web.Controller.Shop.Utils;
using wojilu.Web.Controller.Shop.Caching;
using wojilu.Common.AppBase;
using wojilu.Members.Sites.Domain;
using wojilu.Common.Tags;
using wojilu.ORM;
using wojilu.Common.AppBase.Interface;

namespace wojilu.Web.Controller.Shop
{

    [App( typeof( ShopApp ) )]
    public class ItemController : ControllerBase {

        public IShopItemService postService { get; set; }
        public IShopItemImgService imgService { get; set; }
        public IShopSectionService sectionService { get; set; }

        public ItemController()
        {
            LayoutControllerType = typeof( Section.LayoutController );

            imgService = new ShopItemImgService();
            postService = new ShopItemService();
            sectionService = new ShopSectionService();
        }

        [CacheAction( typeof( ShopLayoutCache ) )]
        public override void Layout() {
        }

        public void Recent() {

            DataPage<ShopItem> list = postService.GetByApp( ctx.app.Id, 50 );
            bindPosts( list );

            Page.Title = ctx.app.Name + "������Ʒ";
        }

        private void bindPosts( DataPage<ShopItem> posts ) {
            IBlock block = getBlock( "list" );
            foreach (ShopItem post in posts.Results) {

                if (post.PageSection == null) continue;
                //if (post.PageSection.SectionType == typeof( TextController ).FullName) continue;

                BinderUtils.bindListItem( block, post, ctx );
                block.Next();
            }
            set( "page", posts.PageBar );
        }



        public void Show( int id ) {

            ShopItem post = postService.GetById( id, ctx.owner.Id );

            if (post == null) {
                echo( lang( "exDataNotFound" ) );
                return;
            }
            else if (post.PageSection == null) {
                echo( lang( "exDataNotFound" ) + ":PageSection is null" );
                return;
            }

            // redirect
            if (strUtil.HasText( post.RedirectUrl )) {
                redirectUrl( post.RedirectUrl );
                return;
            }

            //----------------------------------------------------------------------------------------------------

            // 0) page meta
            bindMetaInfo( post );

            // 1) location
            String location = string.Format( "<a href='{0}'>{1}</a>", Link.To( new ShopController().Index ),
    ((AppContext)ctx.app).Menu.Name );
            if (post.CategoryId>0){
            location = location +
                       string.Format(" &gt; {0}", Location.GetItem(ctx, post));
            }else{
                location = location + string.Format(" &gt; <a href='{0}'>{1}</a> &gt; {2}", to(new SectionController().Show, post.PageSection.Id), post.PageSection.Title, post.Title);
            }
            set("location", location);

            // 2) detail
            set( "detailContent", loadHtml( post.PageSection.SectionType, "Show", post.Id ) );

            // 3) comment
            loadComment( post );

            // 4) related posts
            loadRelatedPosts( post );

            // 5) prev/next
            bindPrevNext( post );

            // 6) tag
            String tag = post.Tag.List.Count > 0 ? post.Tag.HtmlString : "";
            set( "sku.Tag", tag );

            // 7) digg
            set( "lnkDiggUp", to( DiggUp, post.Id ) );
            set( "lnkDiggDown", to( DiggDown, post.Id ) );

            // 8) link
            //set( "shareLink", WebUtils.getShareLink( ctx, post, "��Ʒ", to( new ItemController().Show, post.Id ) ) );
            String postUrl = strUtil.Join( ctx.url.SiteAndAppPath, alink.ToAppData( post ) );
            set("sku.Url", postUrl);
            bind("sku", post);

        }

        private void bindMetaInfo( ShopItem post ) {

            WebUtils.pageTitle( this, post.Title, ctx.app.Name );

            if (strUtil.HasText( post.MetaKeywords ))
                this.Page.Keywords = post.MetaKeywords;
            else
                this.Page.Keywords = post.Tag.TextString;

            if (strUtil.HasText( post.MetaDescription ))
                this.Page.Description = post.MetaDescription;
            else
                this.Page.Description = post.Summary;
        }


        [HttpPost, DbTransaction]
        public void DiggUp( int id ) {

            if (ctx.viewer.IsLogin == false) {
                echoText( "�����¼���ܲ��������ȵ�¼" );
                return;
            }

            ShopItem post = postService.GetById( id, ctx.owner.Id );

            if (post == null) {
                echoText( lang( "exDataNotFound" ) );
                return;
            }

            ShopDigg digg = ShopDigg.find( "UserId=" + ctx.viewer.Id + " and ItemId=" + post.Id ).first();
            if (digg != null) {
                echoText( "���Ѿ������������ظ�" );
                return;
            }

            ShopDigg d = new ShopDigg();
            d.UserId = ctx.viewer.Id;
            d.ItemId = post.Id;
            d.TypeId = 0;
            d.Ip = ctx.Ip;
            d.insert();

            post.DiggUp++;
            post.update( "DiggUp" );

            echoAjaxOk();

        }

        [HttpPost, DbTransaction]
        public void DiggDown( int id ) {

            if (ctx.viewer.IsLogin == false) {
                echoText( "�����¼���ܲ��������ȵ�¼" );
                return;
            }

            ShopItem post = postService.GetById( id, ctx.owner.Id );

            if (post == null) {
                echoText( lang( "exDataNotFound" ) );
                return;
            }

            ShopDigg digg = ShopDigg.find( "UserId=" + ctx.viewer.Id + " and ItemId=" + post.Id ).first();
            if (digg != null) {
                echoText( "���Ѿ������������ظ�" );
                return;
            }

            ShopDigg d = new ShopDigg();
            d.UserId = ctx.viewer.Id;
            d.ItemId = post.Id;
            d.TypeId = 1;
            d.Ip = ctx.Ip;
            d.insert();

            post.DiggDown++;
            post.update( "DiggDown" );

            echoAjaxOk();

        }

        private void bindPrevNext( ShopItem post ) {

            ShopItem prev = postService.GetPrevPost( post );
            ShopItem next = postService.GetNextPost( post );

            String lnkPrev = prev == null ? "(û��)" : string.Format( "<a href=\"{0}\">{1}</a>", alink.ToAppData( prev ), prev.Title );
            String lnkNext = next == null ? "(û��)" : string.Format( "<a href=\"{0}\">{1}</a>", alink.ToAppData( next ), next.Title );

            set( "prevPost", lnkPrev );
            set( "nextPost", lnkNext );
        }

        private void loadRelatedPosts( ShopItem post ) {

            List<DataTagShip> list = postService.GetRelatedDatas( post );
            IBlock block = getBlock( "related" );

            foreach (DataTagShip dt in list) {

                EntityInfo ei = Entity.GetInfo( dt.TypeFullName );
                if (ei == null) continue;

                IAppData obj = ndb.findById( ei.Type, dt.DataId ) as IAppData;
                if (obj == null) continue;

                block.Set( "p.Title", obj.Title );
                block.Set( "p.Link", alink.ToAppData( obj ) );
                block.Set( "p.Created", obj.Created );

                block.Next();

            }


        }

        private void loadComment( ShopItem post ) {

            ShopApp app = ctx.app.obj as ShopApp;

            if (post.CommentCondition == CommentCondition.Close || app.GetSettingsObj().AllowComment == 0) {
                set( "commentSection", "" );
                return;
            }

            ctx.SetItem( "createAction", to( new ShopCommentController().Create, post.Id ) );
            ctx.SetItem( "commentTarget", post );
            load("commentSection", new ShopCommentController().ListAndForm);
        }



    }

}
