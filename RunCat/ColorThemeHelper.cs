using Microsoft.Win32;
using System;
using System.Drawing;

namespace RunCat
{
    public static class ColorThemeHelper
    {
        public static string GetPrefix(ColorTheme theme)
        {
            switch (theme)
            {
                case ColorTheme.AccentColor:
                case ColorTheme.Dark:
                    return "dark";

                case ColorTheme.Light:
                default:
                    return "light";
            }
        }

        /// <summary>
        /// 取得 Windows 的 Accent Color (強調色)
        /// </summary>
        public static Color GetWindowsAccentColor()
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
                if (key != null)
                {
                    object accentColorValue = key.GetValue("AccentColor");
                    if (accentColorValue != null)
                    {
                        int accentColorInt = (int)accentColorValue;
                        return Color.FromArgb(
                            255,  // Alpha 通道 (透明度)
                            accentColorInt & 0xFF,          // R
                            (accentColorInt >> 8) & 0xFF,   // G
                            (accentColorInt >> 16) & 0xFF   // B
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(@$"無法讀取 Accent Color: {ex.Message}");
            }

            return Color.Gray; // 預設回傳灰色 (如果讀取失敗)
        }

        public static bool hasThemeColor()
        {
            string keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using (RegistryKey rKey = Registry.CurrentUser.OpenSubKey(keyName))
            {
                object value;
                if (rKey == null || (value = rKey.GetValue("SystemUsesLightTheme")) == null)
                {
                    Console.WriteLine(@"Oh No! Couldn't get theme light/dark");
                    return false;
                }
                int theme = (int)value;
                return theme != 0;
            }
        }

        /// <summary>
        /// 取得 Windows 的 顏色模式
        /// </summary>
        /// <returns></returns>
        public static ColorTheme GetAppsUseTheme()
        {
            string keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using (RegistryKey rKey = Registry.CurrentUser.OpenSubKey(keyName))
            {
                object value;
                if (rKey == null || (value = rKey.GetValue("SystemUsesLightTheme")) == null)
                {
                    Console.WriteLine(@"Oh No! Couldn't get theme light/dark");
                    return ColorTheme.Light;
                }
                int theme = (int)value;
                return theme == 0 ? ColorTheme.Dark : ColorTheme.Light;
            }
        }
    }
}
