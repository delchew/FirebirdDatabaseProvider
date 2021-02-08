using System.Text;

namespace FirebirdDatabaseProvider.Extensions
{
    internal static class StringExtensions
    {
        internal static bool FBIdentifierLengthIsTooLong(this string identifier)
        {
            return Encoding.UTF8.GetByteCount(identifier) > 31;
        }
    }
}
