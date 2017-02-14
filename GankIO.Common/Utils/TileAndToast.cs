﻿using GankIO.Models;
using GankIO.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace GankIO.Common
{
    public class TileAndToast
    {
        public static TileSetting Setting = SettingsService.Instance.TileSetting;
        public static async Task Show(bool force = false)
        {
            //两小时以内不重复更新。
            if (!force && Setting.LastUpdateTime > DateTime.Now.AddHours(-2)) return;

            //每日更新内容显示四小时
            if (Setting.ShowDayResult &&
                Setting.LastDayResultTime < DateTime.Now.AddHours(-4))
            {
                var res = await GankService.GetDayResult(DateTime.Today, false);
                if (res.all.Any())
                {
                    UpdateTileAndShowToastByResults(res);
                    return;
                }                       
            }
            await UpdateTileByPhotos();

            Setting.LastUpdateTime = DateTime.Now;

        }
        public static async Task UpdateTileByPhotos(IEnumerable<福利> fulis = null)
        {
            try
            {
                fulis = fulis ?? await GankService.GetRandomCategoryItemsAsync<福利>(5);
                var updater = getTileUpdater(fulis.Count() >= 5);
                foreach (var f in fulis.Sample(5))
                {
                    string xml;
                    xml = getFuliTileXML(f);
                    var doc = loadXml(xml);
                    updater.Update(new TileNotification(doc));
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public static void UpdateTileAndShowToastByResults(Models.DayResult res)
        {
            var items = res.all.Shuffle();

            try
            {
                if (Setting.NeedToNofity &&
                    Setting.LastDayResultTime < DateTime.Today)
                {
                    //每日只通知一次
                    ShowToast(res.福利.FirstOrDefault());
                }                

                var updater = getTileUpdater();
                foreach (var n in items.Take(5))
                {
                    string xml;
                    if (n is 福利)
                    {
                        xml = getFuliTileXML((福利)n);
                    }
                    else
                    {
                        var item2 = items.Skip(5).Where(i => !(i is 福利)).Sample(1).FirstOrDefault();
                        xml = getItemXML(n, item2);
                    }
                    var doc = loadXml(xml);
                    updater.Update(new TileNotification(doc));
                }

                Setting.LastDayResultTime = DateTime.Now;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }


        private static void ShowToast(福利 fuli)
        {
            var xml = getToastXML(fuli);
            var doc = loadXml(xml);
            ToastNotificationManager.CreateToastNotifier()
                .Show(new ToastNotification(doc));
        }
        private static TileUpdater getTileUpdater(bool clear=true)
        {
            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            updater.EnableNotificationQueueForWide310x150(true);
            updater.EnableNotificationQueueForSquare150x150(true);
            updater.EnableNotificationQueueForSquare310x310(true);
            updater.EnableNotificationQueue(true);
            if(clear) updater.Clear();
            return updater;
        }
        private static XmlDocument loadXml(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(WebUtility.HtmlDecode(xml), new XmlLoadSettings
            {
                ProhibitDtd = false,
                ValidateOnParse = false,
                ElementContentWhiteSpace = false,
                ResolveExternals = false
            });
            return doc;
        }

        #region templates

        private static string getToastXML(福利 fuli)
        {
            var xml = $@"
<toast launch='0'>
  <visual>
    <binding template='ToastGeneric'>      
      <text>{fuli?.publishedAt:yyyy-MM-dd} 干货</text>
      <text>点击查看今日更新...</text>
      <image src='{fuli?.url}'/>
    </binding>
  </visual>
</toast>";
            return xml;
        }


        private static string getFuliTileXML(福利 f)
        {

            return $@"<tile>
  <visual branding='none'>
    <binding template='TileSmall'>
      <image placement='peek' src='{f.url}' />
      <text hint-style='captionSubtle' ></text>
      <text hint-style='caption' >{f.publishedAt:MM-dd}</text>
    </binding>

    <binding template='TileMedium'>
      <image placement = 'peek' src='{f.url}' />
      <text hint-style='captionSubtle' >福利</text>

      <text hint-style='caption' hint-wrap='true'>{f.desc}</text>
      <text hint-style='captionSubtle' hint-align='right'>{f.who}</text>
      <text hint-style='captionSubtle' hint-align='right'>{f.publishedAt:yyyy-MM-dd}</text>
    </binding>

    <binding template='TileWide'>
      <image placement = 'background' src='{f.url}' />
    </binding>

    <binding template='TileLarge'>
      <image alt ='{f.desc}' placement ='background'  src='{f.url}'/>
    </binding>

  </visual>
</tile>";
        }


        private static string getItemXML(all item, all item2)
        {
            //if (item.images == null || item.images.Length == 0)
            {

                return $@"<tile>
  <visual branding='name'>
    <binding template='TileSmall' branding='none'>
      <text hint-style='caption' ></text>
      <text hint-style='caption' >{item.publishedAt:MMdd}</text>
    </binding>

    <binding template='TileMedium'>
      <text hint-style='captionSubtle'>{item.type}</text>
      <text hint-style='caption' hint-wrap='true' hint-maxLines='2'>{item.desc}</text>
      <text hint-style='captionSubtle' hint-align='right' hint-maxLines='1'>{item.publishedAt:yyyy-MM-dd}</text>
    </binding>

    <binding template='TileWide'>
      <text hint-style='captionSubtle'>{item.type}</text>
      <text hint-style='caption' hint-wrap='true' hint-maxLines='2'>{item.desc}</text>
      <text hint-style='captionSubtle' hint-align='right' hint-maxLines='1'>{item.who} @ {item.publishedAt:yyyy-MM-dd}</text>
    </binding>

    <binding template='TileLarge'>
      <group>
        <subgroup>
          <text hint-style='titleSubtle'>{item.type}</text>
          <text hint-style='caption' hint-wrap='true' hint-maxLines='3'>{item.desc}</text>
        </subgroup>
      </group>
      <group>
        <subgroup>
          <text hint-style='captionSubtle'>{item2?.type}</text>
          <text hint-style='caption' hint-wrap='true' hint-maxLines='3'>{item2?.desc}</text>
          <text hint-style='captionSubtle' hint-align='right' hint-maxLines='1'>{item.publishedAt:yyyy-MM-dd}</text>
        </subgroup>
      </group>
    </binding>

  </visual>
</tile>
";
            }
            //else  //有网络图片经常会显示异常。
            {
                var bakImg = item.images.Length > 1 ? item.images[1] : item.images[0];
                return $@"
<tile>
  <visual branding='nameAndLogo'>
    <binding template='TileSmall'>
      <text hint-style='caption' >{item.publishedAt:MMdd}</text>
    </binding>

    <binding template='TileMedium'>
      <image src='{item.images[0]}'  placement='peek'/>
      <text hint-style='captionSubtle'>{item.type}</text>
      <text hint-style='caption' hint-wrap='true' hint-maxLines='2'>{item.desc}</text>
      <text hint-style='captionSubtle' hint-align='right' hint-maxLines='1'>{item.publishedAt:yyyy-MM-dd}</text>
    </binding>

    <binding template='TileWide'>
      <group>
        <subgroup hint-weight='33' hint-textStacking='center'>
          <image src='{item.images[0]}'  hint-align='center' />
        </subgroup>
        <subgroup>
          <text hint-style='captionSubtle'>{item.type}</text>
          <text hint-style='caption' hint-wrap='true' hint-maxLines='2'>{item.desc}</text>
          <text hint-style='captionSubtle' hint-align='right' hint-maxLines='1'>{item.who} @ {item.publishedAt:yyyy-MM-dd}</text>
        </subgroup>
      </group>
    </binding>

    <binding template='TileLarge'>
      <group>
        <subgroup hint-weight='33' hint-textStacking='center'>
          <image src='{item.images[0]}'  hint-align='center' />
        </subgroup>
        <subgroup>
          <text hint-style='titleSubtle'>{item.type}</text>
          <text hint-style='caption' hint-wrap='true' hint-maxLines='3'>{item.desc}</text>
        </subgroup>

      </group>
      <group>
        <subgroup hint-weight='33' hint-textStacking='center'>
          <image src='{item2?.images?[0]}'  hint-align='center'/>
        </subgroup>
        <subgroup>
          <text hint-style='captionSubtle'>Android</text>
          <text hint-style='caption' hint-wrap='true' hint-maxLines='3'>{item2?.desc}</text>
        </subgroup>
      </group>
      <group>
        <subgroup>
          <text hint-style='captionSubtle' hint-maxLines='1' hint-align='right'>{item.publishedAt:yyyy-MM-dd}</text>
        </subgroup>
      </group>
      <image src='{bakImg}'  placement='peek' />
    </binding>

  </visual>
</tile>";
            };
        }



        #endregion
    }
}
