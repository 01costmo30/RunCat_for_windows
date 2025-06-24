using RunCat.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Resources;

namespace RunCat
{
    public class IconManager
    {
        private Icon[] _icons;
        private Icon[] _heatedIcons;
        private Icon[] _themeIcons;
        private string _runner;
        private ColorTheme _colorTheme;
        private bool _hasThemeColor = false;
        private bool _isHeatUp = false;

        public IconManager(ColorTheme theme, string runner)
        {
            _colorTheme = theme;
            _runner = runner;

            IconInit();
        }

        private Icon[] getIconSets(bool addColor, Color color)
        {
            string prefix = ColorThemeHelper.GetPrefix(_colorTheme);
            ResourceManager rm = Resources.ResourceManager;
            // default runner is cat
            int capacity;

            switch (_runner)
            {
                case "parrot":
                    capacity = 10;
                    break;
                case "horse":
                    capacity = 14;
                    break;
                default:
                    capacity = 5;
                    break;
            }

            List<Icon> list = new List<Icon>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                var icon = (Icon)rm.GetObject($"{prefix}_{_runner}_{i}");

                if (addColor)
                    icon = IconMaskHelper.ApplyColorMaskToIcon(icon, color);
                list.Add(icon);
            }
            return list.ToArray();
        }

        private void IconInit()
        {
            // TODO: test
            Console.WriteLine("icon init");
            _heatedIcons = getIconSets(true, Color.DarkRed);
            if (ColorThemeHelper.hasThemeColor())
            {
                _hasThemeColor = true;
                _themeIcons = getIconSets(true, ColorThemeHelper.GetWindowsAccentColor());
            }
            _icons = getIconSets(false, Color.Transparent);
        }

        public bool isColorOverride()
        {
            return _isHeatUp;
        }

        public Icon[] GetIcons(ColorTheme colorTheme, string runner, bool isheatUp)
        {
            if (colorTheme != _colorTheme || runner != _runner)
            {
                _colorTheme = colorTheme;
                _runner = runner;
                IconInit();
            }

            if (isheatUp && isheatUp != _isHeatUp)
            {
                _isHeatUp = isheatUp;
                return _heatedIcons;
            }
            _isHeatUp = false;
            if (colorTheme == ColorTheme.AccentColor && _hasThemeColor)
                return _themeIcons;
            return _icons;
        }
    }
}
