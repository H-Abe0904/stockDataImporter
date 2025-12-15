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
                //  LZ在庫データは取込時に「商品名」と「検索名称」のカラムを対象外とする
                _ when fileName.Contains("LzStockData") => $@"LOAD DATA INFILE '{stockDataPath.Replace(@"\", @"\\")}'
                INTO TABLE dbwrk_lz_stock
                FIELDS TERMINATED BY ','
                ENCLOSED BY '""'
                LINES TERMINATED BY '\r\n'
                IGNORE 1 LINES
                (
                    契約者ID, 契約者名, 荷主ID, 荷主名, 倉庫ID, 倉庫名, ブロックID, ブロック略称, ロケーション, 入荷日,
                    商品ID, @dummy, @dummy,
                    品質区分ID, 品質区分, ロット, 有効期限, 在庫キー1, 在庫キー1名称, 在庫キー2,
                    在庫キー2名称, 在庫キー3, 在庫キー3名称, 在庫キー4, 在庫キー4名称,
                    在庫数_引当数含む, 引当数, バーコード, バラ, ボール, ケース,
                    ロケ引当条件, ロケ業務区分, 取置取引先ID, 取置取引先名, 梱包形態, 棚卸状況, 検索名称2, 仕入単価,
                    小売価格, 小売価格2, 小売価格3, 小売価格4, 小売価格5, 大分類, 中分類, 小分類, ロット管理フラグ, 有効期限区分, 入荷日管理フラグ,
                    温度帯区分, セット構成区分, 在庫区分, 引当不可日数, 入荷期限日数, 商品予備項目001, 商品予備項目002, 商品予備項目003,
                    商品予備項目004, 商品予備項目005, 商品予備項目006, 商品予備項目007, 商品予備項目008, 商品予備項目009, 商品予備項目010,
                    部門ID, 部門名, 画像URL, 英語名, 重量, シリアル登録フラグ, HT検品対象除外フラグ, 納品書出力対象除外ファイル,
                    発注点管理除外フラグ, 最終入荷日, 最終出荷日, 縦_SKU単位, 横_SKU単位, 高さ_SKU単位, `重量(SKU単位)`, 仕入単価_SKU単位,
                    小売価格_SKU単位, 小売価格2_SKU単位, 小売価格3_SKU単位, 小売価格4_SKU単位, 小売価格5_SKU単位,
                    SKU予備項目001, SKU予備項目002, SKU予備項目003, SKU予備項目004, SKU予備項目005, SKU予備項目006, SKU予備項目007, SKU予備項目008,
                    SKU予備項目009, SKU予備項目010, ブロックグループID, ブロックグループ名称, 引当条件ID, 業務区分ID, 梱包形態ID, 棚卸状況フラグ
                );", 

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
