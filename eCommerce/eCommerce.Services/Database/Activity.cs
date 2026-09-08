using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eCommerce.Model.Enums;

namespace eCommerce.Services.Database
{
    public class Activity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public int? AnimalId { get; set; }

        [ForeignKey("AnimalId")]
        public Animal? Animal { get; set; }

        public ActivityType Type { get; set; }

        public TimeSpan Duration { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int Capacity { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public ActivityStatus Status { get; set; } = ActivityStatus.Draft;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    }
}
