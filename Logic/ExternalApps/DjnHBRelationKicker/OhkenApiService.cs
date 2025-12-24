using System.Diagnostics;
namespace stockDataImporter.Logic
{
	public class OhkenApiService : IOhkenApiService
	{
		private readonly string _exePath;

		public OhkenApiService(string exePath)
		{
			_exePath = exePath ?? throw new ArgumentNullException(nameof(exePath));
		}

		/// <summary>
		/// 受注伝票データ取得処理
		/// </summary>
		/// <returns></returns>
		public async Task FetchBackOrdersAsync()
	=> await StartOhkenProcessAsync("/co:1 /data:1 /code:4109 /winlogin:False");

		/// <summary>
		/// 受注伝票データ取得処理
		/// </summary>
		/// <returns>ステータスが「完了」以外の受注伝票</returns>
		/// <exception cref="Exception"></exception>
		public async Task StartOhkenProcessAsync(string arguments)
		{
			await Task.Run(() =>
			{
				var app = new ProcessStartInfo
				{
					FileName = _exePath,
					Arguments = arguments, // 受注伝票データ取得用引数,
					UseShellExecute = true
				};

				using var process = Process.Start(app);
				process?.WaitForExit();

				if (process?.ExitCode != 0)
				{
					throw new Exception("自動実行ツールが異常終了しました。" + process?.ExitCode);
				}
			});

		}
	}
}
