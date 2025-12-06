using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;

namespace stockDataImporter.Logic
{
    /// <summary>
	/// MySQLデータローダー
	/// </summary>
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
        /// <param name="fileName">取込対象CSVファイル</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">指定ファイル名が存在しなかった時</exception>
        public async Task ExecuteSPAsync(string fileName)
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // ファイル名に基づいてストアドプロシージャ名を決定
            var spName = fileName switch
            {
                _ when fileName.Contains("LzStockData") => "ImportLzStockDataFromCsv",  // LZ在庫データ取込用プロシージャについては仮名
                _ when fileName.Contains("backOrders") => "import_djn_orders",          // 受注伝票データ取込用プロシージャ
                _ => throw new ArgumentException($"不明なファイル名: {fileName}"),      // 不明なファイル名の場合のエラー処理
            };

            // ストアドプロシージャの実行
            await using var command = new MySqlCommand(spName, connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@fileName", fileName);     // ストアドプロシージャのパラメータ名に合わせる

            try
            {
                Console.WriteLine($"{fileName}");
                var result = await command.ExecuteNonQueryAsync();

                //Console.WriteLine($"プロシージャ名: '{spName}'は正常に実行されました. 結果: {result}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"プロシージャ名: '{spName}'の実行中にエラーが発生しました. エラー内容: {ex.Message}");
                throw;
            }

        }
    }
}
