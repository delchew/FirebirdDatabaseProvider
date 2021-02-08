namespace FirebirdDatabaseProvider.Extensions
{
    internal static class DecimalExtensions
    {
        internal static string ToFBSqlString(this decimal number)
        {
            var str = number.ToString();
            return str.Replace(',', '.');
        }
    }
}
