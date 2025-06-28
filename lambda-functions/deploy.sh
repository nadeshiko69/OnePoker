#!/bin/bash

# Lambda関数のデプロイスクリプト
# 使用方法: ./deploy.sh [function-name]

FUNCTION_NAME=$1

if [ -z "$FUNCTION_NAME" ]; then
    echo "使用方法: ./deploy.sh [function-name]"
    echo "利用可能な関数:"
    echo "  - start-game"
    echo "  - get-game-state"
    echo "  - update-game-state"
    exit 1
fi

# 関数ディレクトリの存在確認
if [ ! -d "$FUNCTION_NAME" ]; then
    echo "エラー: 関数ディレクトリ '$FUNCTION_NAME' が見つかりません"
    exit 1
fi

echo "デプロイ中: $FUNCTION_NAME"

# 作業ディレクトリを関数ディレクトリに変更
cd "$FUNCTION_NAME"

# 依存関係をインストール
if [ -f "../requirements.txt" ]; then
    echo "依存関係をインストール中..."
    pip install -r ../requirements.txt -t .
fi

# ZIPファイルを作成
echo "ZIPファイルを作成中..."
zip -r "../${FUNCTION_NAME}.zip" .

# AWS CLIでデプロイ
echo "AWS Lambdaにデプロイ中..."
aws lambda update-function-code \
    --function-name "$FUNCTION_NAME" \
    --zip-file "fileb://../${FUNCTION_NAME}.zip"

if [ $? -eq 0 ]; then
    echo "デプロイ成功: $FUNCTION_NAME"
    # 一時ファイルを削除
    rm -f "../${FUNCTION_NAME}.zip"
else
    echo "デプロイ失敗: $FUNCTION_NAME"
    exit 1
fi

cd .. 