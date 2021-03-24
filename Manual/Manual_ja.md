# AkyuiUnity / AkyuiUnity.Xd マニュアル

***Read this document in other languages: [English](https://github.com/kyubuns/AkyuiUnity/blob/main/Manual/Manual_en.md)***

---

## 動画で説明を見る

[![](https://img.youtube.com/vi/bJC9ueWZp28/0.jpg)](https://www.youtube.com/watch?v=bJC9ueWZp28)8

https://www.youtube.com/watch?v=bJC9ueWZp28

---

## 使い方

### 初回セットアップ

- PackageManagerで、以下の2つをインポートします。
  - AkyuiUnity `https://github.com/kyubuns/AkyuiUnity.git?path=Assets/AkyuiUnity`
  - AkyuiUnity.Xd `https://github.com/kyubuns/AkyuiUnity.git?path=Assets/AkyuiUnity.Xd`
- Assets > Create > AkyuiXd > XdImportSettingsを選び設定ファイルを作ります。
  - この設定をいじることで、プロジェクト毎にどういう風にXDをインポートするかが決めれます。
  - トリガーという強力なカスタマイズ機能がついています。後ほど紹介します。


### XDファイルの作り方

- どのArtboardを出力するかは、Artboard自身のMark for Exportフラグで決定します。
- 特別なルールは[こちら](https://github.com/kyubuns/AkyuiUnity/blob/main/Manual/Manual_ja.md#xd%E5%A4%89%E6%8F%9B%E8%A6%8F%E5%89%87)にまとめています。
- 使用できない機能は[こちら](https://github.com/kyubuns/AkyuiUnity/blob/main/Manual/Manual_ja.md#%E6%9C%AA%E5%AF%BE%E5%BF%9C%E9%A0%85%E7%9B%AE)を参照してください。


### インポート方法

- 「初回セットアップ」で作ったXdImportSettingsのInspectorにある「Drop xd」と書いてある箇所にXDファイルをドラッグアンドドロップします。
- 2回目以降は、履歴からボタン1つでインポート出来ます。


### トリガー

- XDファイルをどうPrefabに落とすか、そのカスタマイズが出来る機能です。
- 例えば「テキストにはuGUIのTextを使うのか、TextMeshProを使うのか」「SpriteAtlasを作るのか、作るならどこに作るのか」などを設定出来ます。
- トリガー一覧は[こちら](https://github.com/kyubuns/AkyuiUnity/blob/main/Manual/Manual_ja.md#%E3%83%88%E3%83%AA%E3%82%AC%E3%83%BC%E4%B8%80%E8%A6%A7)をご覧ください。


### アップデート方法

- Packages/packages-lock.jsonのhashを更新してください。
  - Unityさん、なんか良い感じに更新する方法準備してくださいお願いします。


---
## Xd Import Settings

### Prefab Output Path

- Prefabを出力するパスです。
- `{name}` がArtboardの名前に置換されます。

### Asset Output Directory Path

- Prefabに使用される画像を入れるディレクトリのパスです。
- `{name}` がArtboardの名前に置換されます。

### Meta Output Path

- PrefabのためのMetaファイルを出力するパスです。
  - Metaファイルはビルドに含める必要はありませんが、再インポート時に情報を使うためUnityEditorから見える場所に保存しておいてください。
- `{name}` がArtboardの名前に置換されます。

### Font Directory Path

- Prefabを生成する時に使用するFontのディレクトリパスです。

### Triggers

### Sprite Save Scale

- Prefabに使用する画像をXD上でのサイズの何倍で保存するかの指定です。
- 大きくするほどSpriteが大きく、綺麗に保存されるようになります。

### Reimport Layout

- XDに変更がなくても強制的にPrefabを生成しなおします。
  - XdImportSettingsの設定を変更して、反映させたい時などに使います。

### Reimport Asset

- XDに変更がなくても強制的にPrefabに使われる画像を生成しなおします。
  - XdImportSettingsの設定を変更して、反映させたい時などに使います。

### Object Parsers

### Group Parsers

### Xd Triggers

### Akyui Output Path

- Akyui(UI構造定義)ファイルの出力パスです。
- 空白にすると無効になります。


---
## 推奨する使い方

### Prefabの扱い

- 生成されたPrefabを手動で変更を加えることは推奨しません。
  - XDを更新し、再度インポートしたときに変更が失われてしまいます。
  - 特定の操作をしたいときはTriggerの使用を検討してください。


### [AnKuchen](https://github.com/kyubuns/AnKuchen)とのつなぎ込み

- [AnKuchen](https://github.com/kyubuns/AnKuchen)を使用することで生成されたUIを簡単にスクリプトから操作出来ます。
- AnKuchenをインポートしてからPrefabを生成すると、自動的にUICacheComponentを付与するTriggerが使えるようになります。
- (ToDo)
  - 具体的な使い方


---
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

#### `@Pivot`

- そのオブジェクトの中心がそのグループの原点となる。


---
## 未対応項目

### やりたい

#### Horizontal & Vertical Scroll

### 保留

#### State

- どこまで何を再現するか。

#### different radius for each corner

- 要調査

#### Polygon


### やらない

#### Shadow

- やらない。

#### Blur

- やらない。

#### 3D Transforms

- Unityなら再現出来てしまうが、Akyuiに入れたくないのでやらない。

#### Blend Mode

- Unity上で汎用的に再現する方法が思いつかないのでやらない。


---
## トリガー一覧

ToDo


---
## フィードバック

ぜひフィードバックをお寄せください！

- [githubのissue](https://github.com/kyubuns/AkyuiUnity/issues) （バグ報告のみ）
- [githubのdiscussion](https://github.com/kyubuns/AkyuiUnity/discussions)
- twitterで[ハッシュタグ #akyui](https://twitter.com/search?q=%23akyui)をつけるか[@kyubuns](https://twitter.com/kyubuns)にリプライ！
- [メッセージフォーム](https://kyubuns.dev/message.html)


---
## Buy me a coffee

もしこのプロジェクトが気に入ったなら、ぜひコーヒーを奢ってください！  
https://www.buymeacoffee.com/kyubuns


---
## 「ゲームに使ったよ！」

「このゲームにこのライブラリ使ったよ！」という報告を貰えるとめっちゃ喜びます！  
メールやtwitterでお気軽にご連絡ください。  
(MITライセンスのため、報告は義務ではありません。)  
[メッセージフォーム](https://kyubuns.dev/message.html)

https://kyubuns.dev/
