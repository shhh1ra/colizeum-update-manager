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
                // 1) ENV (удобно для docker / сервера)
                var env = Environment.GetEnvironmentVariable("COLIZEUM_DB");
                if (!string.IsNullOrWhiteSpace(env))
                    return env;

                // 2) App.config / exe.config (удобно для “поменять без сборки”)
                var cs = ConfigurationManager.ConnectionStrings["MainDb"]?.ConnectionString;
                if (!string.IsNullOrWhiteSpace(cs))
                    return cs;

                throw new InvalidOperationException(
                    "Нет строки подключения: задай COLIZEUM_DB или connectionStrings/MainDb в App.config.");
            }
        }
    }
}