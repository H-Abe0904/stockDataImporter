using System;
using System.Collections.Generic;
using System.Text;
using Org.BouncyCastle.Asn1.X509.Qualified;

namespace stockDataImporter.Logic.ImportStockData
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
	public readonly struct ImportConfig
	{
		public string FileName { get; }

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="fileName">baseDirに存在するファイル名</param>
		/// <param name="tableName">インポート先のテーブル名</param>
		public ImportConfig(string fileName)
		{
			FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
		}
	}

	/// <summary>
	/// インポート設定マップ
	/// </summary>
	public static class ImportConfigMap
	{
		// 在庫データの保存ディレクトリ
		public static string[] StockBaseDir = Directory.GetFiles(@"\\cgspider\DataSpiderServista\server\data\DataLink\LogiExp\stock", "*.csv");
		public const string OdrBaseDir = @"\\cgspider\DataSpiderServista\server\data\DataLink\DjExp\BKODR\backOrders.csv";
		public static readonly Dictionary<ImportOrders, ImportConfig> Map = new Dictionary<ImportOrders, ImportConfig>
		{
			{ ImportOrders.LzStockData, new ImportConfig(Path.GetFileName(StockBaseDir[0])) },	//	1件しか存在しない設計なので0番目のファイル名固定
			{ ImportOrders.backOrders, new ImportConfig(Path.GetFileName(OdrBaseDir)) },
		};

		/// <summary>
		/// [デバッグ用]インポート順序リスト取得
		/// </summary>
		/// <returns></returns>
		public static List<ImportOrders> GetImportOrders()
		{
			return [.. Enum.GetValues<ImportOrders>()
			.Cast<ImportOrders>()
			.OrderBy(x => (int)x)];
		}
	}
}
