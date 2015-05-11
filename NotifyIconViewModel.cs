using System;
using System.Windows.Input;
using System.Timers;
using System.Threading.Tasks;

namespace IpAddressGetTrayApplication
{
   public class NotifyIconViewModel
   {
      #region Fields

      private ICommand _sendMailCommand;
      private ICommand _getIpCommand;
      private ICommand _exitCommand;
      private Timer _timer;
      private string _currentIP;
      private GoogleServices _googleServices;
      private bool _isVisible;

      #endregion

      #region Commands

      public ICommand SendMailCommand
      {
         get
         {
            if(_sendMailCommand == null)
            {
               _sendMailCommand = new RelayCommand(
                   param => this.SendMail()
                   );
            }
            return _sendMailCommand;
         }
      }


      public ICommand GetIpCommand
      {
         get
         {
            if(_getIpCommand == null)
            {
               _getIpCommand = new RelayCommand(
                   param => this.GetIp()
                   );
            }
            return _getIpCommand;
         }
      }

      public ICommand ExitCommand
      {
         get
         {
            if(_exitCommand == null)
            {
               _exitCommand = new RelayCommand(
                   param => { System.Windows.Application.Current.Shutdown(); }
                   );
            }
            return _exitCommand;
         }
      }

      #endregion

      #region Properties

      public string CurrentIP
      {
         get { return this._currentIP; }
         set { this._currentIP = value; OnPropertyChanged("CurrentIP"); }
      }

      public bool IsVisible
      {
         get { return this._isVisible; }
         set { this._isVisible = value; OnPropertyChanged("IsVisible"); }
      }

      #endregion

      #region Ctor

      public NotifyIconViewModel()
      {
         //Register to start with Windows
         try
         {
#if !DEBUG
            using(Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
               if(key.GetValue("IpAddressGetTrayApplication") == null)
               {
                  key.SetValue("IpAddressGetTrayApplication", "\"" + System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + "\"");
               }
            }
#endif
         }
         catch(Exception err)
         {
            Logger.Log.WriteLog(err);
         }
         //Unregister
         //using(Microsoft.Win32.RegistryKey key = Microsoft.Win32.RegistryKey.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
         //{
         //   key.DeleteValue("My Program", false);
         //}

         this.CurrentIP = "IP currently retrieved...";

         this._isVisible = true;
         this._timer = new Timer();
         this._timer.Elapsed += timer_Elapsed;
         this._timer.Interval = TimeSpan.FromHours(6).TotalMilliseconds;
         this._timer.Start();
         Logger.Log.WriteLog("IP getter service started.");

         try
         {
            this._googleServices = new GoogleServices();
            this.CurrentIP = this._googleServices.CurrentIP;
            this._isVisible = true;
         }
         catch(Exception err)
         {
            Logger.Log.WriteLog(err);
         }
      }

      #endregion

      #region Methods

      private void SendMail()
      {
         Task.Factory.StartNew(() =>
         {
            try
            {
               this._googleServices.SendMail();
            }
            catch(Exception err)
            {
               Logger.Log.WriteLog(err);
            }
         });
      }

      private void GetIp()
      {
         Task.Factory.StartNew(() =>
         {
            try
            {
               this.CurrentIP = this._googleServices.CheckIpAddress();
            }
            catch(Exception err)
            {
               Logger.Log.WriteLog(err);
            }
         });
      }

      private void timer_Elapsed(object sender, ElapsedEventArgs e)
      {
         Task.Factory.StartNew(() =>
         {
            try
            {
               this._googleServices.Update();
            }
            catch(Exception err)
            {
               Logger.Log.WriteLog(err);
            }
         });
      }

      #endregion

      #region Property changed

      public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         System.ComponentModel.PropertyChangedEventHandler handler = this.PropertyChanged;
         if(handler != null)
         {
            var e = new System.ComponentModel.PropertyChangedEventArgs(propertyName);
            handler(this, e);
         }
      }

      #endregion
   }
}
