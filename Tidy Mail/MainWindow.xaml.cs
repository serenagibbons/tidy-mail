using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
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

        //ObservableCollection<Email> emailMessages = new ObservableCollection<Email>();

        int maxResults = 0;
        int messageCount = 0;

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
        private void RefreshEmails(object sender, RoutedEventArgs e)
        {
            String query = "in:inbox is:unread";
            GetEmails(query);
        }

        private async void GetEmails(String searchQuery)
        {
            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = AppName,
            });

            IList<Message> messages = null;
            var emailMessages = new ObservableCollection<Email>();
            OperationText.Text = "downloading messages";
            BusyBorder.Visibility = Visibility.Visible;

            await Task.Run(async () =>
            {
                /*UsersResource.MessagesResource.ListRequest request =
                service.Users.Messages.List("me");
                request.Q = "in:inbox is:unread";
                //request.MaxResults = 500;

                //messages = request.Execute().Messages;*/

                String query = searchQuery;
                messages = ListMessages(service, query);

                // limit app to show only first 500 results
                messageCount = messages.Count;
                if (messageCount > 100)
                {
                    maxResults = 100;
                }
                else
                {
                    maxResults = messageCount;
                }
                // set progress bar to amound of email results in UI thread
                await Dispatcher.InvokeAsync(() =>
                    ProgressBar.Maximum = maxResults, System.Windows.Threading.DispatcherPriority.Normal);

                for (int index = 0; index < maxResults; index++)
                {
                    try
                    {
                        var message = messages[index];
                        var getRequest = service.Users.Messages.Get("me", message.Id);
                        getRequest.Format =
                            UsersResource.MessagesResource.GetRequest.FormatEnum.Metadata;
                        getRequest.MetadataHeaders = new Repeatable<string>(
                            new[] { "Subject", "Date", "From" });
                        messages[index] = getRequest.Execute();

                        emailMessages.Add(new Email()
                        {
                            Id = messages[index].Id,
                            Snippet = WebUtility.HtmlDecode(messages[index].Snippet),
                            From = messages[index].Payload.Headers.FirstOrDefault(h =>
                                h.Name == "From").Value,
                            Subject = messages[index].Payload.Headers.FirstOrDefault(h =>
                                h.Name == "Subject").Value,
                            Date = messages[index].Payload.Headers.FirstOrDefault(h =>
                                h.Name == "Date").Value,
                        });

                        // update progress bar and download progress text in UI thread
                        var index1 = index + 1;
                        await Dispatcher.InvokeAsync(() =>
                        {
                            ProgressBar.Value = index1;
                            if (maxResults < messages.Count)
                            {
                                OperationProgress.Text = $"{index1} of first {maxResults}";
                            }
                            else
                            {
                                OperationProgress.Text = $"{index1} of {maxResults}";
                            }
                        }, System.Windows.Threading.DispatcherPriority.Normal);
                    }
                    catch (NullReferenceException)
                    {
                        // do nothing
                    }
                }
            });

            BusyBorder.Visibility = Visibility.Collapsed;
            EmailListView.ItemsSource = new ObservableCollection<Email>(emailMessages);
            //EmailListView.ItemsSource = emailMessages;
            if (maxResults > 0)
            {
                EmailCount.Text = $"1 - {maxResults} of {messages.Count} messages.";
            }
            else
            {
                EmailCount.Text = $"{messages.Count} messages.";
            }
        }


        // <summary>
        /// List all Messages of the user's mailbox matching the query.
        /// </summary>
        /// <param name="service">Gmail API service instance.</param>
        public static List<Message> ListMessages(GmailService service, String query)
        {
            List<Message> result = new List<Message>();
            UsersResource.MessagesResource.ListRequest request = service.Users.Messages.List("me");
            request.Q = query;
            
            do
            {
                try
                {
                    ListMessagesResponse response = request.Execute();
                    result.AddRange(response.Messages);
                    request.PageToken = response.NextPageToken;
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                }
            } while (!String.IsNullOrEmpty(request.PageToken));
            
            return result;
        }

        // search for emails according to input criteria and display results
        private void SearchEmails(object sender, RoutedEventArgs e)
        {
            String query = searchBox.Text;
            GetEmails(query);
        }

        // delete selected emails
        private async void DeleteEmails(object sender, RoutedEventArgs e)
        {
            var messages = (ObservableCollection<Email>)EmailListView.ItemsSource;
            var messagesToDelete = messages.Where(m => m.IsSelected).ToList();
            if (!messagesToDelete.Any())
            {
                MessageBoxResult messageBox = MessageBox.Show("There are no selected messages to delete");
                return;
            }

            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = AppName,
            });
            OperationText.Text = "deleting messages";
            ProgressBar.Maximum = messagesToDelete.Count;
            OperationProgress.Text = "";
            BusyBorder.Visibility = Visibility.Visible;

            await Task.Run(async () =>
            {
                for (int index = 0; index < messagesToDelete.Count; index++)
                {
                    var message = messagesToDelete[index];
                    var response = service.Users.Messages.Trash("me", message.Id);
                    response.Execute();
                    var index1 = index + 1;
                    await Dispatcher.InvokeAsync(() =>
                    {
                        ProgressBar.Value = index1;
                        OperationText.Text = $"{index1} of {messagesToDelete.Count}";
                        messages.Remove(message);
                        EmailCount.Text = $"1 - {--maxResults} of {--messageCount} messages.";
                    }, System.Windows.Threading.DispatcherPriority.Normal);
                }
            });
            BusyBorder.Visibility = Visibility.Collapsed;
        }

        private void SelectAllEmails(object sender, RoutedEventArgs e)
        {
            var messages = (ObservableCollection<Email>)EmailListView.ItemsSource;

            if ((string)selectAllButton.Content == "Select All")
            {
                // select all messages
                foreach(var a in messages)
                {
                    a.IsSelected = true;
                }

                selectAllButton.Content = "Unselect All";
            }
            else
            {
                // unselect all messages 
                foreach (var a in messages)
                {
                    a.IsSelected = false;
                }

                selectAllButton.Content = "Select All";
            }
        }
    }
}