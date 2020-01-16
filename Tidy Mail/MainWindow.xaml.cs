using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Tidy_Mail
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private UserCredential credential;
        const string AppName = "Tidy Mail";

        public MainWindow()
        {
            InitializeComponent();
            GetCredentials();
        }

        // load emails into listview when window is first loaded
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshEmails(sender, e);
        }

        // open a web page to prompt for user authorization
        public async void GetCredentials()
        {
            var scopes = new[] { GmailService.Scope.GmailModify };
            using (var stream = new FileStream("client_id.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets, scopes, "user", CancellationToken.None);
            }
        }

        // load emails from Gmail and update number of emails
        private void RefreshEmails (object sender, RoutedEventArgs e)
        {
            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = AppName,
            });

            //UsersResource.MessagesResource.ListRequest request =
              //  service.Users.Messages.List("me");
            //var messages = request.Execute().Messages;

            List<Message> result = ListMessages(service);

            for (int index = 0; index < result.Count; ++index)
            {
                var message = result[index];
                var getRequest = service.Users.Messages.Get("me", message.Id);
                getRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Metadata;
                getRequest.MetadataHeaders = new Repeatable<string>(
                    new[] { "Subject", "Date", "From" });
                result[index] = getRequest.Execute();

            }

            // load emails into listview
            EmailListView.ItemsSource = result;

            // update number of emails
            String numEmails = (result.Count).ToString("#,##0");
            EmailCount.Text = numEmails + " results";
        }

        // <summary>
        /// List all Messages of the user's mailbox matching the query.
        /// </summary>
        /// <param name="service">Gmail API service instance.</param>
        public static List<Message> ListMessages(GmailService service)
        {
            List<Message> result = new List<Message>();
            UsersResource.MessagesResource.ListRequest request = service.Users.Messages.List("me");
            request.Q = "is:unread";
            request.MaxResults = 200;
            
            do
            {
                try
                {
                    ListMessagesResponse response = request.Execute();
                    result.AddRange(response.Messages);
                    //request.PageToken = response.NextPageToken;
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                }
            } while (!String.IsNullOrEmpty(request.PageToken));
            
            return result;
        }

    }
}