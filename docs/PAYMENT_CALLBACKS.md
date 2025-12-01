# Payment Callback Flows (Stripe & VNPay)

How payment callbacks map into internal entities (`Subscription`, `PaymentHistory`, `DepositTransaction`) in the current ASP.NET Core monolith.

## Stripe

### Endpoints
- Create deposit checkout: `POST /api/payment/deposit/stripe`
- Confirm deposit (client-side check): `POST /api/payment/deposit/stripe/confirm`
- Get session info: `GET /api/payment/stripe/session/{sessionId}`
- Webhook: `POST /api/payment/webhook`

### Events handled
- `checkout.session.completed` → `HandleStripeCheckoutCompletedAsync` (credits `DepositTransaction` via `CreditDepositAsync`)
- `payment_intent.payment_failed` → `HandleStripePaymentFailedAsync` (marks deposit failed)

### Flow
1) User starts deposit → `DepositTransaction` created (Status=PENDING, provider=STRIPE, orderId, session metadata includes `depositId`, `userId`, `orderId`).
2) Stripe webhook calls `/api/payment/webhook`:
   - Signature validated if `Stripe:WebhookSecret` is set.
   - `checkout.session.completed` → lookup deposit by `depositId` (metadata); if still PENDING, mark SUCCESS via `CreditDepositAsync`, credit USD wallet, insert WalletMovement.
   - `payment_intent.payment_failed` → mark deposit FAILED with message.
3) Optional client confirm: `/api/payment/deposit/stripe/confirm` checks session status and (if paid) calls `CreditDepositAsync`.

### Idempotency notes
- Deposit is guarded by status check (`PENDING` only). No dedicated event-id store; duplicate events should be safe but consider adding `(gateway,eventId)` persistence.
- Signature verification via `Stripe-Signature` header and `Stripe:WebhookSecret` is mandatory.

## VNPay

### Endpoints
- Create deposit checkout: `POST /api/payment/deposit/vnpay`
- Deposit return (browser/IPN): `GET /api/payment/vnpay/callback`
- Subscription return: `GET /api/payment/subscription/vnpay/callback`

### Flow: Deposits
1) User starts deposit → `DepositTransaction` created (Status=PENDING, Provider=VNPAY, OrderId=ticks).
2) VNPay returns to `/api/payment/vnpay/callback` with signed query params.
3) Backend:
   - Verifies `vnp_SecureHash` and response code via `VnPayService.PaymentExecute`.
   - Finds `DepositTransaction` by `OrderId`.
   - If already processed (Status != PENDING) → redirect with existing status.
   - On success (`vnp_ResponseCode == "00"`): set VnPay transaction info, convert VND→USD (~24000), call `CreditDepositAsync` to credit wallet and mark SUCCESS.
   - On failure: mark FAILED and redirect.

### Flow: Subscriptions
1) VNPay return: `GET /api/payment/subscription/vnpay/callback`.
2) Backend verifies signature via `PaymentExecute`, loads `PaymentHistory` by `VnpayOrderId`.
3) If pending and `vnp_ResponseCode == "00"`:
   - Mark `PaymentHistory` success, set `VnpayTransactionId`.
   - Create/Update `Subscription` (default planType=2 if missing), attach `SubscriptionId` to `PaymentHistory`.
   - Commit transaction, redirect with success.
4) Else mark failed and redirect.

### Idempotency notes
- Deposit/Subscription callbacks gate on Status/PENDING to avoid double processing.
- No persistent idempotency table; consider adding unique `(Provider, OrderId)` and `(Provider, TransactionId)` constraints plus event log.

## Security
- Stripe: verify webhook signature (`Stripe:WebhookSecret`).
- VNPay: verify `vnp_SecureHash` and response code; never trust client-passed amounts.
- Do not credit without signature validation and matching `OrderId`/amount.

## TODO
- Persist webhook event IDs (Stripe) and enforce uniqueness.
- Add tests for Stripe/VNPay callbacks (happy path, duplicate, bad signature, mismatched amount).
- Add centralized callback logging with correlation IDs (orderId/depositId/subscriptionId).
