﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using System.Timers;
using System.Threading.Tasks;

namespace IpAddressGetTrayApplication
{
   /// <summary>
   /// Interaction logic for App.xaml
   /// </summary>
   public partial class App : Application
   {
      private TaskbarIcon notifyIcon;

      protected override void OnStartup(StartupEventArgs e)
      {
         base.OnStartup(e);

         //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
         notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
         NotifyIconViewModel viewModel = new NotifyIconViewModel();
         notifyIcon.DataContext = viewModel;
         viewModel.Start();
      }

      protected override void OnExit(ExitEventArgs e)
      {
         notifyIcon.Dispose(); //the icon would clean up automatically, but this is cleaner
         base.OnExit(e);
      }
   }
}
