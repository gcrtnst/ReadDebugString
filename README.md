# ReadDebugString
`OutputDebugString` 関数よって出力されるデバッグ文字列を、指定されたプロセスから取得して、標準出力に流すコンソールアプリケーションです。
C# や Win32API などの学習目的で作成しており、実用性はありません。

## ビルド
1. .NET 5 またはそれ以降の環境を用意します。
2. プロジェクトフォルダにて、下記コマンドを実行します。

```
dotnet publish -c Release
```

3. 生成された `ReadDebugString.exe` を `rdbg.exe` にリネームします。

## 使い方
- `rdbg attach <pid>` を実行すると、既存のプロセスにアタッチして、デバッグ文字列を取得します。
- `rdbg start <cmdline>...` を実行すると、新しくプロセスを作成して、デバッグ文字列を取得します。

## ライセンス
[LICENSE](LICENSE) を参照。
