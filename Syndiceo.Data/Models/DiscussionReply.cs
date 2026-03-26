using Syndiceo.Data;
using SyndiceoWeb.Areas.Identity.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Syndiceo.Data.Models
{
    public class DiscussionReply
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int DiscussionId { get; set; }
        public virtual Discussion Discussion { get; set; }

        public string UserId { get; set; }
        public virtual SyndiceoWebUser User { get; set; } 
    }
}
