using Syndiceo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Syndiceo.Utilities
{
    public class TaxesTemplate
    {
        public string Name { get; set; }                 // име на шаблона
        public List<TransactionViewModel> Incomes { get; set; }
        public List<TransactionViewModel> Expenses { get; set; }
        public decimal Cashbox { get; set; }

      
    }

}
