# Cloudflare avatar upload configuration

The backend requests **Cloudflare Images** direct-upload URLs and expects your
avatar assets to be delivered from the same host(s) that the client will later
load. Configure `appsettings.json` / `appsettings.Development.json` with real
values before running the API.

## Required keys
```jsonc
"Cloudflare": {
  "AccountId": "<your-account-id>",
  "ApiToken": "<token with Account.Cloudflare Images:Edit>",
  "DeliveryUrl": "https://imagedelivery.net/<delivery-hash>",
  "AllowedAvatarDomains": [
    "https://imagedelivery.net/<delivery-hash>",
    "https://cdn.example.com"
  ]
}
```

- `DeliveryUrl` should be the exact base URL Cloudflare gives you for Images.
  If you mapped the asset to **Cloudflare R2** or any custom hostname (e.g.
  `https://cdn.example.com` or `https://pub-1234abcd.r2.dev`), add those URLs to
  `AllowedAvatarDomains` so the backend allows users to persist them.
- The backend validates avatar URLs in `AuthService.ValidateAvatarUrl` and will
  reject anything whose host does not match the allowed list. Leaving
  `AllowedAvatarDomains` empty makes it fall back to `DeliveryUrl`.

## Where to find values
1. **Account ID** – shown on the right-hand panel of the Cloudflare dashboard
   (`Images → Overview`).
2. **API Token** – create from `My Profile → API Tokens → Create Token` and pick
   the *Cloudflare Images* template, which already grants
   `Account.Cloudflare Images:Edit`.
3. **Delivery URL / R2 domain** – under `Images → Custom Domains`. If you are
   serving images straight from **Cloudflare R2 (not Backblaze B2)** via
   Cloudflare’s delivery layer, copy the URL that browsers will use
   (for example `https://pub-1234abcd.r2.dev` or a worker route) and include it
   in both `DeliveryUrl` and `AllowedAvatarDomains`.

After editing the configuration, restart the API so `CloudflareImagesOptions`
gets the new values through `builder.Services.Configure<CloudflareImagesOptions>`
in `Program.cs`.
