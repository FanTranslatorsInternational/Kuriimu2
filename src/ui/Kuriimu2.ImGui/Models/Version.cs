using System;
using System.Text.RegularExpressions;

namespace Kuriimu2.ImGui.Models
{
    public partial class Version
    {
        [GeneratedRegex(@"(\d+)\.(\d+)\.(\d+)", RegexOptions.Compiled)]
        private static partial Regex VersionRegex();

        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }

        public Version(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public Version(string version)
        {
            if (!VersionRegex().IsMatch(version))
                throw new InvalidOperationException("The given version is not of pattern 'x.x.x'.");

            var versionMatch = VersionRegex().Match(version);

            Major = int.Parse(versionMatch.Groups[1].Value);
            Minor = int.Parse(versionMatch.Groups[2].Value);
            Patch = int.Parse(versionMatch.Groups[3].Value);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Version)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Major, Minor, Patch);
        }

        public static bool operator <=(Version v1, Version v2)
        {
            if (v1.Major > v2.Major)
                return false;

            if (v1.Major < v2.Major)
                return true;

            if (v1.Minor > v2.Minor)
                return false;

            if (v1.Minor < v2.Minor)
                return true;

            return v1.Patch <= v2.Patch;
        }
        public static bool operator >=(Version v1, Version v2)
        {
            if (v1.Major < v2.Major)
                return false;

            if (v1.Major > v2.Major)
                return true;

            if (v1.Minor < v2.Minor)
                return false;

            if (v1.Minor > v2.Minor)
                return true;

            return v1.Patch >= v2.Patch;
        }

        public static bool operator <(Version v1, Version v2)
        {
            if (v1.Major > v2.Major)
                return false;

            if (v1.Major < v2.Major)
                return true;

            if (v1.Minor > v2.Minor)
                return false;

            if (v1.Minor < v2.Minor)
                return true;

            return v1.Patch < v2.Patch;
        }
        public static bool operator >(Version v1, Version v2)
        {
            if (v1.Major < v2.Major)
                return false;

            if (v1.Major > v2.Major)
                return true;

            if (v1.Minor < v2.Minor)
                return false;

            if (v1.Minor > v2.Minor)
                return true;

            return v1.Patch > v2.Patch;
        }

        public static bool operator ==(Version v1, Version v2) => v1.Major == v2.Major && v1.Minor == v2.Minor && v1.Patch == v2.Patch;
        public static bool operator !=(Version v1, Version v2) => v1.Major != v2.Major || v1.Minor != v2.Minor || v1.Patch != v2.Patch;

        protected bool Equals(Version other)
        {
            return Major == other.Major && Minor == other.Minor && Patch == other.Patch;
        }
    }
}
