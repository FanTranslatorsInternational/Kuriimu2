using System;

namespace Kuriimu2.Cmd.Models
{
    internal class ExportOptions
    {
        public required string Input { get; init; }
        public string? Output { get; set; }
        public bool SubDirectories { get; set; }
        public Guid? PluginId { get; set; }
        public Guid? GamePluginId { get; set; }
        public string[]? DialogOptions { get; set; }
        public string? Type { get; set; }
    }
}
