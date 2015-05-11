namespace IpAddressGetTrayApplication
{
   internal class MailSecrets
   {
      public string Password { get; set; }

      public string Email { get; set; }

      public string Name { get; set; }

      public MailSecrets()
      {
         this.Password = string.Empty;
         this.Email = string.Empty;
         this.Name = string.Empty;
      }

      public MailSecrets(string email, string pwd, string name)
      {
         this.Email = email;
         this.Password = pwd;
         this.Name = name;
      }
   }
}