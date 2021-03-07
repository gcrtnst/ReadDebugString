# ReadDebugString
`OutputDebugString` 関数を使って送信されるデバッグ文字列を取得して標準出力に流すコンソールアプリケーションです。
C# や Win32API などの学習目的で作成しており、実用性はありません。

## ビルド
1. .NET 5 またはそれ以降の環境を用意する。
2. プロジェクトフォルダにて、下記コマンドを実行する。

```
dotnet publish -c Release
```

## 使い方
- `rdbg attach <pid>` を実行すると、既存のプロセスにアタッチして、デバッグ文字列を取得します。
- `rdbg start <cmdline>...` を実行すると、新しくプロセスを作成して、デバッグ文字列を取得します。

## ライセンス
[LICENSE](LICENSE) を参照。
