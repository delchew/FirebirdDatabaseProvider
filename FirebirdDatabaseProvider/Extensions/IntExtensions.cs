using System;

namespace FirebirdDatabaseProvider.Extensions
{
    internal static class IntExtensions
    {
        /// <summary>
        /// Метод используется для преобразования целочисленного булевого типа из базы данны FireBird в bool. 0 = false, 1 = true, любое другое значение выбросит исключение
        /// </summary>
        /// <param name="value">Значение из базы данных</param>
        /// <returns></returns>
        internal static bool ToBool(this int value)
        {
            if (value == 0) return false;
            if (value == 1) return true;
            throw new ArgumentException("Метод принимает только 0 = false или 1 = true!");
        }
    }
}
