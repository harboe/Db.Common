namespace Db.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    static class DbConvert
    {
        public static T Change<T>(object value, T defaultValue = default(T))
        {
            if (IsEnum(typeof(T)))
                return GetEnumValue<T>(value, defaultValue);
            else
                return GetDefaultValue<T>(value, defaultValue);
        }

        private static bool IsEnum(Type type)
        {
            return type.BaseType == typeof(Enum);
        }

        private static T GetDefaultValue<T>(object value, T defaultValue)
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        private static T GetEnumValue<T>(object value, T defaultValue)
        {
            try
            {
                if (value == null)
                    value = Enum.GetNames(typeof(T)).FirstOrDefault();

                return (T)Enum.Parse(typeof(T), value.ToString());
            }
            catch
            {
                return defaultValue;
            }

        }
    }
}
