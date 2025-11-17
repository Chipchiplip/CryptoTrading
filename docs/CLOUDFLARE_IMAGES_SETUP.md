# Cloudflare R2 avatar configuration

The backend now generates **Cloudflare R2 S3** presigned URLs and expects avatar
assets to be delivered from the same R2 host the client will later load. Update
`appsettings.json` / `appsettings.Development.json` with the values Cloudflare
provides for your account, R2 bucket, and access key pair.

## Required keys
```jsonc
"Cloudflare": {
  "AccountId": "f57aad0e4caab95af1c52c46175ca7a6",
  "ApiToken": "<optional API token for other calls>",
  "DeliveryUrl": "https://f57aad0e4caab95af1c52c46175ca7a6.r2.cloudflarestorage.com",
  "AllowedAvatarDomains": [
    "https://f57aad0e4caab95af1c52c46175ca7a6.r2.cloudflarestorage.com"
  ],
  "BucketName": "cryptotrading",
  "UploadPrefix": "cryptotranding/upload",
  "AccessKeyId": "<your R2 access key ID>",
  "SecretAccessKey": "<your R2 secret access key>",
  "UploadUrlExpiryMinutes": 15
}
```

- `DeliveryUrl` must be the exact public host you will expose to browsers (for
  example the `*.r2.cloudflarestorage.com` endpoint or a custom domain proxied
  through Cloudflare).
- `AllowedAvatarDomains` should only include that host so the backend rejects any
  third-party avatar domains.
- `BucketName` is the R2 bucket that stores avatars, while `UploadPrefix` is an
  optional folder/prefix (`cryptotranding/upload` in this environment).
- `AccessKeyId` / `SecretAccessKey` are the S3 credentials Cloudflare gives you
  when creating an R2 API token. They are required to generate presigned `PUT`
  URLs.

After editing the configuration, restart the API so
`CloudflareImagesOptions` picks up the changes through
`builder.Services.Configure<CloudflareImagesOptions>` in `Program.cs`.

## Migration notes (Cloudflare Images → R2)

- **Old flow**: requested Cloudflare Images direct-upload URLs and validated
  avatars served from `imagedelivery.net` or custom Images domains.
- **New flow**: generates Cloudflare R2 S3 presigned `PUT` URLs, uploads files
  straight to the `cryptotrading/cryptotranding/upload/` prefix, and only allows
  avatars hosted on `https://f57aad0e4caab95af1c52c46175ca7a6.r2.cloudflarestorage.com`.
