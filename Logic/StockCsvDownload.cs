using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using MySql.Data.MySqlClient;

namespace stockDataImporter.Logic
{
    internal class StockCsvDownload
    {
        private readonly string _connectionString;

        public StockCsvDownload(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// CSVデータ作成要求処理
        /// </summary>
        /// <param name="viewName"></param>
        /// <param name="outputFilePath"></param>
        /// <returns></returns>

        public async Task DownloadAsync(string viewName, string outputFilePath)
        {
            var dataTable = await GetStockData(viewName);
            Convert2Csv(dataTable, outputFilePath);
        }

        /// <summary>
        /// 有効在庫データ取得・データテーブルへの複製処理
        /// </summary>
        /// <param name="viewName"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private async Task<DataTable> GetStockData(string viewName)
        {

            // DB接続とSELECTクエリ実行、DataTable格納処理
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string getStockQuery = $"SELECT 商品コード, 有効在庫数 FROM {viewName}";

            await using var cmd = new MySqlCommand(getStockQuery, connection);

            using var adapter = new MySqlDataAdapter(cmd);
            var dataTable = new DataTable();

            adapter.Fill(dataTable);


            throw new NotImplementedException();
        }

        /// <summary>
        /// CSV形式に変換処理
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="outputFilePath"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Convert2Csv(DataTable dataTable, string outputFilePath)
        {
            var sb = new StringBuilder();
            var header = dataTable.Columns.Cast<DataColumn>()
                .Select(column => $"\"{column.ColumnName.Replace("\"", "\"\"")}\"");
            sb.AppendLine(string.Join(", ", header));

            foreach (DataRow row in dataTable.Rows)
            {
                var fields = row.ItemArray.Select(field =>
                {
                    if (field == null || field == DBNull.Value) return "";

                    string fieldString = field.ToString();
                    return $"\"{fieldString.Replace("\"", "\"\"")}\"";
                });

                sb.AppendLine(string.Join(",", fields));

                // CSV文字列とファイル書込み処理
                throw new NotImplementedException();
            }

        }
    }
}
