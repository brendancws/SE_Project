using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RROMS.Models
{
    public class MenuCategory
    {
        [Key]
        public int CategoryID { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Admin compatibility aliases.
        [NotMapped]
        public int Id
        {
            get => CategoryID;
            set => CategoryID = value;
        }

        [NotMapped]
        public string Name
        {
            get => CategoryName;
            set => CategoryName = value;
        }

        // These two fields did not exist in the integrated schema.
        // They allow the inherited Admin views to compile, but are not persisted.
        [NotMapped]
        public int DisplayOrder { get; set; }

        [NotMapped]
        public bool IsActive { get; set; } = true;
    }
}