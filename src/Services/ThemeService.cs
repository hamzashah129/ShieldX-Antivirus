using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace ShieldX.Services
{
    public enum AppTheme { Dark, Light }

    public static class ThemeService
    {
        public static AppTheme CurrentTheme { get; private set; }
            = AppTheme.Dark;

        public static event Action<AppTheme>? ThemeChanged;
        public static bool IsDark =>
            CurrentTheme == AppTheme.Dark;

        public static void ApplyTheme(AppTheme theme)
        {
            CurrentTheme = theme;
            var r = Application.Current.Resources;

            if (theme == AppTheme.Dark)
            {
                Set(r, "AppBg",         "#0D1117");
                Set(r, "CardBg",        "#161B27");
                Set(r, "CardBg2",       "#12161F");
                Set(r, "SidebarBg",     "#0A0D14");
                Set(r, "TitleBg",       "#0D1117");
                Set(r, "TextPrimary",   "#FFFFFF");
                Set(r, "TextSecondary", "#718096");
                Set(r, "TextMuted",     "#4A5568");
                Set(r, "BorderClr",     "#2D3748");
                Set(r, "InputBg",       "#0D1117");
                Set(r, "HoverBg",       "#1E2433");
                Set(r, "NavHover",      "#161B27");
                Set(r, "NavActive",     "#0D2818");
                Set(r, "NavSelectedBg", "#1A4D48");
                Set(r, "RowBg",         "#0D1117");
                Set(r, "AltRowBg",      "#111827");
                Set(r, "GridLine",      "#1E293B");
                Set(r, "AccentClr",     "#00E5CC");
                Set(r, "DataGridHdr",   "#0D1117");

                // Legacy keys for compatibility
                Set(r, "AppBackground", "#0D1117");
                Set(r, "CardBackground", "#161B27");
                Set(r, "CardBackground2", "#12161F");
                Set(r, "SidebarBackground", "#0A0D14");
                Set(r, "BorderColor", "#2D3748");
                Set(r, "InputBackground", "#0D1117");
                Set(r, "HoverBackground", "#1E2433");
                Set(r, "AccentColor", "#00E5CC");
                Set(r, "TitleBarBackground", "#0D1117");
                Set(r, "NavButtonHover", "#161B27");
                Set(r, "NavButtonActive", "#0D2818");
                Set(r, "RowBackground", "#0D1117");
                Set(r, "AltRowBackground", "#111827");
                Set(r, "GridLineColor", "#1E293B");

                // Dark mode extra brushes
                Set(r, "BackgroundDeepBrush",    "#080C14");
                Set(r, "BackgroundCardBrush",    "#0D1421");
                Set(r, "CardBackgroundBrush",    "#0D1421");
                Set(r, "BackgroundSurfaceBrush", "#111B2E");
                Set(r, "AccentPrimaryBrush",     "#00E5CC");
                Set(r, "TextPrimaryBrush",       "#E8F4F8");
                Set(r, "TextSecondaryBrush",     "#718096");
                Set(r, "PageBackgroundBrush",    "#080C14");
                Set(r, "PrimaryBrush",           "#00E5CC");
            }
            else
            {
                Set(r, "AppBg",         "#F0F4F8");
                Set(r, "CardBg",        "#FFFFFF");
                Set(r, "CardBg2",       "#F8FAFC");
                Set(r, "SidebarBg",     "#FFFFFF");
                Set(r, "TitleBg",       "#FFFFFF");
                Set(r, "TextPrimary",   "#0F172A");
                Set(r, "TextSecondary", "#475569");
                Set(r, "TextMuted",     "#94A3B8");
                Set(r, "BorderClr",     "#E2E8F0");
                Set(r, "InputBg",       "#F8FAFC");
                Set(r, "HoverBg",       "#E8F7F6");
                Set(r, "NavHover",      "#E2E8F0");
                Set(r, "NavActive",     "#DCFCE7");
                Set(r, "NavSelectedBg", "#D5F1F0");
                Set(r, "RowBg",         "#FFFFFF");
                Set(r, "AltRowBg",      "#F8FAFC");
                Set(r, "GridLine",      "#E2E8F0");
                Set(r, "AccentClr",     "#00A89E");
                Set(r, "DataGridHdr",   "#F1F5F9");

                // Legacy keys for compatibility
                Set(r, "AppBackground", "#F0F4F8");
                Set(r, "CardBackground", "#FFFFFF");
                Set(r, "CardBackground2", "#F8FAFC");
                Set(r, "SidebarBackground", "#FFFFFF");
                Set(r, "BorderColor", "#E2E8F0");
                Set(r, "InputBackground", "#F8FAFC");
                Set(r, "HoverBackground", "#E8F7F6");
                Set(r, "AccentColor", "#00A89E");
                Set(r, "TitleBarBackground", "#FFFFFF");
                Set(r, "NavButtonHover", "#E2E8F0");
                Set(r, "NavButtonActive", "#DCFCE7");
                Set(r, "RowBackground", "#FFFFFF");
                Set(r, "AltRowBackground", "#F8FAFC");
                Set(r, "GridLineColor", "#E2E8F0");

                // Light mode extra brushes
                Set(r, "BackgroundDeepBrush",    "#F0F4F8");
                Set(r, "BackgroundCardBrush",    "#FFFFFF");
                Set(r, "CardBackgroundBrush",    "#FFFFFF");
                Set(r, "BackgroundSurfaceBrush", "#F8FAFC");
                Set(r, "AccentPrimaryBrush",     "#00A89E");
                Set(r, "TextPrimaryBrush",       "#0F172A");
                Set(r, "TextSecondaryBrush",     "#475569");
                Set(r, "PageBackgroundBrush",    "#F0F4F8");
                Set(r, "PrimaryBrush",           "#00A89E");
            }

            SavePref(theme);
            ThemeChanged?.Invoke(theme);
        }

        private static void Set(ResourceDictionary r,
            string key, string hex)
        {
            try
            {
                r[key] = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(hex));
            }
            catch { }
        }

        public static void LoadSavedTheme()
        {
            try
            {
                string? pref = Registry.GetValue(
                    @"HKEY_CURRENT_USER\SOFTWARE\ShieldX",
                    "Theme", "Dark")?.ToString();
                ApplyTheme(pref == "Light"
                    ? AppTheme.Light : AppTheme.Dark);
            }
            catch { ApplyTheme(AppTheme.Dark); }
        }

        private static void SavePref(AppTheme t)
        {
            try
            {
                Registry.SetValue(
                    @"HKEY_CURRENT_USER\SOFTWARE\ShieldX",
                    "Theme", t.ToString());
            }
            catch { }
        }
    }
}
