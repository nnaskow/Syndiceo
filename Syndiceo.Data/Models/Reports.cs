using SyndiceoWeb.Areas.Identity.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Syndiceo.Data.Models
{
    public class Report
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int EntranceId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsResolved { get; set; } = false;
        public bool isEdited { get; set; } = false;

        public virtual SyndiceoWebUser User { get; set; }
        public virtual Entrance Entrance { get; set; }
    }
}

