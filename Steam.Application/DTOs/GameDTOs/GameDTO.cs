using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Application.DTOs.GameDTOs;

public class GameDTO
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Guid DeveloperId { get; set; }
    public string DeveloperName { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string? BannerImageUrl { get; set; }
    public DateTime ReleaseDate { get; set; }
    public IEnumerable<Steam.Application.DTOs.MediaDTOs.MediaDTO> Media { get; set; } = [];
    public IEnumerable<Steam.Application.DTOs.SystemRequirementsDTOs.SystemRequirementsDTO> SystemRequirements { get; set; } = [];
    public IEnumerable<Steam.Application.DTOs.AchievementDTOs.AchievementDTO> Achievements { get; set; } = [];
}
