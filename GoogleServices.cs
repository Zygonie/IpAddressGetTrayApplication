using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Threading;
using Json = Newtonsoft.Json;

namespace IpAddressGetTrayApplication
{
   internal class GoogleServices
   {
      public string CurrentIP { get; set; }

      private DriveService driveService { get; set; }

      private GmailService gmailService { get; set; }

      private string FileID { get; set; }

      private static readonly string url = "http://icanhazip.com";

      public GoogleServices()
      {
         this.CurrentIP = string.Empty;
         InitializeGoogleServices();
         GetFileId();
         this.CurrentIP = CheckIpAddress();
         Logger.Log.WriteLog(string.Format("The current IP is {0}", this.CurrentIP));
         UpdateGoogleDocument();
      }

      /// <summary>
      /// Initialize Google servicess
      /// </summary>
      private void InitializeGoogleServices()
      {
         //https://console.developers.google.com/project/named-dialect-796/apiui/credential
         //https://developers.google.com/drive/web/quickstart/quickstart-cs
         //https://developers.google.com/console/help/new/#usingkeys
         //https://developers.google.com/drive/web/examples/dotnet#additional_resources

         try
         {
            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(assemblyName + ".MyCredentials.client_secrets.json"))
            {
               ClientSecrets secrets = GoogleClientSecrets.Load(stream).Secrets;
               IDataStore credentialStore = new FileDataStore("FindIpAddress.credentials");

               UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                  secrets,
                  new[] { DriveService.Scope.Drive, GmailService.Scope.GmailCompose },
                  "user",
                  CancellationToken.None,
                  credentialStore).Result;

               // Create the service.
               this.driveService = new DriveService(new BaseClientService.Initializer()
               {
                  HttpClientInitializer = credential,
                  ApplicationName = "GetMyIpAddress",
               });
               Logger.Log.WriteLog("Google Drive service created");

               this.gmailService = new GmailService(new BaseClientService.Initializer()
               {
                  HttpClientInitializer = credential,
                  ApplicationName = "GetMyIpAddress",
               });
               Logger.Log.WriteLog("Gmail service created");
            }
         }
         catch(Exception err)
         {
            Logger.Log.WriteLog(err);
         }
      }

      /// <summary>
      /// Retrieve the Google Drive File Id
      /// </summary>
      private void GetFileId()
      {
         List<File> result = new List<File>();
         FilesResource.ListRequest listRequest = this.driveService.Files.List();
         listRequest.Q = "title = 'MyIpAddress.txt'";
         do
         {
            try
            {
               FileList files = listRequest.Execute();

               result.AddRange(files.Items);
               listRequest.PageToken = files.NextPageToken;
            }
            catch(Exception e)
            {
               Logger.Log.WriteLog(e);
               listRequest.PageToken = null;
            }
         } while(!String.IsNullOrEmpty(listRequest.PageToken));

         if(result.Count == 1)
         {
            this.FileID = result[0].Id;
            Logger.Log.WriteLog("Google Drive Document found");
         }
         else
         {
            if(result.Count == 0)
            {
               // File's metadata.
               string mimeType = "text/plain";
               File body = new File();
               body.Title = "MyIpAddress.txt";
               body.Description = "The updated IP address of my laptop on Internet";
               body.MimeType = mimeType;

               // File's content.
               System.IO.MemoryStream stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(this.CurrentIP));
               try
               {
                  FilesResource.InsertMediaUpload request = this.driveService.Files.Insert(body, stream, mimeType);
                  request.Upload();

                  File file = request.ResponseBody;

                  this.FileID = file.Id;

                  Logger.Log.WriteLog("New Google Drive Document created");
               }
               catch(Exception e)
               {
                  Logger.Log.WriteLog(e);
               }
            }
            else
            {
               this.FileID = string.Empty;
            }
         }
      }

      /// <summary>
      /// Find the IP address over the net
      /// </summary>
      /// <returns></returns>
      internal string CheckIpAddress()
      {
         WebClient webClient = new WebClient();
         string IP = webClient.DownloadString(url);
         return IP.Replace("\n", string.Empty);
      }

      /// <summary>
      /// To encode the message into GMail known format
      /// </summary>
      /// <param name="input"></param>
      /// <returns></returns>
      private static string Base64UrlEncode(string input)
      {
         var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
         // Special "url-safe" base64 encode.
         return Convert.ToBase64String(inputBytes)
           .Replace('+', '-')
           .Replace('/', '_')
           .Replace("=", "");
      }

      /// <summary>
      /// Send mail service
      /// </summary>
      internal void SendMail()
      {
         MailSecrets mailSecret = null;

         using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("IpAddressGetTrayApplication.MyCredentials.mail_secrets.json"))
         {
            Json.JsonSerializer serializer = new Json.JsonSerializer();
            System.IO.StreamReader streamReader = new System.IO.StreamReader(stream);
            Json.JsonTextReader reader = new Json.JsonTextReader(streamReader);
            mailSecret = serializer.Deserialize<MailSecrets>(reader);
         }

         try
         {
            var fromAddress = new MailAddress(mailSecret.Email, mailSecret.Name);
            var toAddress = new MailAddress(mailSecret.Email, mailSecret.Name);
            string body = CheckIpAddress();

            var msg = new AE.Net.Mail.MailMessage
            {
               Subject = "Adresse",
               Body = CheckIpAddress(),
               From = fromAddress
            };
            msg.To.Add(toAddress);
            msg.ReplyTo.Add(msg.From); // Bounces without this!!
            var msgStr = new System.IO.StringWriter();
            msg.Save(msgStr);

            var result = this.gmailService.Users.Messages.Send(new Message
            {
               Raw = Base64UrlEncode(msgStr.ToString())
            }, "me").Execute();
            Logger.Log.WriteLog("Email sent");
         }
         catch(Exception err)
         {
            Logger.Log.WriteLog(err);
         }

         #region With unauthorized application

         // //Need to allow unauthorized applications in Google account
         //using(var smtp = new SmtpClient())
         //{
         //   smtp.Host = "smtp.gmail.com";
         //   smtp.Port = 587;
         //   smtp.EnableSsl = true;
         //   smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
         //   smtp.UseDefaultCredentials = false;
         //   smtp.Timeout = 10000;
         //   smtp.Credentials = new NetworkCredential(fromAddress.Address, mailSecret.Password);

         //   using(var message = new MailMessage(fromAddress, toAddress))
         //   {
         //      message.Subject = subject;
         //      message.Body = body;
         //      message.IsBodyHtml = true;
         //      try
         //      {
         //         smtp.Send(message);
         //         if(this.currentWindowState == System.Windows.WindowState.Normal)
         //         {
         //            MessageBox.Show("eMail sent", "", MessageBoxButton.OK, MessageBoxImage.Information);
         //         }
         //      }
         //      catch(Exception ep)
         //      {
         //         MessageBox.Show("Exception Occured:" + ep.Message, "Send Mail Error", MessageBoxButton.OK, MessageBoxImage.Error);
         //      }
         //   }
         //}

         #endregion With unauthorized application
      }

      /// <summary>
      /// Update tht Google Drive text document.
      /// </summary>
      internal void UpdateGoogleDocument()
      {
         try
         {
            // First retrieve the file from the API.
            File file = this.driveService.Files.Get(this.FileID).Execute();

            // File's new content.
            using(System.IO.MemoryStream stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(this.CurrentIP)))
            {
               // Send the request to the API.
               FilesResource.UpdateMediaUpload request = this.driveService.Files.Update(file, this.FileID, stream, file.MimeType);
               request.NewRevision = true;
               request.Upload();
               Logger.Log.WriteLog("Google Drive Document updated");
               //File updatedFile = request.ResponseBody;
               //return updatedFile;
            }
         }
         catch(Exception err)
         {
            Logger.Log.WriteLog(err);
         }
      }

      /// <summary>
      /// Check the current IP and update the Google Drive text document and send email is needed.
      /// </summary>
      internal void Update()
      {
         string IP = CheckIpAddress();
         if(this.CurrentIP != IP)
         {
            Logger.Log.WriteLog("Ip has changed");
            SendMail();
            UpdateGoogleDocument();
         }
      }
   }
}