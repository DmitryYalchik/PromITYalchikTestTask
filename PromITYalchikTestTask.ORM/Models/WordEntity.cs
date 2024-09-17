using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PromITYalchikTestTask.SqlClient.Models
{
    public class WordEntity
    {
        public Guid Id { get; set; }
        public string Word { get; set; } = default!;
        public int Count { get; set; } = default!;
    }
}
