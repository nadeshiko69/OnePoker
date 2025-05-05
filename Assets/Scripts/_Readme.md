## ゲーム開始から1ラウンドの流れ
1. ゲーム開始（GameManager.Start()）
    - UIや各種パネル（確認パネル、ベットパネル、オープンパネル）を非表示にする。
    - DeckManagerとResultViewManagerのインスタンスを取得。
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
    - DeckManagerからカードの値を取得し、SetCardInfoでCardDisplayにセット。
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

### 各スクリプトの役割
- GameManager.cs
    - ゲーム全体の進行管理（カード配置、ベット、勝敗判定、ラウンド管理）
- CardDisplay.cs
    - カード1枚の見た目・値の管理（表裏切り替え、数字・マーク表示、値の保持）
- DeckManager.cs
    - 山札の管理、カードのシャッフル・配布、カードの値の決定
- ResultViewManager.cs
    - 勝敗結果の表示、結果表の更新




## 採点（ライフ増減）の処理フロー
1. ベットフェーズ
    - GameManager.cs の PlaceBet メソッド
        - プレイヤーがベットボタンを押すと呼ばれる。
    - ベット額を増減するたびに、自分のライフを1ずつ増減する。
2. 勝負開始（コール）
    - GameManager.cs の HandleCall コルーチン
        - プレイヤーが「Call」ボタンを押すと呼ばれる。
    - ベットパネルを非表示にし、カードをオープンする演出を行う。
    - 最終的に RevealCards() を呼び出し、カードを表向きにする。
3. 勝敗判定と採点
    - GameManager.cs の RevealCards メソッド
        - プレイヤーと相手のカードを表向きにし、ShowResultWithDelay コルーチンを開始。
    - GameManager.cs の ShowResultWithDelay コルーチン
        - 1秒待機後、resultViewManager.ShowResult で勝敗を表示。
        - その直後に UpdateLife メソッドを呼び出し、採点（ライフ増減）を行う。
    - GameManager.cs の UpdateLife メソッド
        - 勝敗に応じてライフを増減するロジックの中心。
        - ベット時に両者のライフは減っているので、勝者にベット分を加算
            - 引き分け：両者にベット分を返す
            - プレイヤー勝利：プレイヤーに相手のベット分を加算
            - 相手勝利：相手にプレイヤーのベット分を加算
    - MatchManager.cs の UpdatePlayerLife / UpdateOpponentLife メソッド
        - 実際のライフ値を加算・減算し、UIを更新する。