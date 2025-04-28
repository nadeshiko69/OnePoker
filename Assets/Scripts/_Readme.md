## ゲーム開始から1ラウンドの流れ
1. ゲーム開始（PlacementManager.Start()）
    - UIや各種パネル（確認パネル、ベットパネル、オープンパネル）を非表示にする。
    - RandomChoiceCardとResultViewManagerのインスタンスを取得。
    - ボタンのリスナーをセット。
2. プレイヤーがカードをセット
    - プレイヤーがカードをドロップゾーンにドラッグ＆ドロップすると、ShowConfirmation(card, zone)が呼ばれる。
        - currentCardとcurrentZoneにセットしたカードとゾーンを記録。
        - 確認パネルを表示し、「はい」「いいえ」ボタンのリスナーをセット。
3. カード配置の確認
    - 「はい」ボタンでConfirmPlacement()が呼ばれる。
    - カードをゾーンの位置に移動。
    - playerCardsリストにcurrentCardを追加。
    - 状態をリセットし、1秒後に相手のカードセット処理（SetOpponentCardFlag()）を開始。
4. 相手（CPU）がカードをセット
    - PlaceOpponentCard(opponentCard, opponentZone)が呼ばれる。
    - 相手カードをゾーンに移動し、裏面表示。
    - RandomChoiceCardからカードの値を取得し、SetCardInfoでCardDisplayにセット。
5. 両者カードが揃ったらベットフェーズ
    - bothCardsPlacedがtrueになり、ShowBettingUI()が呼ばれる。
    - ベットパネルを表示し、ベットボタンのリスナーをセット。
    - プレイヤーはライフを賭けてベットできる。
6. コール（勝負開始）
    - プレイヤーが「Call」ボタンを押すとHandleCall()が呼ばれる。
    - ベットパネルを非表示。
    - 相手のライフをベット分減らす。
    - オープンパネルを表示し、1秒後に非表示。
    - RevealCards()でカードを表向きにする。
7. 勝敗判定
    - ShowResultWithDelay()が呼ばれる。
    - currentCardとopponentCardのCardDisplayからカードの値を取得。
    - ResultViewManager.ShowResult()で勝敗判定＆結果表に記載。
    - 3秒後に結果表示を非表示。
8. 次ラウンド or ゲーム終了
    - 必要に応じてカードを補充し、次のラウンドへ。
    - どちらかのライフが0になったらゲーム終了。

## 各スクリプトの役割
- PlacementManager.cs
    - ゲーム全体の進行管理（カード配置、ベット、勝敗判定、ラウンド管理）
- CardDisplay.cs
    - カード1枚の見た目・値の管理（表裏切り替え、数字・マーク表示、値の保持）
- RandomChoiceCard.cs
    - 山札の管理、カードのシャッフル・配布、カードの値の決定
- ResultViewManager.cs
    - 勝敗結果の表示、結果表の更新