# Error Handling Contract

Canonical error model for the CryptoTrading monolith (backend + frontend consumption).

## Response Shape
```json
{
  "code": "string",        // machine-readable error code
  "message": "string",     // human-friendly message
  "details": "string|obj", // optional extra info
  "traceId": "string"      // correlation/diagnostic id if available
}
```
- Content-Type: `application/json`.
- `traceId` should align with logging/tracing context (Activity/OTel when enabled).

## Examples by Domain
- Auth
  - 401 Unauthorized: `{"code":"AUTH_UNAUTHORIZED","message":"Invalid or missing credentials","traceId":"..."}`
  - 400 Bad Request (2FA): `{"code":"AUTH_2FA_INVALID","message":"Invalid 2FA code","traceId":"..."}`
- Trading
  - 400 Validation: `{"code":"TRADING_INVALID_ORDER","message":"Price out of allowed range","details":{"minPrice":100,"maxPrice":150},"traceId":"..."}`
  - 409 Conflict: `{"code":"TRADING_CONFLICT","message":"Order was modified by another process","traceId":"..."}`
- Bots
  - 400 Params: `{"code":"BOT_INVALID_PARAMS","message":"Missing grid bounds","traceId":"..."}`
  - 503 Dependency: `{"code":"BOT_MARKETDATA_UNAVAILABLE","message":"Market data unavailable","traceId":"..."}`
- Payments
  - 400/422: `{"code":"PAYMENT_INVALID_REQUEST","message":"Amount must be at least 10,000 VND","traceId":"..."}`
  - 502 Upstream: `{"code":"PAYMENT_GATEWAY_ERROR","message":"Stripe error while creating deposit","traceId":"..."}`
  - 400 Callback: `{"code":"PAYMENT_SIGNATURE_INVALID","message":"Invalid VNPay signature","traceId":"..."}`

## Mapping Exceptions → Errors
- ValidationException / Bad input → 400 with domain-specific `code`.
- Unauthorized/Forbidden → 401/403 with `AUTH_*` codes.
- NotFound → 404 with `RESOURCE_NOT_FOUND` (include resource type/id in details if safe).
- Concurrency/DbUpdateConcurrencyException → 409 `*_CONFLICT`.
- External dependency failure (Stripe/VNPay/CoinGecko) → 502 `*_GATEWAY_ERROR`.
- Unhandled → 500 `INTERNAL_ERROR` (log full exception; return generic message).

## Frontend Consumption
- Always read `code` to branch UX; `message` is user-facing where appropriate.
- Use `traceId` when reporting issues/support tickets.
- Retry policy: only for transient `*_GATEWAY_ERROR` or 5xx; do not retry validation/auth failures.

## Future Improvements
- Correlation IDs: propagate `traceId` from middleware (Activity.Current.TraceId).
- Error enumeration: maintain a centralized list of codes per domain (Auth, Trading, Bots, Payments).
- Logging integration: structured logs include `code`, `userId`, `orderId`/`botId`.
- OpenTelemetry hooks: span events for handled errors; record exception attributes.
- Idempotency: align error responses when idempotency keys are rejected (e.g., 409 with `IDEMPOTENT_REPLAY`).
