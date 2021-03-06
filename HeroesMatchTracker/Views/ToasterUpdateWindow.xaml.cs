﻿using HeroesMatchTracker.Core;
using HeroesMatchTracker.Core.ViewModels;
using HeroesMatchTracker.Data;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace HeroesMatchTracker.Views
{
    /// <summary>
    /// Interaction logic for ToasterWindow.xaml
    /// </summary>
    public partial class ToasterUpdateWindow
    {
        private ToasterUpdateWindowViewModel ToasterUpdateWindowViewModel;
        private IDatabaseService Database;

        public ToasterUpdateWindow(string currentVersion, string newVersion)
        {
            ShowInTaskbar = false;

            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                CurrentVersion.Text = currentVersion;
                NewVersion.Text = newVersion;

                var workingArea = SystemParameters.WorkArea;
                var transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
                var corner = transform.Transform(new Point(workingArea.Right, workingArea.Bottom));

                Left = corner.X - ActualWidth - 20;
                Top = corner.Y - ActualHeight - 20;
            }));

            InitializeComponent();

            ToasterUpdateWindowViewModel = (ToasterUpdateWindowViewModel)DataContext;
            Database = ToasterUpdateWindowViewModel.GetDatabaseService;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (ExternalLinkedSites.IsApprovedSite(e.Uri))
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
                e.Handled = true;
            }
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            Database.SettingsDb().UserSettings.IsUpdateAvailableKnown = true;
        }
    }
}
