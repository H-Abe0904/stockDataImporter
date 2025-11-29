using System;
using System.Collections.Generic;
using System.Text;
using Org.BouncyCastle.Asn1.X509.Qualified;

namespace stockDataImporter.Logic
{
	/// <summary>
	/// インポート順序
	/// </summary>
	public enum ImportOrders
	{
		LzStockData = 1,        // 1: LZ在庫データ
		backOrders = 2,         // 2: DJN売上残データ
	}

	/// <summary>
	/// インポート設定
	/// </summary>
	public struct ImportConfig
	{
		public string FileName { get; }
		public string TableName { get; }

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="fileName">baseDirに存在するファイル名</param>
		/// <param name="tableName">インポート先のテーブル名</param>
		public ImportConfig(string fileName, string tableName)
		{
			FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
			TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
		}
	}

	/// <summary>
	/// インポート設定マップ
	/// </summary>
	public static class ImportConfigMap
	{
		public const string StockBaseDir = @"\\\\habe11-testenv\\daijin_test\\stockData";
		public const string OdrBaseDir = @"\\\\habe11-daijin\\daijin_test\\backOrders";
		public static readonly Dictionary<ImportOrders, ImportConfig> Map = new Dictionary<ImportOrders, ImportConfig>
		{
			{ ImportOrders.LzStockData, new ImportConfig(Path.GetFileName(StockBaseDir), "LzStockData") },
			{ ImportOrders.backOrders, new ImportConfig(Path.GetFileName(OdrBaseDir), "backOrders") },
		};
		
		/// <summary>
		/// [デバッグ用]インポート順序リスト取得
		/// </summary>
		/// <returns></returns>
		public static List<ImportOrders> GetImportOrders()
		{
			return Enum.GetValues(typeof(ImportOrders))
			.Cast<ImportOrders>()
			.OrderBy(x => (int)x)
			.ToList();
		}
	}
}
