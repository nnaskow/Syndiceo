using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Syndiceo.Data.Models
{
    public class Discussion
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsClosed { get; set; }

        public virtual ICollection<DiscussionReply> Replies { get; set; }
    }
}
