# AkyuiUnity

AkyuiUnityは、[Akyui](https://github.com/kyubuns/Akyui)のUnity実装です。  
AkyuiUnity.Xdと合わせて使うことで、簡単に[Adobe XD](https://www.adobe.com/products/xd.html)ファイルからUnityのPrefabを生成することが出来ます。

***Read this document in other languages: [English](https://github.com/kyubuns/AkyuiUnity/blob/main/README.md)***

<a href="https://www.buymeacoffee.com/kyubuns" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174"></a>

<img width="800" src="https://user-images.githubusercontent.com/961165/107123379-93689600-68e0-11eb-9cd0-41759afeb01b.png">  
<img width="800" src="https://user-images.githubusercontent.com/961165/107123374-8e0b4b80-68e0-11eb-89b6-2549a58deaa2.png">

---

**AkyuiUnityはまだ開発中のため、今後の更新により挙動が大きく変更される可能性があります。**  
**詳しくはdiscussionsをご覧ください。(Japanese)**  
https://github.com/kyubuns/AkyuiUnity/discussions/8

---

## AkyuiUnity / AkyuiUnity.Xdとは？

そもそも、[Akyui](https://github.com/kyubuns/Akyui)とは[kyubuns](https://github.com/kyubuns)が作ったUI定義ファイルのフォーマットのことです。  
AkyuiUnityは、AkyuiからUnityPrefabを生成することが出来、  
AkyuiUnity.Xdは、XDファイルをAkyuiに変換することが出来ます。  
この2つを組み合わせることで、Akyuiを意識することなく、XDファイルからUnityPrefabを生成することが出来ます。

## 特徴

### インポートがUnityのみで完結する

- XDファイルをインポートするために、Adobe XDを開く必要はありません。
- 全てがUnity上で完結するため、CIなどに任せることも出来ます。

### XDファイルの更新に追従できる

- デザイナーはずっとAdobe XD上でUI制作を続けることが出来ます。
- 差分だけをインポートするので、2回目以降のインポート時間は短縮されます。

### ランタイムに関与しない

- AkyuiUnityが行うのは、あくまでPrefabを作るだけでランタイム時には一切コストはかかりません。

### カスタマイズ性が高い

- あなたのプロジェクトにあったPrefabを生成出来るように、簡単にトリガー(拡張スクリプト)を書くことが出来ます。
  - 例えば、以下のようなことはパッケージに含まれているトリガーで実現出来ます。
    - 素材を自動的に9SliceSpriteに変換し、テクスチャを節約する。
    - uGUIのTextではなく、TextMeshProを使う。
    - XDファイル上で特定の名前のオブジェクトはUnityに変換しない。

## ユーザーマニュアル

ToDo


## インストール方法

### UnityPackageManager

- AkyuiUnity `https://github.com/kyubuns/AkyuiUnity.git?path=Assets/AkyuiUnity`
- AkyuiUnity.Xd `https://github.com/kyubuns/AkyuiUnity.git?path=Assets/AkyuiUnity.Xd`


## ターゲット環境

- Unity2019.4 or later


## ライセンス

MIT License (see [LICENSE](LICENSE))

## スペシャルサンクス

- サンプルに使っているXDファイル (CC0)
  - https://github.com/beinteractive/Public-Game-UI-XD

## Buy me a coffee

もしこのプロジェクトが気に入ったなら、ぜひコーヒーを奢ってください！  
https://www.buymeacoffee.com/kyubuns

## 「ゲームに使ったよ！」

「このゲームにこのライブラリ使ったよ！」という報告を貰えるとめっちゃ喜びます！  
メールやtwitterでお気軽にご連絡ください。  
(MITライセンスのため、報告は義務ではありません。)  
https://kyubuns.dev/
