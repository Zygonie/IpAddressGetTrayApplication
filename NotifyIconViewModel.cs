using System;
using System.Windows.Input;
//using System.Timers;
using System.Threading;
using System.Threading.Tasks;

namespace IpAddressGetTrayApplication
{
   public class NotifyIconViewModel : System.ComponentModel.INotifyPropertyChanged
   {
      #region Fields

      private ICommand _sendMailCommand;
      private ICommand _getIpCommand;
      private ICommand _exitCommand;
      private Timer _timer;
      private string _currentIP;
      private GoogleServices _googleServices;
      private bool _isVisible;
      private const string defaultMessage = "IP currently retrieved...";

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
         this.CurrentIP = defaultMessage;
         this.IsVisible = true;
      }

      #endregion

      #region Methods

      internal void Start()
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
                  
         this._timer = new Timer(new TimerCallback(timer_Elapsed), null, 6 * 3600 * 1000, Timeout.Infinite);
         Logger.Log.WriteLog("IP getter service started.");

         try
         {
            Task.Factory.StartNew(() =>
            {
               int count = 0;
               do
               {
                  this._googleServices = new GoogleServices();
                  if(this._googleServices == null || this._googleServices.CurrentIP == string.Empty)
                  {
                     Thread.Sleep(5 * 60 * 1000);
                     count++;
                  }
                  else
                  {
                     DispatchService.Invoke(() =>
                     {
                        this.CurrentIP = this._googleServices.CurrentIP;
                     });
                  }
               }
               while(this.CurrentIP == defaultMessage && count < 12);//retry max 1h
            });
         }
         catch(Exception err)
         {
            Logger.Log.WriteLog(err);
         }
      }

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

      private void timer_Elapsed(object state)
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
