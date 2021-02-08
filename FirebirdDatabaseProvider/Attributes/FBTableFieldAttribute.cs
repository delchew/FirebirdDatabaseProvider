using System;
namespace FirebirdDatabaseProvider.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FBTableFieldAttribute : Attribute
    {
        /// <summary>
        /// Имя поля таблицы
        /// </summary>
        public string TableFieldName { get; set; }

        /// <summary>
        /// Тип поля таблицы
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Маркер: является ли поле первичным ключом
        /// </summary>
        public bool IsPrymaryKey { get; set; }

        /// <summary>
        /// Маркер: является ли поле обязательным к заполнению
        /// </summary>
        public bool IsNotNull { get; set; }
    }
}
