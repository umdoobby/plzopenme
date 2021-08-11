using System;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace PlzOpenMe
{
    public static class EmailSender
    {
        private static string Host;

        private static int Port;

        private static string Username;

        private static string Password;

        private static string FromAddress;

        private static string[] ToAddresses;

        
        public static void Init(IConfiguration configuration)
        {
            Host = configuration.GetValue<string>("Smtp:Host");
            Port = configuration.GetValue<int>("Smtp:Port");
            Username = configuration.GetValue<string>("Smtp:Username");
            Password = configuration.GetValue<string>("Smtp:Password");
            FromAddress = configuration.GetValue<string>("Smtp:FromAddress");
            ToAddresses = configuration.GetValue<string[]>("Smtp:ToAddresses");
        }

        
        public static void Send(string subject, string message)
        {
            // The email sender is not configured
            if (Host == null || Username == null || Password == null)
            {
                throw new Exception("EmailSender is not initialized");
            }
            
            // TODO: actually make this send an email
        }
    }
}