using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Syndiceo.Data.Models
{
    public class TransactionViewModel
    {
        public decimal Amount { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
    }
}
