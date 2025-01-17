using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Jint
{
    /// <summary>
    /// Represents a key that Jint uses with pre-calculated hash code
    /// as runtime does a lot of repetitive dictionary lookups.
    /// </summary>
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    internal readonly struct Key : IEquatable<Key>
    {
        private Key(string name)
        {
            Name = name;
            HashCode = StringComparer.Ordinal.GetHashCode(name);
        }

        internal readonly string Name;
        internal readonly int HashCode;

        public static implicit operator Key(string name)
        {
            return new Key(name);
        }

        public static implicit operator string(Key key) => key.Name;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Key a, in Key b)
        {
            return a.HashCode == b.HashCode && string.Equals(a.Name, b.Name, StringComparison.Ordinal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Key a, in Key b)
        {
            return a.HashCode != b.HashCode || !string.Equals(a.Name, b.Name, StringComparison.Ordinal);
        }

        public static bool operator ==(in Key a, string b)
        {
            return string.Equals(a.Name, b, StringComparison.Ordinal);
        }

        public static bool operator !=(in Key a, string b)
        {
            return !string.Equals(a.Name, b, StringComparison.Ordinal);
        }

        public bool Equals(Key other)
        {
            return HashCode == other.HashCode && string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is Key other && Equals(other);
        }

        public override int GetHashCode() => HashCode;

        public override string ToString() => Name;
    }
}
