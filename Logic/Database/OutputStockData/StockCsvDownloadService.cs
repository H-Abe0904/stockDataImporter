using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using MySql.Data.MySqlClient;
using stockDataImporter.Logic.Messaging;

namespace stockDataImporter.Logic.ImportStockData
{
    public class StockCsvDownload : IStockCsvDownloaderService
    {
        private readonly string _connectionString;
        private readonly StockExportSettings _stockExportSettings;
        private readonly CopyStockDataSettings _copyStockDataSettings;
        private readonly IEmailService _emailService;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="connectionString">DB接続情報</param>
        /// <param name="stockExportSettings">在庫データ出力設定</param>
        public StockCsvDownload(string connectionString, StockExportSettings stockExportSettings, IEmailService emailService, CopyStockDataSettings copyStockDataSettings)
        {
            _connectionString = connectionString;
            _stockExportSettings = stockExportSettings;
            _copyStockDataSettings = copyStockDataSettings;
            _emailService = emailService;
        }

        /// <summary>
        /// 在庫CSVダウンロード処理
        /// </summary>
        /// <returns></returns>
        public async Task DownloadStockCsvAsync()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string fileName = $"stock_{timestamp}.csv";
            string outputFilePath = System.IO.Path.Combine(_stockExportSettings.ExportFileNameFormat, fileName);
            if (_stockExportSettings == null)
                throw new NullReferenceException("_stockExportSettings 自体が注入されていません。");

            if (string.IsNullOrEmpty(_stockExportSettings.ViewName))
                throw new NullReferenceException("StockExportSettings.ViewName が空です。JSONのキー名を確認してください。");

            if (string.IsNullOrEmpty(_stockExportSettings.ExportFileNameFormat))
                throw new NullReferenceException("StockExportSettings.ExportFileNameFormat が空です。");
            try
            {
                Console.WriteLine("在庫CSVダウンロード処理開始");
                await DownloadAsync(_stockExportSettings.ViewName, outputFilePath);
                Console.WriteLine("在庫CSVダウンロード処理完了");

                // コピー先へファイルをコピー(上書き保存)
                File.Copy(outputFilePath, _stockExportSettings.CopyTargetDir, true);    // \\chuo3\edi\data\sys\stock_forEC.csv
                // File.Copy(outputFilePath, _copyStockDataSettings.AscensusPath, true);   // F:\中央漁具株式会社 Dropbox\中央漁具EDI\000008_アシェンサスジャパン\stock.csv
                // File.Copy(outputFilePath, _copyStockDataSettings.CustomerPath, true);   // \\chuo3\edi\data\sys\djn_stock_forCustomer.csv
            }
            catch (Exception ex)
            {
                Console.WriteLine($"在庫CSVダウンロードエラー: {ex.Message}");
                await _emailService.SendErrorMailAsync("在庫CSVダウンロードエラー", $"在庫CSVダウンロードエラーが発生しました。\nエラー内容: {ex.Message} \n{ex.InnerException?.StackTrace}", "Debug");
                throw;
            }
            
        }

        /// <summary>
        /// CSVデータ作成要求処理
        /// </summary>
        /// <param name="viewName">MySQLの在庫出力ビューテーブル名</param>
        /// <param name="outputFilePath">出力先ファイルパス</param>
        /// <returns></returns>
        public async Task DownloadAsync(string viewName, string outputFilePath)
        {
            var dataTable = await GetStockData(viewName);
            Convert2Csv(dataTable, outputFilePath);
        }

        /// <summary>
        /// 有効在庫データ取得・データテーブルへの複製処理
        /// </summary>
        /// <param name="viewName">MySQLの在庫出力ビューテーブル名</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private async Task<DataTable> GetStockData(string viewName)
        {
            // DB接続とSELECTクエリ実行、DataTable格納処理
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string getStockQuery = $"SELECT 商品コード AS JANCode, 有効在庫数 AS 引当可能数 FROM {viewName}";

            await using var cmd = new MySqlCommand(getStockQuery, connection);
            using var adapter = new MySqlDataAdapter(cmd);
            var dataTable = new DataTable();

            adapter.Fill(dataTable);
            return dataTable;
        }

        /// <summary>
        /// CSV形式に変換処理
        /// </summary>
        /// <param name="dataTable">在庫データを含むDataTable</param>
        /// <param name="outputFilePath">出力先ファイルパス</param>
        /// <exception cref="NotImplementedException"></exception>
        private async void Convert2Csv(DataTable dataTable, string outputFilePath)
        {
            var sb = new StringBuilder();
            var header = dataTable.Columns.Cast<DataColumn>()
                .Select(column => $"\"{column.ColumnName.Replace("\"", "\"\"")}\"");
            sb.AppendLine(string.Join(",", header));

            foreach (DataRow row in dataTable.Rows)
            {
                var fields = row.ItemArray.Select(field =>
                {
                    if (field == null || field == DBNull.Value) return "\"\"";
                    string fieldString = field.ToString() ?? "";

                    return $"\"{fieldString.Replace("\"", "\"\"")}\"";
                });

                sb.AppendLine(string.Join(",", fields));

            }
            try
            {
                Console.WriteLine($"在庫データCSVファイル書込処理中: {outputFilePath}");
                // CSV文字列とファイル書込み処理
                File.WriteAllText(outputFilePath, sb.ToString(), Encoding.GetEncoding("Shift_JIS"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"在庫データCSV書込エラー: {ex.Message}");
                await _emailService.SendErrorMailAsync("在庫データCSV書込エラー", $"在庫データCSV書込エラーが発生しました。\nエラー内容: {ex.Message} \n{ex.InnerException?.StackTrace}", "Debug");
                throw;
            }

        }
    }
}
