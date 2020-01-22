# Tidy Mail
Tidy Mail is an application which accesses a user's Gmail messages using the Gmail API to view the user's inbox and delete selected messages.

Users may log in to Tidy Mail using Gmail, authenticated by OAuth 2.0 protocol to authorize access to the user's messages. Tidy Mail enables users to search through their Gmail messages using [Gmail search operators](https://support.google.com/mail/answer/7190?hl=en) to filter emails. The default criteria for dispalying messages are messages that are unread and in the user's Gmail inbox. Users may select individual emails to delete, or select all displayed emails to delete up to 1,000 emails at once. Deleted emails are moved to the user's Gmail trash folder, and may be recovered up to 30 days after being deleted.

This application was inpsired by a Gmail account which over many years had acquired thousands of promotional emails. Deleting that many emails page by page - by default 50, or at most 100 emails at a time - in the Gmail website was tedious and time consuming. Tidy Mail was developed to solve this problem of automating and speeding up the process of deleting large amounts of emails.

## References
[Accessing and deleting large e-mails in Gmail with C#](https://docs.microsoft.com/en-us/archive/blogs/mvpawardprogram/accessing-and-deleting-large-e-mails-in-gmail-with-c)  
[Gmail API References: Users.messages:list](https://developers.google.com/gmail/api/v1/reference/users/messages/list)
