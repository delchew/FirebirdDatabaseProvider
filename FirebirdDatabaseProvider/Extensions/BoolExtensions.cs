namespace FirebirdDatabaseProvider.Extensions
{
    internal static class BoolExtensions
    {
        /// <summary>
        /// Метод используется для преобразования типа bool в целочисленный булевый тип для базы данны FireBird. false = 0, true = 1
        /// </summary>
        /// <param name="boolValue">значение для преобразования</param>
        /// <returns></returns>
        internal static int ToFireBirdDBBoolInt(this bool boolValue)
        {
            return boolValue ? 1 : 0;
        }
    }

}
