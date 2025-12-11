using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;

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


        public async Task ExecuteQueryAsync(string fileName)
        {
            string stockDataPath = @"D:\daijin_test\stockData\stock.csv";
            string backOrderPath = @"D:\daijin_test\backOrders\backOrders.csv";

            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // ファイル名に基づいて適切なテーブルのレコードを削除し、データをロードするクエリを選択
            string truncateQuery = fileName switch
            {
                _ when fileName.Contains("LzStockData") => "TRUNCATE TABLE dbwrk_lz_stock",            
                _ when fileName.Contains("backOrders") => "TRUNCATE TABLE dbwrk_djnodr_flat",          
                _ => throw new ArgumentException($"不明なファイル名: {fileName}"),      // 不明なファイル名の場合のエラー処理
            };

            // データロードクエリの選択
            string loadQuery = fileName switch
            {
                _ when fileName.Contains("LzStockData") => "LOAD DATA INFILE @fileName INTO TABLE dbwrk_lz_stock FIELDS TERMINATED BY ',' ENCLOSED BY '\"' LINES TERMINATED BY '\r\n' IGNORE 1 LINES;", // LZ在庫データ取込用プロシージャについては仮名
                
                _ when fileName.Contains("backOrders") => $@"LOAD DATA INFILE '{backOrderPath.Replace(@"\", @"\\")}'
                INTO TABLE dbwrk_djnodr_flat
                FIELDS TERMINATED BY ','
                ENCLOSED BY '""'
                LINES TERMINATED BY '\r\n' 
                IGNORE 1 LINES;", // 受注伝票データ一括取込用クエリ
                
                _ => throw new ArgumentException($"不明なファイル名: {fileName}"),      // 不明なファイル名の場合のエラー処理
            };

            // 各テーブルのレコード一括消去
            await using var truncateCmd = new MySqlCommand(truncateQuery, connection)
            {
                CommandType = System.Data.CommandType.Text
            };
            await truncateCmd.ExecuteNonQueryAsync(); // レコード削除実行

            // データファイルのロード
            await using var loadCmd = new MySqlCommand(loadQuery, connection)
            {
                CommandType = System.Data.CommandType.Text
            };

            //loadCmd.Parameters.AddWithValue("@fileName", Path.Combine(Directory.GetCurrentDirectory(), fileName));  

            try
            {
                Console.WriteLine($"{fileName}");
                var result = await loadCmd.ExecuteNonQueryAsync();

                Console.WriteLine($"プロシージャ名: '{loadCmd}'は正常に実行されました. 結果: {result}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"データ追加時にエラーが発生しました。 エラー内容: {ex.Message}");
                throw;
            }

        }
    }
}
