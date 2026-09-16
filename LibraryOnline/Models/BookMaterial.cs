using System;
using System.ComponentModel.DataAnnotations;

namespace LibraryOnline.Models
{
    // SQL-query DTO, intentionally not an EF entity: additive schema preserves the
    // original Code First model hash and all existing databases/loan history.
    public class BookMaterial
    {
        public int BookId { get; set; }
        public string AuthorBiography { get; set; }
        public string WorkIntroduction { get; set; }
        public string PublisherInformation { get; set; }
        public string PreviewText { get; set; }
        public string FullText { get; set; }
        public bool IsPublished { get; set; }
        public bool IsDemo { get; set; }
        public DateTime UpdatedAt { get; set; }
        public byte[] Version { get; set; }
    }

    public class MaterialInput
    {
        [StringLength(6000)]
        public string AuthorBiography { get; set; }

        [StringLength(6000)]
        public string WorkIntroduction { get; set; }

        [StringLength(6000)]
        public string PublisherInformation { get; set; }

        [StringLength(6000)]
        public string PreviewText { get; set; }

        [StringLength(500000)]
        public string FullText { get; set; }
        public bool IsPublished { get; set; }
        public bool IsDemo { get; set; }
        public string Version { get; set; }
    }
}
