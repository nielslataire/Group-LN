using System.Collections.Generic;
using BOCore;

namespace CPMCore.Models.Instellingen;

public class ActivitySettingsViewModel
{
    public List<ActivityBO> Activities { get; set; } = new();
    public List<ActivityGroupBO> ActivityGroups { get; set; } = new();
}