﻿using System.Collections.Generic;
using System.Text;
using SiteServer.Utils;
using SiteServer.CMS.Core;
using SiteServer.CMS.Model;
using SiteServer.CMS.Model.Attributes;
using SiteServer.CMS.StlParser.Cache;
using SiteServer.CMS.StlParser.Model;
using SiteServer.CMS.StlParser.Parsers;
using SiteServer.CMS.StlParser.Utility;
using SiteServer.Utils.Enumerations;

namespace SiteServer.CMS.StlParser.StlElement
{
    [StlClass(Usage = "播放视频", Description = "通过 stl:player 标签在模板中播放视频")]
    public class StlPlayer
	{
        private StlPlayer() { }
		public const string ElementName = "stl:player";

		private static readonly Attr ChannelIndex = new Attr("channelIndex", "栏目索引");
		private static readonly Attr ChannelName = new Attr("channelName", "栏目名称");
		private static readonly Attr Parent = new Attr("parent", "显示父栏目");
		private static readonly Attr UpLevel = new Attr("upLevel", "上级栏目的级别");
        private static readonly Attr TopLevel = new Attr("topLevel", "从首页向下的栏目级别");
        private static readonly Attr Type = new Attr("type", "指定存储媒体的字段");
		private static readonly Attr PlayUrl = new Attr("playUrl", "视频地址");
        private static readonly Attr ImageUrl = new Attr("imageUrl", "图片地址");
        private static readonly Attr PlayBy = new Attr("playBy", "指定播放器");
		private static readonly Attr Width = new Attr("width", "宽度");
		private static readonly Attr Height = new Attr("height", "高度");
        private static readonly Attr IsAutoPlay = new Attr("isAutoPlay", "是否自动播放");

        public const string PlayByBrPlayer = "BRPlayer";
        public const string PlayByFlowPlayer = "FlowPlayer";
        public const string PlayByJwPlayer = "JWPlayer";

        public static SortedList<string, string> PlayByList => new SortedList<string, string>
        {
            {PlayByBrPlayer, "BRPlayer"},
            {PlayByFlowPlayer, "FlowPlayer"},
            {PlayByJwPlayer, "JWPlayer"}
        };

        public static string Parse(PageInfo pageInfo, ContextInfo contextInfo)
		{
		    var isGetPicUrlFromAttribute = false;
            var channelIndex = string.Empty;
            var channelName = string.Empty;
            var upLevel = 0;
            var topLevel = -1;
            var type = BackgroundContentAttribute.VideoUrl;
            var playUrl = string.Empty;
            var imageUrl = string.Empty;
            var playBy = string.Empty;
            var width = 450;
            var height = 350;
            var isAutoPlay = true;

            foreach (var name in contextInfo.Attributes.Keys)
            {
                var value = contextInfo.Attributes[name];

                if (StringUtils.EqualsIgnoreCase(name, ChannelIndex.Name))
                {
                    channelIndex = StlEntityParser.ReplaceStlEntitiesForAttributeValue(value, pageInfo, contextInfo);
                    if (!string.IsNullOrEmpty(channelIndex))
                    {
                        isGetPicUrlFromAttribute = true;
                    }
                }
                else if (StringUtils.EqualsIgnoreCase(name, ChannelName.Name))
                {
                    channelName = StlEntityParser.ReplaceStlEntitiesForAttributeValue(value, pageInfo, contextInfo);
                    if (!string.IsNullOrEmpty(channelName))
                    {
                        isGetPicUrlFromAttribute = true;
                    }
                }
                else if (StringUtils.EqualsIgnoreCase(name, Parent.Name))
                {
                    if (TranslateUtils.ToBool(value))
                    {
                        upLevel = 1;
                        isGetPicUrlFromAttribute = true;
                    }
                }
                else if (StringUtils.EqualsIgnoreCase(name, UpLevel.Name))
                {
                    upLevel = TranslateUtils.ToInt(value);
                    if (upLevel > 0)
                    {
                        isGetPicUrlFromAttribute = true;
                    }
                }
                else if (StringUtils.EqualsIgnoreCase(name, TopLevel.Name))
                {
                    topLevel = TranslateUtils.ToInt(value);
                    if (topLevel >= 0)
                    {
                        isGetPicUrlFromAttribute = true;
                    }
                }
                else if (StringUtils.EqualsIgnoreCase(name, Type.Name))
                {
                    type = value;
                }
                else if (StringUtils.EqualsIgnoreCase(name, PlayUrl.Name) || StringUtils.EqualsIgnoreCase(name, "src"))
                {
                    playUrl = value;
                }
                else if (StringUtils.EqualsIgnoreCase(name, ImageUrl.Name))
                {
                    imageUrl = value;
                }
                else if (StringUtils.EqualsIgnoreCase(name, PlayBy.Name))
                {
                    playBy = value;
                }
                else if (StringUtils.EqualsIgnoreCase(name, Width.Name))
                {
                    width = TranslateUtils.ToInt(value, width);
                }
                else if (StringUtils.EqualsIgnoreCase(name, Height.Name))
                {
                    height = TranslateUtils.ToInt(value, height);
                }
                else if (StringUtils.EqualsIgnoreCase(name, IsAutoPlay.Name) || StringUtils.EqualsIgnoreCase(name, "play"))
                {
                    isAutoPlay = TranslateUtils.ToBool(value, true);
                }
            }

            return ParseImpl(pageInfo, contextInfo, isGetPicUrlFromAttribute, channelIndex, channelName, upLevel, topLevel, playUrl, imageUrl, playBy, width, height, type, isAutoPlay);
		}

