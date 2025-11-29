using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;

namespace stockDataImporter.Logic
{
    public class MySqlDataLoader
    {
        private readonly string _connectionString;
        /// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="connectionString">在庫連携用DBサーバへの接続クエリ</param>
		/// <exception cref="ArgumentNullException">nullの場合のエラー処理</exception>
        public MySqlDataLoader(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// ストアドプロシージャ実行
        /// </summary>
        /// <param name="spName">MySQLのプロシージャ名</param>
        /// <param name="fileName">取込対象のCSVファイル名</param>
        /// <param name="tableName">取込先テーブル名</param>
        /// <returns></returns>
        public async Task ExecuteSPAsync(string spName, string fileName, string tableName)
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new MySqlCommand(spName, connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@fileName", fileName);
            command.Parameters.AddWithValue("@tableName", tableName);

            try
            {
                var result = await command.ExecuteScalarAsync();
                Console.WriteLine($"プロシージャ名: '{spName}'は正常に実行されました. 結果: {result}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"プロシージャ名: '{spName}'の実行中にエラーが発生しました. エラー内容: {ex.Message}");
                throw;
            }

        }
    }
}
