// Copyright 2020 Takuto Nakamura
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using Microsoft.Win32;
using RunCat.Properties;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace RunCat
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // terminate runcat if there's any existing instance
            var procMutex = new System.Threading.Mutex(true, "_RUNCAT_MUTEX", out var result);
            if (!result)
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.Run(new RunCatApplicationContext());

            procMutex.ReleaseMutex();
        }
    }

    public class RunCatApplicationContext : ApplicationContext
    {
        private const int CPU_TIMER_DEFAULT_INTERVAL = 3000;
        private const int ANIMATE_TIMER_DEFAULT_INTERVAL = 200;
        private readonly PerformanceCounter cpuUsage;
        private readonly PerformanceCounter memoryUsage;
        private readonly ToolStripMenuItem runnerMenu;
        private readonly ToolStripMenuItem themeMenu;
        private readonly ToolStripMenuItem startupMenu;
        private readonly ToolStripMenuItem runnerSpeedLimit;
        private readonly NotifyIcon notifyIcon;
        private string runner;
        private int current;
        private float minCPU;
        private float interval;
        private float memoryPercentage;
        private float memoryLimit = 85;
        private bool hasThemeColor = true;
        private ColorTheme manualTheme;
        private string speed = UserSettings.Default.Speed;
        private bool isColorOverride = false;
        private IconManager iconManager;
        private Icon[] currentIcons;
        private readonly Timer animateTimer = new();
        private readonly Timer cpuTimer = new();


        public RunCatApplicationContext()
        {
            UserSettings.Default.Reload();
            runner = UserSettings.Default.Runner;
            manualTheme = (ColorTheme)UserSettings.Default.Theme;

            Application.ApplicationExit += new EventHandler(OnApplicationExit);

            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(UserPreferenceChanged);

            cpuUsage = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
            memoryUsage = new PerformanceCounter("Memory", "% Committed Bytes in Use");
            _ = cpuUsage.NextValue(); // discards first return value
            memoryUsage.NextValue();

            #region sub menu item

            runnerMenu = new ToolStripMenuItem(
                "Runner",
                null,
                new ToolStripMenuItem("Cat", null, SetRunner)
                {
                    Checked = runner.Equals("cat")
                },
                new ToolStripMenuItem("Parrot", null, SetRunner)
                {
                    Checked = runner.Equals("parrot")
                },
                new ToolStripMenuItem("Horse", null, SetRunner)
                {
                    Checked = runner.Equals("horse")
                }
            );

            themeMenu = new ToolStripMenuItem(
                "Theme",
                null,
                new ToolStripMenuItem("Default", null, (sender, e) => SetIconAs(sender, e, ColorTheme.Default))
                {
                    Checked = manualTheme.Equals(ColorTheme.Default)
                },
                new ToolStripMenuItem("Light", null, (sender, e) => SetIconAs(sender, e, ColorTheme.Light))
                {
                    Checked = manualTheme.Equals(ColorTheme.Light)
                },
                new ToolStripMenuItem("Dark", null, (sender, e) => SetIconAs(sender, e, ColorTheme.Dark))
                {
                    Checked = manualTheme.Equals(ColorTheme.Dark)
                },
                new ToolStripMenuItem("Accent Color", null, (sender, e) => SetIconAs(sender, e, ColorTheme.AccentColor))
                {
                    Checked = manualTheme.Equals(ColorTheme.AccentColor)
                }
            );

            startupMenu = new ToolStripMenuItem("Startup", null, SetStartup);
            if (IsStartupEnabled())
            {
                startupMenu.Checked = true;
            }

            runnerSpeedLimit = new ToolStripMenuItem(
                "Runner Speed Limit",
                null,
                new ToolStripMenuItem("Default", null, SetSpeedLimit)
                {
                    Checked = speed.Equals("default")
                },
                new ToolStripMenuItem("CPU 10%", null, SetSpeedLimit)
                {
                    Checked = speed.Equals("cpu 10%")
                },
                new ToolStripMenuItem("CPU 20%", null, SetSpeedLimit)
                {
                    Checked = speed.Equals("cpu 20%")
                },
                new ToolStripMenuItem("CPU 30%", null, SetSpeedLimit)
                {
                    Checked = speed.Equals("cpu 30%")
                },
                new ToolStripMenuItem("CPU 40%", null, SetSpeedLimit)
                {
                    Checked = speed.Equals("cpu 40%")
                }
            );

            #endregion sub menu item

            #region main menu

            ContextMenuStrip contextMenuStrip = new ContextMenuStrip(new Container());
            contextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                runnerMenu,
                themeMenu,
                startupMenu,
                runnerSpeedLimit,
                new ToolStripSeparator(),
                new ToolStripMenuItem($"{Application.ProductName} v{Application.ProductVersion}")
                {
                    Enabled = false
                },
                new ToolStripMenuItem("Exit", null, Exit)
            });

            #endregion main menu

            notifyIcon = new NotifyIcon
            {
                Icon = Resources.light_cat_0,
                ContextMenuStrip = contextMenuStrip,
                Text = "0.0%",
                Visible = true
            };

            notifyIcon.DoubleClick += HandleDoubleClick;

            iconManager = new IconManager(manualTheme, runner);
            UpdateThemeIcons();
            SetAnimation();
            SetSpeed();
            StartObserveCPU();

            current = 1;
        }
        private void OnApplicationExit(object sender, EventArgs e)
        {
            UserSettings.Default.Runner = runner;
            UserSettings.Default.Theme = (int)manualTheme;
            UserSettings.Default.Speed = speed;
            UserSettings.Default.Save();
        }

        private bool IsStartupEnabled()
        {
            string keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using (RegistryKey rKey = Registry.CurrentUser.OpenSubKey(keyName))
            {
                return rKey?.GetValue(Application.ProductName) != null;
            }
        }

        private void UpdateCheckedState(ToolStripMenuItem sender, ToolStripMenuItem menu)
        {
            foreach (ToolStripMenuItem item in menu.DropDownItems)
            {
                item.Checked = false;
            }
            sender.Checked = true;
        }

        private void SetRunner(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            UpdateCheckedState(item, runnerMenu);
            runner = item.Text.ToLower();
            UpdateThemeIcons();
        }

        private void SetSpeed()
        {
            if (speed.Equals("default"))
                return;
            else if (speed.Equals("cpu 10%"))
                minCPU = 100f;
            else if (speed.Equals("cpu 20%"))
                minCPU = 50f;
            else if (speed.Equals("cpu 30%"))
                minCPU = 33f;
            else if (speed.Equals("cpu 40%"))
                minCPU = 25f;
        }

        private void SetSpeedLimit(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            UpdateCheckedState(item, runnerSpeedLimit);
            speed = item.Text.ToLower();
            SetSpeed();
        }

        private void UpdateThemeIcons()
        {
            currentIcons = iconManager.GetIcons(manualTheme, runner, memoryPercentage >= memoryLimit);
            isColorOverride = iconManager.isColorOverride();
        }

        private void SetIconAs(object sender, EventArgs e, ColorTheme color)
        {
            UpdateCheckedState((ToolStripMenuItem)sender, themeMenu);
            manualTheme = color;
            UpdateThemeIcons();
        }

        private void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General) UpdateThemeIcons();
        }

        private void SetStartup(object sender, EventArgs e)
        {
            startupMenu.Checked = !startupMenu.Checked;
            string keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using (RegistryKey rKey = Registry.CurrentUser.OpenSubKey(keyName, true))
            {
                if (startupMenu.Checked)
                {
                    rKey?.SetValue(Application.ProductName, Process.GetCurrentProcess().MainModule.FileName);
                }
                else
                {
                    rKey?.DeleteValue(Application.ProductName, false);
                }
                rKey?.Close();
            }
        }

        private void Exit(object sender, EventArgs e)
        {
            cpuUsage.Close();
            memoryUsage.Close();
            animateTimer.Stop();
            cpuTimer.Stop();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void AnimationTick(object sender, EventArgs e)
        {
            if (currentIcons.Length <= current) current = 0;
            notifyIcon.Icon = currentIcons[current];
            current = (current + 1) % currentIcons.Length;
        }

        private void SetAnimation()
        {
            animateTimer.Interval = ANIMATE_TIMER_DEFAULT_INTERVAL;
            animateTimer.Tick += AnimationTick;
        }

        private void CPUTickSpeed()
        {
            if (!speed.Equals("default"))
            {
                float manualInterval = Math.Max(minCPU, interval);
                animateTimer.Stop();
                animateTimer.Interval = (int)manualInterval;
                animateTimer.Start();
            }
            else
            {
                animateTimer.Stop();
                animateTimer.Interval = (int)interval;
                animateTimer.Start();
            }
        }

        private void CPUTick()
        {
            interval = Math.Min(100, cpuUsage.NextValue()); // Sometimes got over 100% so it should be limited to 100%
            memoryPercentage = memoryUsage.NextValue();
            notifyIcon.Text = $"CPU: {interval:f1}%\n°O¾ÐÅé: {memoryPercentage:f1}%";
            interval = 200.0f / (float)Math.Max(1.0f, Math.Min(20.0f, interval / 5.0f));
            _ = interval;
            CPUTickSpeed();

            if ((memoryPercentage >= memoryLimit && !isColorOverride) || (memoryPercentage < memoryLimit && isColorOverride))
                UpdateThemeIcons();
        }
        private void ObserveCPUTick(object sender, EventArgs e)
        {
            CPUTick();
        }

        private void StartObserveCPU()
        {
            cpuTimer.Interval = CPU_TIMER_DEFAULT_INTERVAL;
            cpuTimer.Tick += new EventHandler(ObserveCPUTick);
            cpuTimer.Start();
        }

        private void HandleDoubleClick(object sender, EventArgs e)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell",
                UseShellExecute = false,
                Arguments = " -c Start-Process taskmgr.exe",
                CreateNoWindow = true,
            };
            Process.Start(startInfo);
        }

    }
}
