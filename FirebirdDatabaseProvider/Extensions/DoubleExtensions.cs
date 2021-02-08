namespace FirebirdDatabaseProvider.Extensions
{
    internal static class DoubleExtensions
    {
        internal static string ToFBSqlString(this double number)
        {
            var str = number.ToString();
            return str.Replace(',', '.');
        }
    }
}
