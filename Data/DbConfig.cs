using System;
using System.Configuration;

namespace colizeumUpdateManager.Data
{
    internal static class DbConfig
    {
        public static string ConnectionString
        {
            get
            {
                // Строка подключения к бд, для смены сервера без повторной сборки приложения.
                var cs = ConfigurationManager.ConnectionStrings["MainDb"]?.ConnectionString;
                if (!string.IsNullOrWhiteSpace(cs))
                    return cs;

                throw new InvalidOperationException(
                    "Нет строки подключения: задай COLIZEUM_DB или connectionStrings/MainDb в App.config.");
            }
        }
    }
}