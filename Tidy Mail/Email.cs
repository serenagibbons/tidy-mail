using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tidy_Mail
{
    class Email
    {
        public string Id { get; set; }
        public bool IsSelected { get; set; }
        public string Snippet { get; set; }
        public string From { get; set; }
        public string Date { get; set; }
        public string Subject { get; set; }
    }
}
