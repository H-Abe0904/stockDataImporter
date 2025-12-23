using System;
using System.Collections.Generic;
using System.IO.Enumeration;
using System.Runtime.CompilerServices;
using System.Text;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;

namespace stockDataImporter.Logic
{
	public class MasterImportService : IMasterImportService
	{

		private readonly string _connectionString;
		private readonly ImportMSTPathSettings _importMSTPathSettings;
		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="connectionString">在庫連携用DBサーバへの接続クエリ</param>
		/// <param name="filePath">ファイルパス</param>
		/// <param name="tableName">テーブル名</param>
		/// <exception cref="ArgumentNullException">nullの場合のエラー処理</exception>
		public MasterImportService(string connectionString, ImportMSTPathSettings importMSTPathSettings)
		{
			_connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
			_importMSTPathSettings = importMSTPathSettings ?? throw new ArgumentNullException(nameof(importMSTPathSettings));
		}

		/// <summary>
		/// マスターデータ取込み処理
		/// </summary>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public Task ImportMasterDataAsync()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// マスターデータ取込み処理
		/// </summary>
		/// <param name="filePath">取込元ファイルパス</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">不明なファイル名の場合のエラー処理</exception>
		private async Task ImportMSTData(string filePath)
		{
			// 実際のデータ取込みロジックをここに実装

			// 取込ファイルパスの判定
			string targetFilePath = filePath switch
			{
				_ when filePath.Contains("商品マスタ") => _importMSTPathSettings.ImportMSTPath,
				_ => throw new ArgumentException($"不明なファイル名: {filePath}"),      // 不明なファイル名の場合のエラー処理
			};

			string escapedPath = targetFilePath.Replace(@"\", @"\\");
			await using var connection = new MySqlConnection(_connectionString);
			await connection.OpenAsync();

			// ファイル名に基づいて適切なテーブルのレコードを削除し、
			// データをロードするクエリを選択
			string truncateQuery = filePath switch
			{
				_ when filePath.Contains("商品マスタ") => "TRUNCATE TABLE dbmst_djn_products",
				_ => throw new ArgumentException($"不明なファイル名: {filePath}"),      // 不明なファイル名の場合のエラー処理
			};

			// マスターデータ取込みクエリ実行
			string loadDataQuery = filePath switch
			{
				_ when filePath.Contains("商品マスタ") => $@"
				LOAD DATA LOCAL INFILE '{escapedPath}'
				INTO TABLE dbmst_djn_products
				CHARACTER SET cp932
				FIELDS TERMINATED BY ','
				ENCLOSED BY '""'
				LINES TERMINATED BY '\r\n'
				IGNORE 1 LINES;",
				_ => throw new ArgumentException($"不明なファイル名: {filePath}"),      // 不明なファイル名の場合のエラー処理
			};

			await using var truncateCmd = new MySqlCommand(truncateQuery, connection);
			await truncateCmd.ExecuteNonQueryAsync();

			await using var loadCmd = new MySqlCommand(loadDataQuery, connection);
			loadCmd.CommandTimeout = 600; // タイムアウト時間を10分に設定
			var result = await loadCmd.ExecuteNonQueryAsync();

			try
			{
				Console.WriteLine($"マスターデータ取込み完了: {filePath}, 取込件数: {result}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"マスターデータ取込みエラー: {ex.Message}");
			}
		}

	}
}