        private static string ParseImpl(PageInfo pageInfo, ContextInfo contextInfo, bool isGetPicUrlFromAttribute, string channelIndex, string channelName, int upLevel, int topLevel, string playUrl, string imageUrl, string playBy, int width, int height, string type, bool isAutoPlay)
        {
            var parsedContent = string.Empty;

            var contentId = 0;
            //判断是否图片地址由标签属性获得
            if (!isGetPicUrlFromAttribute)
            {
                contentId = contextInfo.ContentId;
            }

            if (string.IsNullOrEmpty(playUrl))
            {
                if (contentId != 0)//获取内容视频
                {
                    if (contextInfo.ContentInfo == null)
                    {
                        //playUrl = DataProvider.ContentDao.GetValue(pageInfo.SiteInfo.AuxiliaryTableForContent, contentId, type);
                        playUrl = Content.GetValue(pageInfo.SiteInfo.TableName, contentId, type);
                        if (string.IsNullOrEmpty(playUrl))
                        {
                            if (!StringUtils.EqualsIgnoreCase(type, BackgroundContentAttribute.VideoUrl))
                            {
                                //playUrl = DataProvider.ContentDao.GetValue(pageInfo.SiteInfo.AuxiliaryTableForContent, contentId, BackgroundContentAttribute.VideoUrl);
                                playUrl = Content.GetValue(pageInfo.SiteInfo.TableName, contentId, BackgroundContentAttribute.VideoUrl);
                            }
                        }
                        if (string.IsNullOrEmpty(playUrl))
                        {
                            if (!StringUtils.EqualsIgnoreCase(type, BackgroundContentAttribute.FileUrl))
                            {
                                //playUrl = DataProvider.ContentDao.GetValue(pageInfo.SiteInfo.AuxiliaryTableForContent, contentId, BackgroundContentAttribute.FileUrl);
                                playUrl = Content.GetValue(pageInfo.SiteInfo.TableName, contentId, BackgroundContentAttribute.FileUrl);
                            }
                        }
                    }
                    else
                    {
                        playUrl = contextInfo.ContentInfo.GetString(type);
                        if (string.IsNullOrEmpty(playUrl))
                        {
                            playUrl = contextInfo.ContentInfo.GetString(BackgroundContentAttribute.VideoUrl);
                        }
                        if (string.IsNullOrEmpty(playUrl))
                        {
                            playUrl = contextInfo.ContentInfo.GetString(BackgroundContentAttribute.FileUrl);
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(imageUrl))
            {
                if (contentId != 0)
                {
                    //imageUrl = contextInfo.ContentInfo == null ? DataProvider.ContentDao.GetValue(pageInfo.SiteInfo.AuxiliaryTableForContent, contentId, BackgroundContentAttribute.ImageUrl) : contextInfo.ContentInfo.GetString(BackgroundContentAttribute.ImageUrl);
                    imageUrl = contextInfo.ContentInfo == null ? Content.GetValue(pageInfo.SiteInfo.TableName, contentId, BackgroundContentAttribute.ImageUrl) : contextInfo.ContentInfo.GetString(BackgroundContentAttribute.ImageUrl);
                }
            }
            if (string.IsNullOrEmpty(imageUrl))
            {
                var channelId = StlDataUtility.GetChannelIdByLevel(pageInfo.SiteId, contextInfo.ChannelId, upLevel, topLevel);
                channelId = StlDataUtility.GetChannelIdByChannelIdOrChannelIndexOrChannelName(pageInfo.SiteId, channelId, channelIndex, channelName);
                var channel = ChannelManager.GetChannelInfo(pageInfo.SiteId, channelId);
                imageUrl = channel.ImageUrl;
            }

            if (!string.IsNullOrEmpty(playUrl))
            {
                var extension = PathUtils.GetExtension(playUrl);
                if (EFileSystemTypeUtils.IsFlash(extension))
                {
                    parsedContent = StlFlash.Parse(pageInfo, contextInfo);
                }
                else if (EFileSystemTypeUtils.IsImage(extension))
                {
                    parsedContent = StlImage.Parse(pageInfo, contextInfo);
                }
                else
                {
                    var uniqueId = pageInfo.UniqueId;
                    playUrl = PageUtility.ParseNavigationUrl(pageInfo.SiteInfo, playUrl, pageInfo.IsLocal);
                    imageUrl = PageUtility.ParseNavigationUrl(pageInfo.SiteInfo, imageUrl, pageInfo.IsLocal);

                    var fileType = EFileSystemTypeUtils.GetEnumType(extension);
                    if (fileType == EFileSystemType.Avi)
                    {
                        parsedContent = $@"
<object id=""palyer_{uniqueId}"" width=""{width}"" height=""{height}"" border=""0"" classid=""clsid:CFCDAA03-8BE4-11cf-B84B-0020AFBBCCFA"">
<param name=""ShowDisplay"" value=""0"">
<param name=""ShowControls"" value=""1"">
<param name=""AutoStart"" value=""{(isAutoPlay ? "1" : "0")}"">
<param name=""AutoRewind"" value=""0"">
<param name=""PlayCount"" value=""0"">
<param name=""Appearance"" value=""0"">
<param name=""BorderStyle"" value=""0"">
<param name=""MovieWindowHeight"" value=""240"">
<param name=""MovieWindowWidth"" value=""320"">
<param name=""FileName"" value=""{playUrl}"">
<embed width=""{width}"" height=""{height}"" border=""0"" showdisplay=""0"" showcontrols=""1"" autostart=""{(isAutoPlay
                            ? "1"
                            : "0")}"" autorewind=""0"" playcount=""0"" moviewindowheight=""240"" moviewindowwidth=""320"" filename=""{playUrl}"" src=""{playUrl}"">
</embed>
</object>
";
                    }
                    else if (fileType == EFileSystemType.Mpg)
                    {
                        parsedContent = $@"
<object classid=""clsid:05589FA1-C356-11CE-BF01-00AA0055595A"" id=""palyer_{uniqueId}"" width=""{width}"" height=""{height}"">
<param name=""Appearance"" value=""0"">
<param name=""AutoStart"" value=""{(isAutoPlay ? "true" : "false")}"">
<param name=""AllowChangeDisplayMode"" value=""-1"">
<param name=""AllowHideDisplay"" value=""0"">
<param name=""AllowHideControls"" value=""-1"">
<param name=""AutoRewind"" value=""-1"">
<param name=""Balance"" value=""0"">
<param name=""CurrentPosition"" value=""0"">
<param name=""DisplayBackColor"" value=""0"">
<param name=""DisplayForeColor"" value=""16777215"">
<param name=""DisplayMode"" value=""0"">
<param name=""Enabled"" value=""-1"">
<param name=""EnableContextMenu"" value=""-1"">
<param name=""EnablePositionControls"" value=""-1"">
<param name=""EnableSelectionControls"" value=""0"">
<param name=""EnableTracker"" value=""-1"">
<param name=""Filename"" value=""{playUrl}"" valuetype=""ref"">
<param name=""FullScreenMode"" value=""0"">
<param name=""MovieWindowSize"" value=""0"">
<param name=""PlayCount"" value=""1"">
<param name=""Rate"" value=""1"">
<param name=""SelectionStart"" value=""-1"">
<param name=""SelectionEnd"" value=""-1"">
<param name=""ShowControls"" value=""-1"">
<param name=""ShowDisplay"" value=""-1"">
<param name=""ShowPositionControls"" value=""0"">
<param name=""ShowTracker"" value=""-1"">
<param name=""Volume"" value=""-480"">
</object>
";
                    }
                    else if (fileType == EFileSystemType.Mpg)
                    {
                        parsedContent = $@"
<OBJECT id=""palyer_{uniqueId}"" classid=""clsid:CFCDAA03-8BE4-11cf-B84B-0020AFBBCCFA"" width=""{width}"" height=""{height}"">
<param name=""_ExtentX"" value=""6350"">
<param name=""_ExtentY"" value=""4763"">
<param name=""AUTOSTART"" value=""{(isAutoPlay ? "true" : "false")}"">
<param name=""SHUFFLE"" value=""0"">
<param name=""PREFETCH"" value=""0"">
<param name=""NOLABELS"" value=""-1"">
<param name=""SRC"" value=""{playUrl}"">
<param name=""CONTROLS"" value=""ImageWindow"">
<param name=""CONSOLE"" value=""console1"">
<param name=""LOOP"" value=""0"">
<param name=""NUMLOOP"" value=""0"">
<param name=""CENTER"" value=""0"">
<param name=""MAINTAINASPECT"" value=""0"">
<param name=""BACKGROUNDCOLOR"" value=""#000000"">
<embed src=""{playUrl}"" type=""audio/x-pn-realaudio-plugin"" console=""Console1"" controls=""ImageWindow"" width=""{width}"" height=""{height}"" autostart=""{(isAutoPlay
                            ? "true"
                            : "false")}""></OBJECT>
";
                    }
                    else if (fileType == EFileSystemType.Rm)
                    {
                        parsedContent = $@"
<OBJECT id=""palyer_{uniqueId}"" CLASSID=""clsid:CFCDAA03-8BE4-11cf-B84B-0020AFBBCCFA"" WIDTH=""{width}"" HEIGHT=""{height}"">
<param name=""_ExtentX"" value=""9313"">
<param name=""_ExtentY"" value=""7620"">
<param name=""AUTOSTART"" value=""{(isAutoPlay ? "true" : "false")}"">
<param name=""SHUFFLE"" value=""0"">
<param name=""PREFETCH"" value=""0"">
<param name=""NOLABELS"" value=""0"">
<param name=""SRC"" value=""{playUrl}"">
<param name=""CONTROLS"" value=""ImageWindow"">
<param name=""CONSOLE"" value=""Clip1"">
<param name=""LOOP"" value=""0"">
<param name=""NUMLOOP"" value=""0"">
<param name=""CENTER"" value=""0"">
<param name=""MAINTAINASPECT"" value=""0"">
<param name=""BACKGROUNDCOLOR"" value=""#000000"">
<embed SRC type=""audio/x-pn-realaudio-plugin"" CONSOLE=""Clip1"" CONTROLS=""ImageWindow"" WIDTH=""{width}"" HEIGHT=""{height}"" AUTOSTART=""{(isAutoPlay
                            ? "true"
                            : "false")}"">
</OBJECT>
";
                    }
                    else if (fileType == EFileSystemType.Wmv)
                    {
                        parsedContent = $@"
<object id=""palyer_{uniqueId}"" WIDTH=""{width}"" HEIGHT=""{height}"" classid=""CLSID:22d6f312-b0f6-11d0-94ab-0080c74c7e95"" codebase=""http://activex.microsoft.com/activex/controls/mplayer/en/nsmp2inf.cab#Version=6,4,5,715"" standby=""Loading Microsoft Windows Media Player components..."" type=""application/x-oleobject"" align=""right"" hspace=""5"">
<param name=""AutoRewind"" value=""1"">
<param name=""ShowControls"" value=""1"">
<param name=""ShowPositionControls"" value=""0"">
<param name=""ShowAudioControls"" value=""1"">
<param name=""ShowTracker"" value=""0"">
<param name=""ShowDisplay"" value=""0"">
<param name=""ShowStatusBar"" value=""0"">
<param name=""ShowGotoBar"" value=""0"">
<param name=""ShowCaptioning"" value=""0"">
<param name=""AutoStart"" value=""{(isAutoPlay ? "1" : "0")}"">
<param name=""FileName"" value=""{playUrl}"">
<param name=""Volume"" value=""-2500"">
<param name=""AnimationAtStart"" value=""0"">
<param name=""TransparentAtStart"" value=""0"">
<param name=""AllowChangeDisplaySize"" value=""0"">
<param name=""AllowScan"" value=""0"">
<param name=""EnableContextMenu"" value=""0"">
<param name=""ClickToPlay"" value=""0"">
</object>
";
                    }
                    else if (fileType == EFileSystemType.Wma)
                    {
                        parsedContent = $@"
<object classid=""clsid:22D6F312-B0F6-11D0-94AB-0080C74C7E95"" id=""palyer_{uniqueId}"">
<param name=""Filename"" value=""{playUrl}"">
<param name=""PlayCount"" value=""1"">
<param name=""AutoStart"" value=""{(isAutoPlay ? "1" : "0")}"">
<param name=""ClickToPlay"" value=""1"">
<param name=""DisplaySize"" value=""0"">
<param name=""EnableFullScreen Controls"" value=""1"">
<param name=""ShowAudio Controls"" value=""1"">
<param name=""EnableContext Menu"" value=""1"">
<param name=""ShowDisplay"" value=""1"">
</object>
";
                    }
                    else if (fileType == EFileSystemType.Rm || fileType == EFileSystemType.Rmb || fileType == EFileSystemType.Rmvb)
                    {
                        if (!contextInfo.Attributes.ContainsKey("ShowDisplay"))
                        {
                            contextInfo.Attributes["ShowDisplay"] = "0";
                        }
                        if (!contextInfo.Attributes.ContainsKey("ShowControls"))
                        {
                            contextInfo.Attributes["ShowControls"] = "1";
                        }
                        contextInfo.Attributes["AutoStart"] = isAutoPlay ? "1" : "0";
                        if (!contextInfo.Attributes.ContainsKey("AutoRewind"))
                        {
                            contextInfo.Attributes["AutoRewind"] = "0";
                        }
                        if (!contextInfo.Attributes.ContainsKey("PlayCount"))
                        {
                            contextInfo.Attributes["PlayCount"] = "0";
                        }
                        if (!contextInfo.Attributes.ContainsKey("Appearance"))
                        {
                            contextInfo.Attributes["Appearance"] = "0";
                        }
                        if (!contextInfo.Attributes.ContainsKey("BorderStyle"))
                        {
                            contextInfo.Attributes["BorderStyle"] = "0";
                        }
                        if (!contextInfo.Attributes.ContainsKey("Controls"))
                        {
                            contextInfo.Attributes["ImageWindow"] = "0";
                        }
                        contextInfo.Attributes["moviewindowheight"] = height.ToString();
                        contextInfo.Attributes["moviewindowwidth"] = width.ToString();
                        contextInfo.Attributes["filename"] = playUrl;
                        contextInfo.Attributes["src"] = playUrl;

                        var paramBuilder = new StringBuilder();
                        var embedBuilder = new StringBuilder();
                        foreach (string key in contextInfo.Attributes.Keys)
                        {
                            paramBuilder.Append($@"<param name=""{key}"" value=""{contextInfo.Attributes[key]}"">").Append(StringUtils.Constants.ReturnAndNewline);
                            embedBuilder.Append($@" {key}=""{contextInfo.Attributes[key]}""");
                        }

                        parsedContent = $@"
<object id=""video_{uniqueId}"" width=""{width}"" height=""{height}"" border=""0"" classid=""clsid:CFCDAA03-8BE4-11cf-B84B-0020AFBBCCFA"">
{paramBuilder}
<embed{embedBuilder}>
</embed>
</object>
";
                    }
                    else
                    {
                        if (StringUtils.EqualsIgnoreCase(playBy, PlayByJwPlayer))
                        {
                            pageInfo.AddPageBodyCodeIfNotExists(PageInfo.Const.JsAcJwPlayer6);
                            var ajaxElementId = StlParserUtility.GetAjaxDivId(pageInfo.UniqueId);
                            parsedContent = $@"
<div id='{ajaxElementId}'></div>
<script type='text/javascript'>
	jwplayer('{ajaxElementId}').setup({{
        autostart: {isAutoPlay.ToString().ToLower()},
		file: ""{playUrl}"",
		width: ""{width}"",
		height: ""{height}"",
		image: ""{imageUrl}""
	}});
</script>
";
                        }
                        else
                        {
                            var ajaxElementId = StlParserUtility.GetAjaxDivId(pageInfo.UniqueId);
                            pageInfo.AddPageBodyCodeIfNotExists(PageInfo.Const.JsAcFlowPlayer);

                            var swfUrl = SiteFilesAssets.GetUrl(pageInfo.ApiUrl, SiteFilesAssets.FlowPlayer.Swf);
                            parsedContent = $@"
<a href=""{playUrl}"" style=""display:block;width:{width}px;height:{height}px;"" id=""player_{ajaxElementId}""></a>
<script language=""javascript"">
    flowplayer(""player_{ajaxElementId}"", ""{swfUrl}"", {{
        clip:  {{
            autoPlay: {isAutoPlay.ToString().ToLower()}
        }}
    }});
</script>
";
                        }
                    }
                }
            }

            return parsedContent;
        }
	}
}
