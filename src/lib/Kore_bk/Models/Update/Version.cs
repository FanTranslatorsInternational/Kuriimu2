using System;
using System.Text.RegularExpressions;

namespace Kore.Models.Update
{
    public class Version
    {
        private static readonly Regex VersionRegex = new Regex(@"(\d+)\.(\d+)\.(\d+)");

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
            if (!VersionRegex.IsMatch(version))
                throw new InvalidOperationException("The given version is not of pattern '0.0.0'");

            var versionMatch = VersionRegex.Match(version);

            Major = int.Parse(versionMatch.Groups[1].Value);
            Minor = int.Parse(versionMatch.Groups[2].Value);
            Patch = int.Parse(versionMatch.Groups[3].Value);
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

        public static bool operator ==(Version v1, Version v2)=> v1.Major == v2.Major && v1.Minor == v2.Minor && v1.Patch == v2.Patch;
        public static bool operator !=(Version v1, Version v2)=> v1.Major != v2.Major || v1.Minor != v2.Minor || v1.Patch != v2.Patch;
    }
}
