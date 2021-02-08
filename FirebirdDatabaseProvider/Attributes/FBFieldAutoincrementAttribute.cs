using System;

namespace FirebirdDatabaseProvider.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FBFieldAutoincrementAttribute : Attribute
    {
        /// <summary>
        /// Имя генератора автоинкремента
        /// </summary>
        public string GeneratorName { get; set; }
    }
}
