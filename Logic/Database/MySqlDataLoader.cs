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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task ExecuteQueryAsync(string fileName)
        {
            //  各ファイルについてはCSV保存ディレクトリを変更予定 12/12
            string[] StockBaseDir = Directory.GetFiles(@"\\cgspider\DataSpiderServista\server\data\DataLink\LogiExp\stock", "*.csv");
            string stockDataPath = StockBaseDir[0];

            string backOrderPath = @"\\cgspider\DataSpiderServista\server\data\DataLink\DjExp\BKODR\backOrders.csv";

            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // ファイル名に基づいて適切なテーブルのレコードを削除し、データをロードするクエリを選択
            string truncateQuery = fileName switch
            {
                _ when fileName.Contains("stock") => "TRUNCATE TABLE dbwrk_lz_stock",
                _ when fileName.Contains("backOrders") => "TRUNCATE TABLE dbwrk_djnodr_flat",
                _ => throw new ArgumentException($"不明なファイル名: {fileName}"),      // 不明なファイル名の場合のエラー処理
            };

            // データロードクエリの選択
            string loadQuery = fileName switch
            {
                //  LZ在庫データは取込時に「商品名」カラムを対象外とする
                _ when fileName.Contains("stock") => $@"LOAD DATA INFILE '{stockDataPath.Replace(@"\", @"\\")}'
                INTO TABLE dbwrk_lz_stock
                CHARACTER SET cp932
                FIELDS TERMINATED BY ','
                ENCLOSED BY '""'
                LINES TERMINATED BY '\r\n'
                IGNORE 1 LINES
                (
                    ブロックID, ブロック略称, ロケーション,商品ID, @dummy,在庫数_引当数含む, 引当数, 商品予備項目001, 商品予備項目003
);",

                _ when fileName.Contains("backOrders") => $@"SET NAMES cp932; LOAD DATA INFILE '{backOrderPath.Replace(@"\", @"\\")}'
                INTO TABLE dbwrk_djnodr_flat
                CHARACTER SET cp932
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
