# AkyuiUnity / AkyuiUnity.Xd マニュアル

***Read this document in other languages: [English](https://github.com/kyubuns/AkyuiUnity/blob/main/Manual/Manual_en.md)***

ToDo

## XD変換規則

### ネーミング

オブジェクトの名前の最後を以下のようにすると、Unity上でもコンポーネントが貼られます。

#### `*Button`

- Button

#### `*Scrollbar`

- Scrollbar

#### `*Spacer`

- Scroll以下にSpacerという名前のオブジェクトを入れるとPaddingの指定が出来る。

#### `*InputField`

- InputField

### パラメーター

オブジェクトの名前の最後に@〜と書くと、以下の効果が得られます。

#### `@Placeholder`

- 画像はエクスポートせず、位置だけを保持する。

#### `@MultiItems`

- ScrollのついたGroupにのみ有効。
- 展開される要素が子供ではなく孫になる。

#### `@Vector`

- 子供が全てベクターデータのとき、そのグループを1画像としてUnityにインポートする。

