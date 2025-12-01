# Team Workflow (Current)

## Branching
- `main`: stable
- `feature/*`, `bugfix/*`: short-lived branches off main
- Rebase or merge main before PR

## PR Rules
- Small, focused PRs
- CI: build + tests (add when available)
- Must reference area (auth/trading/bot/payment/frontend/etc.)
- Require one review; two for P0 changes (trading/payments/auth)

## Commits
- Conventional style preferred (`feat:`, `fix:`, `chore:`)
- Include area tags where helpful (`trading:`, `bot:`, `payment:`)

## Code Review
- Check against: error contract, concurrency rules, logging/trace, validation, security (authz, input limits)
- Verify docs updated when APIs/flows change

## DB Migrations
- Create migrations locally; review SQL impact
- Do not auto-apply in prod; run manually
- Coordinate migration timing with releases and background services

## Background Services Testing
- Validate locally: matcher, bot exec, bot monitor, crypto sync, realtime broadcast, subscription expiration
- Watch logs for errors/backoff; verify side effects in DB

## New Bot Strategies
- Implement strategy class + metadata
- Validate parameters and defaults
- Register in StrategyRegistry; seed BotStrategyDefinitions if needed
- Add tests (signal generation) and doc update if parameters change

## Frontend → Backend Integration
- Align with `ERROR_HANDLING_CONTRACT.md`
- Verify endpoints against Swagger and domain docs
- Test SignalR flows using `/marketHub` and `/botHub`

## Release Cycle
- Manual deploy; ensure migrations applied first
- Smoke test: auth, order placement, bot run + SignalR, payment callback (test mode), portfolio

## Hotfix Protocol
- Branch `hotfix/*` from main
- Minimal change set; fast review; targeted deploy
- Postmortem and backport if needed
