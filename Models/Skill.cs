using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoScheduling3.Models
{
    internal class Skill
    {
        // Database id
        public int Id { get; set; }

        // Skill name
        public string Name { get; set; } = string.Empty;

        // Skill description
        public string Description { get; set; } = string.Empty;
    }
}
