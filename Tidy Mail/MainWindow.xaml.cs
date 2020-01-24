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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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

        // load unread emails from Gmail inbox
        private void RefreshEmails(object sender, RoutedEventArgs e)
        {
            // refresh to default query
            string query = "in:inbox is:unread";
            // if search box is not empty, set query to search box text
            if (searchBox.Text != "")
            {
                query = searchBox.Text;
            }
            GetEmails(query);
        }

        // load emails from Gmail according to search query
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
                String query = searchQuery;
                messages = ListMessages(service, query);

                // limit app to show only first 1000 results
                messageCount = messages.Count;
                if (messageCount > 1000)
                {
                    maxResults = 1000;
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
                                OperationText.Text = $"dowloading {index1} of first {maxResults} messages";
                            }
                            else
                            {
                                OperationText.Text = $"downloading {index1} of {maxResults} messages";
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
                MessageBox.Show("There are no selected messages to delete");
                return;
            }

            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = AppName,
            });
            OperationText.Text = "deleting messages";
            ProgressBar.Maximum = messagesToDelete.Count;
            //OperationProgress.Text = "";

            // prompt user for confirmation to delete selected emails
            MessageBoxResult messageBoxResult = MessageBox.Show($"Are you sure you would you like to delete {messagesToDelete.Count} messages?", "Delete Messages", MessageBoxButton.YesNo);
            switch(messageBoxResult)
            {
                case MessageBoxResult.Yes:
                    break;
                case MessageBoxResult.No:
                    return;
            }

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
                        OperationText.Text = $"deleting {index1} of {messagesToDelete.Count} messages";
                        messages.Remove(message);

                        if (EmailListView.Items.Count == 0)
                        {
                            // if all emails were deleted
                            EmailCount.Text = $"0 of {--messageCount} messages.";
                        }
                        else
                        {
                            EmailCount.Text = $"1 - {--maxResults} of {--messageCount} messages.";
                        }
                       
                    }, System.Windows.Threading.DispatcherPriority.Normal);
                }
            });
            BusyBorder.Visibility = Visibility.Collapsed;

            // change button text to select all
            selectAllButton.Content = "Select All";

            if (messageCount > 0)
            {
                // retrieve any remaining messages
                String query = searchBox.Text;
                GetEmails(query);
            }
            // display message of number of messages deleted
            MessageBox.Show($"{messagesToDelete.Count} messages deleted");
        }

        // select or unselect all messages in listview
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
                // change button text to unselect all
                selectAllButton.Content = "Unselect All";
            }
            else
            {
                // unselect all messages 
                foreach (var a in messages)
                {
                    a.IsSelected = false;
                }
                // change button text to select all
                selectAllButton.Content = "Select All";
            }
        }
    }
}