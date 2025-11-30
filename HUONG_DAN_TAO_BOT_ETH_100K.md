# Hướng dẫn tạo Bot mua ETH/USD với 100k USD

## Bước 1: Lấy Strategy ID

Gọi API để lấy danh sách strategies:

```http
GET http://localhost:5299/api/bot-strategies
Authorization: Bearer YOUR_TOKEN
```

Tìm strategy có `strategyKey` là `"aggressive-forex"` và copy `id` của nó.

Ví dụ response:
```json
[
  {
    "id": "a1a11bc3-415d-4a5b-9da4-2e3afac22be5",
    "strategyKey": "aggressive-forex",
    "displayName": "Aggressive Forex Style",
    ...
  }
]
```

## Bước 2: Tạo Bot

Sử dụng file `create_bot_eth_100k.json` và thay thế `YOUR_STRATEGY_ID_HERE` bằng Strategy ID từ bước 1.

```http
POST http://localhost:5299/api/bots
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json
```

Body:
```json
{
  "name": "ETH Buy Bot - 100k USD",
  "strategyDefinitionId": "a1a11bc3-415d-4a5b-9da4-2e3afac22be5",
  "baseAsset": "ETH",
  "quoteAsset": "USD",
  "riskProfile": "Moderate",
  "parameters": {
    "emaPeriod": 200,
    "emaShift": 5,
    "initialLot": 0.1,
    "capitalAllocation": 100000,
    "pyramidEnabled": true,
    "pyramidTriggerPercent": 3.0,
    "pyramidLotMultiplier": 1.0,
    "maxPyramidOrders": 10,
    "martingaleEnabled": true,
    "martingaleDistancePercent": 4.0,
    "martingaleMultiplier": 1.1,
    "maxMartingaleOrders": 10,
    "rsiPeriod": 5,
    "rsiOverbought": 80,
    "rsiOversold": 20,
    "cutLossUSD": 10000,
    "trailingEnabled": true,
    "trailingTriggerPercent": 10.0,
    "trailingDistancePercent": 10.0,
    "executionIntervalSeconds": 60
  },
  "executionIntervalSeconds": 60
}
```

## Bước 3: Start Bot

Sau khi bot được tạo, lấy `id` từ response và start bot:

```http
POST http://localhost:5299/api/bots/{botId}/start
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json
```

Body:
```json
{
  "mode": "live"
}
```

## Giải thích Parameters

- **capitalAllocation**: 100000 (100k USD) - Tổng vốn bot được phép sử dụng
- **initialLot**: 0.1 - Số lượng ETH ban đầu khi mua (có thể điều chỉnh)
- **cutLossUSD**: 10000 - Cắt lỗ khi thua lỗ 10k USD
- **baseAsset**: "ETH" - Coin muốn mua
- **quoteAsset**: "USD" - Đồng tiền thanh toán (đã được normalize từ USDT sang USD)

## Lưu ý

1. Bot sẽ tự động chuyển đổi USDT → USD nếu bạn nhập USDT
2. Bot sẽ tạo orders với symbol `ETH/USD` để match với user orders
3. Bot sẽ chạy mỗi 60 giây (executionIntervalSeconds)
4. Strategy "aggressive-forex" sẽ tự động mua ETH khi có tín hiệu EMA + RSI phù hợp

