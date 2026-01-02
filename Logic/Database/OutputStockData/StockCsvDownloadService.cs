using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Microsoft.VisualBasic;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1;
using stockDataImporter.Logic.Messaging;
using stockDataImporter.Logic.ImportStockData;
using static stockDataImporter.Logic.ImportStockData.StockExportSettings;

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
        /// 全在庫CSVダウンロード処理
        /// </summary>
        /// <returns></returns>
        public async Task DL_AllStockCsv()
        {
            await ExecBySettings("EC");
            await ExecBySettings("Customer");
        }

        /// <summary>
        /// 設定情報に基づく在庫CSVダウンロード処理
        /// </summary>
        /// <param name="config">エクスポート設定</param>
        /// <param name="label">ラベル</param>
        /// <returns>ラベルに応じた在庫CSVファイル</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<string> ExecBySettings(string label)
        {
            // 1. ラベルに応じて使う設定（View名や保存先）を切り替える
            var config = label == "EC"
                ? _stockExportSettings.StockEcDataExportSettings
                : _stockExportSettings.CustomerStockDataExportSettings;

            // 2. 設定が読み込めているかチェック
            if (config == null || string.IsNullOrEmpty(config.ViewName))
            {
                Console.WriteLine($"【エラー】{label}向けの設定が読み込めません。クラス名を確認してください。");
            }

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            //  運用開始時は後者をstock.csvに修正
            string fileName = label == "EC" ? $"stock_{timestamp}.csv" : "stock_Test.csv";

            // 3. 保存先のフォルダが存在するか確認し、なければ作成する
            if (!Directory.Exists(config!.ExportFileNameFormat))
            {
                Console.WriteLine($"フォルダ作成: {config.ExportFileNameFormat}");
                Directory.CreateDirectory(config.ExportFileNameFormat);
            }

            // 4. ファイルのフルパスを組み立てる
            string outputFilePath = Path.Combine(config.ExportFileNameFormat, fileName);

            Console.WriteLine($"{label}向けCSV出力開始 -> View: {config.ViewName}");

            try
            {
                // 5. データを取得してCSVとして保存
                await DownloadAsync(config.ViewName, outputFilePath);
                Console.WriteLine($"{label}向けCSV保存成功: {outputFilePath}");

                //  ECB向けCSVファイルのコピー先ディレクトリの指定
                if (label == "EC" && !string.IsNullOrEmpty(config.CopyTargetDir))
                {
                    string copy2Path = _copyStockDataSettings.SourceFilePath;
                    File.Copy(outputFilePath, copy2Path, true);
                }

                return outputFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{label}向けCSV保存失敗: {ex.Message}");
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
            await Convert2Csv(dataTable, outputFilePath);
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
        private async Task Convert2Csv(DataTable dataTable, string outputFilePath)
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
                await File.WriteAllTextAsync(outputFilePath, sb.ToString(), Encoding.GetEncoding("Shift_JIS"));
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
