はい、承知いたしました。先述の「**TypeScript (Node.js) スクリプトで、順序を保証しながらMySQLのストアドプロシージャを呼び出す**」というビジネスロジックを、C\#（.NET環境）で実装する際のファイル構造とサンプルコードをご紹介します。

C\#では、データアクセス層に **ADO.NET** または **Entity Framework Core (EF Core)** を使用しますが、ストアドプロシージャの呼び出しとファイル順序管理という要件に合わせ、シンプルで直接的な **ADO.NET (MySQL Connector/NET)** を使用したコンソールアプリケーションとして構成します。

-----

## 💻 C\# (.NET Core Console App) ファイル構造

コンソールアプリケーションのプロジェクトとして構成します。

```
/CSharpDataImporter
├── CSharpDataImporter.csproj // プロジェクトファイル
├── appsettings.json          // 設定ファイル (接続文字列など)
├── Program.cs                // メインの実行ロジック
└── Logic/
    ├── ImportOrder.cs        // 処理順序のEnumと設定
    └── MySqlDataLoader.cs    // DB接続とSP実行ロジック
```

-----

## 🛠️ プロジェクトの準備

まず、プロジェクトを作成し、MySQLの接続に必要なNuGetパッケージを追加します。

1.  **プロジェクト作成**

    ```bash
    dotnet new console -n CSharpDataImporter
    cd CSharpDataImporter
    ```

2.  **パッケージの追加**

    ```bash
    dotnet add package MySql.Data
    dotnet add package Microsoft.Extensions.Configuration
    dotnet add package Microsoft.Extensions.Configuration.Json
    ```

-----

## 📄 サンプルコード

### 1\. `appsettings.json` (接続設定)

接続文字列を管理します。

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=your_db_name;Uid=your_user;Pwd=your_password;"
  }
}
```

### 2\. `Logic/ImportOrder.cs` (順序管理)

TypeScriptの`enum`とマッピングの役割を果たします。C\#では、`enum`の定義と、それに対応するデータ構造（`struct`または`class`）を定義します。

```csharp
namespace CSharpDataImporter.Logic
{
    // 処理順序を定義するEnum
    public enum ImportOrder
    {
        FileOne = 1,
        FileTwo = 2
    }

    // 各ファイルの設定を保持する構造体
    public struct ImportConfig
    {
        public string FileName { get; }
        public string TableName { get; }

        public ImportConfig(string fileName, string tableName)
        {
            FileName = fileName;
            TableName = tableName;
        }
    }

    // 設定を静的に保持するクラス
    public static class ImportConfigMap
    {
        // Enumの順序と設定をDictionaryでマッピング
        public static readonly Dictionary<ImportOrder, ImportConfig> Map = new Dictionary<ImportOrder, ImportConfig>
        {
            { ImportOrder.FileOne, new ImportConfig("users_master.csv", "users_temp") },
            { ImportOrder.FileTwo, new ImportConfig("transactions_data.csv", "transactions_temp") }
        };
        
        // 順序付けられたImportOrderのリストを返す
        public static List<ImportOrder> GetOrderedImports()
        {
            return Enum.GetValues(typeof(ImportOrder))
                       .Cast<ImportOrder>()
                       .OrderBy(o => (int)o)
                       .ToList();
        }
    }
}
```

### 3\. `Logic/MySqlDataLoader.cs` (DBアクセス)

`MySql.Data`の `MySqlConnection` と `MySqlCommand` を使用してストアドプロシージャを呼び出します。

```csharp
using MySql.Data.MySqlClient;

namespace CSharpDataImporter.Logic
{
    public class MySqlDataLoader
    {
        private readonly string _connectionString;

        public MySqlDataLoader(string connectionString)
        {
            _connectionString = connectionString;
        }

        /**
         * ストアドプロシージャを実行するメソッド
         * @param spName 呼び出すストアドプロシージャ名
         * @param fileName SPに渡すファイル名の引数
         * @param tableName SPに渡すテーブル名の引数
         */
        public async Task ExecuteStoredProcedureAsync(string spName, string fileName, string tableName)
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new MySqlCommand(spName, connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure // SP呼び出しを指定
            };

            // 引数をSPに追加
            command.Parameters.AddWithValue("p_fileName", fileName);
            command.Parameters.AddWithValue("p_tableName", tableName);
            
            try
            {
                // SPを実行
                var result = await command.ExecuteScalarAsync(); 
                Console.WriteLine($"\tSP Execution Result/Status: {result ?? "No scalar result."}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\t[ERROR] Failed to execute SP '{spName}': {ex.Message}");
                throw; // 呼び出し元にエラーを再スロー
            }
        }
    }
}
```

### 4\. `Program.cs` (メインロジック)

構成を読み込み、順序付けられたロジックを実行します。

```csharp
using Microsoft.Extensions.Configuration;
using CSharpDataImporter.Logic;

namespace CSharpDataImporter
{
    internal class Program
    {
        private const string SpName = "sp_import_data"; // データベース側のSP名

        static async Task Main(string[] args)
        {
            // 1. 設定の読み込み
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("FATAL: Connection string 'DefaultConnection' not found.");
                return;
            }

            var dataLoader = new MySqlDataLoader(connectionString);
            Console.WriteLine("--- Start Sequential Data Import Process ---");

            // 2. 順序付けられたEnumのリストを取得し、ループ処理
            var orderedImports = ImportConfigMap.GetOrderedImports();

            foreach (var order in orderedImports)
            {
                var config = ImportConfigMap.Map[order];
                Console.WriteLine($"\n--- [Step {(int)order}] Processing Import ---");
                Console.WriteLine($"\tFile: {config.FileName}, Table: {config.TableName}");

                try
                {
                    // 3. ストアドプロシージャを実行
                    // SP側で実際の LOAD DATA INFILE 処理を行うことを想定
                    await dataLoader.ExecuteStoredProcedureAsync(SpName, config.FileName, config.TableName);
                    
                    Console.WriteLine($"\t✅ Step {(int)order} Completed.");
                }
                catch (Exception)
                {
                    // 順序が重要なので、エラー発生時は処理を中断
                    Console.WriteLine("\n!!! Process Aborted due to fatal error in current step. !!!");
                    return; 
                }
            }

            Console.WriteLine("\n=============================================");
            Console.WriteLine("🎉 All files imported successfully in defined order.");
            Console.WriteLine("=============================================");
        }
    }
}
```