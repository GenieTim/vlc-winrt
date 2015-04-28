﻿/**********************************************************************
 * VLC for WinRT
 **********************************************************************
 * Copyright © 2013-2014 VideoLAN and Authors
 *
 * Licensed under GPLv2 and MPLv2
 * Refer to COPYING file of the official project for license
 **********************************************************************/

using Windows.Storage;

namespace VLC_WinRT.Helpers
{
    static class ApplicationSettingsHelper
    {
        /// <summary>
        /// Function that checks if the entry exists in Application settings
        /// </summary>
        public static bool Contains(string key)
        {
            return ApplicationData.Current.LocalSettings.Values.ContainsKey(key);
        }

        /// <summary>
        /// Function to read a setting value and clear it after reading it
        /// </summary>
        public static object ReadResetSettingsValue(string key)
        {
            LogHelper.RuntimeLog(key);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
            {
                LogHelper.RuntimeLog("null returned");
                return null;
            }
            else
            {
                var value = ApplicationData.Current.LocalSettings.Values[key];
                ApplicationData.Current.LocalSettings.Values.Remove(key);
                LogHelper.RuntimeLog("value found " + value.ToString());
                return value;
            }
        }

        /// <summary>
        /// Function to read a setting value
        /// </summary>
        public static object ReadSettingsValue(string key)
        {
            LogHelper.RuntimeLog(key);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
            {
                LogHelper.RuntimeLog("null returned");
                return null;
            }
            else
            {
                var value = ApplicationData.Current.LocalSettings.Values[key];
                LogHelper.RuntimeLog("value found " + value.ToString());
                return value;
            }
        }
        
        /// <summary>
        /// Save a key value pair in settings. Create if it doesn't exist
        /// </summary>
        public static void SaveSettingsValue(string key, object value)
        {
            LogHelper.RuntimeLog(key + ":" + value.ToString());
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
            {
                ApplicationData.Current.LocalSettings.Values.Add(key, value);
            }
            else
            {
                ApplicationData.Current.LocalSettings.Values[key] = value;
            }
        }
    }
}
