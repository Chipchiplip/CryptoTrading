import React, { useState } from 'react';
import Auth from './Auth';

function LoginModal({ open, onClose, onSuccess }) {
  const [showEmail, setShowEmail] = useState(false);
  const [qrFallback, setQrFallback] = useState(false);
  if (!open) return null;

  return (
    <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }} onClick={onClose}>
      <div style={{ width: 520, maxWidth: '95%', background: '#fff', borderRadius: 12, boxShadow: '0 10px 30px rgba(0,0,0,0.25)' }} onClick={(e) => e.stopPropagation()}>
        <div style={{ padding: '16px 20px', borderBottom: '1px solid #e5e7eb', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <div style={{ fontSize: 20, fontWeight: 800 }}>Welcome to CoinGecko</div>
          <button onClick={onClose} style={{ background: 'transparent', border: 'none', fontSize: 22, cursor: 'pointer' }}>×</button>
        </div>
        <div style={{ padding: 20 }}>
          {/* Social buttons */}
          <div style={{ display: 'grid', gap: 12 }}>
            <button style={{ padding: 12, borderRadius: 10, border: '1px solid #e5e7eb', background: '#fff', fontWeight: 700, cursor: 'pointer', display:'flex', alignItems:'center', justifyContent:'center', gap:10 }}>
              <svg width="18" height="18" viewBox="0 0 48 48"><path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.9-6.9C36.89 2.6 30.89 0 24 0 14.62 0 6.61 5.37 2.69 13.13l8.18 6.35C12.61 13.9 17.83 9.5 24 9.5z"/><path fill="#4285F4" d="M46.5 24c0-1.64-.15-3.22-.44-4.74H24v9h12.7c-.55 2.96-2.22 5.47-4.72 7.16l7.21 5.6C43.89 37.39 46.5 31.2 46.5 24z"/><path fill="#FBBC05" d="M10.87 28.48A14.49 14.49 0 0 1 9.5 24c0-1.56.27-3.06.75-4.48l-8.18-6.35A23.94 23.94 0 0 0 0 24c0 3.86.93 7.51 2.57 10.72l8.3-6.24z"/><path fill="#34A853" d="M24 48c6.48 0 11.92-2.13 15.89-5.82l-7.21-5.6c-2.02 1.36-4.61 2.17-8.68 2.17-6.17 0-11.39-4.4-13.13-10.24l-8.3 6.24C6.61 42.63 14.62 48 24 48z"/></svg>
              Continue with Google
            </button>
            <button style={{ padding: 12, borderRadius: 10, border: '1px solid #e5e7eb', background: '#fff', fontWeight: 700, cursor: 'pointer', display:'flex', alignItems:'center', justifyContent:'center', gap:10 }}>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="#000"><path d="M16.365 1.43c0 1.14-.533 2.285-1.28 3.164-.73.879-1.9 1.544-3.074 1.456-.13-1.11.42-2.31 1.16-3.2.76-.905 2.07-1.55 3.194-1.62zm4.546 15.06c-.058.117-1.047 3.58-3.43 3.58-1.034 0-1.84-.69-2.943-.69-1.118 0-1.97.705-2.997.705-2.42 0-3.854-3.318-3.91-3.434-.58-1.442-1.17-3.66-.48-5.66.8-2.28 2.74-3.74 4.93-3.77 1.04-.02 2.03.72 2.945.72.9 0 1.97-.74 3.32-.64.566.02 2.16.23 3.18 1.75-.083.052-1.82 1.08-1.8 3.21.022 2.56 2.22 3.41 2.26 3.42z"/></svg>
              Continue with Apple
            </button>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12, margin: '16px 0' }}>
            <div style={{ height: 1, flex: 1, background: '#e5e7eb' }} />
            <div style={{ color: '#94a3b8', fontWeight: 700 }}>or</div>
            <div style={{ height: 1, flex: 1, background: '#e5e7eb' }} />
          </div>

          {!showEmail ? (
            <button onClick={() => setShowEmail(true)} style={{ width:'100%', padding: 12, borderRadius: 10, border:'1px solid #e5e7eb', background:'#fff', fontWeight:700, cursor:'pointer' }}>
              Continue with email
            </button>
          ) : (
            <Auth onSuccess={onSuccess} />
          )}

          {/* QR Banner */}
          {(() => {
            const appStoreUrl = 'https://apps.apple.com/us/app/coingecko-crypto-tracker/id1390323960?_branch_match_id=1508792687499158738&utm_source=CoinGecko&utm_campaign=cg_footer&utm_medium=cg_footer&_branch_referrer=H4sIAAAAAAAAA8soKSkottLXT87PzEtPTc7O10ssKNDLyczL1k%2FLzy9JLdLNzC%2B2rytKTUstKgIqiU8qyi8vTi2ydU1JTwUAYJMFDj0AAAA%3D';
            const playStoreUrl = 'https://play.google.com/store/apps/details?id=com.coingecko.coingeckoapp&_branch_match_id=1508792687499158738&utm_source=CoinGecko&utm_campaign=cg_footer&utm_medium=cg_footer&_branch_referrer=H4sIAAAAAAAAA8soKSkottLXT87PzEtPTc7O10ssKNDLyczL1k%2FLzy9JLdJNzEspys9Msa8rSk1LLSoCKotPKsovL04tsnVNSU8FAEm1U4JBAAAA';
            // Preferred: exact QR image you provide at /coingecko-qr.png (put under public/)
            // Fallback: generated QR for Play Store deep link
            const generatedQr = `https://chart.googleapis.com/chart?chs=180x180&cht=qr&chld=L|0&chl=${encodeURIComponent(playStoreUrl)}`;
            const imgSrc = qrFallback ? generatedQr : '/coingecko-qr.png';
            return (
              <div style={{ marginTop: 16, border: '1px solid #e5e7eb', borderRadius: 12, padding: 16, display:'flex', alignItems:'center', justifyContent:'space-between', gap: 12 }}>
                <div>
                  <div style={{ fontWeight: 800, fontSize: 16 }}>Get Price Alerts with CoinGecko App</div>
                  <div style={{ display:'flex', gap: 8, marginTop: 8 }}>
                    <a href={appStoreUrl} target="_blank" rel="noreferrer" style={{ width: 140, height: 40, background: '#111', borderRadius: 6, display:'flex', alignItems:'center', justifyContent:'center', color:'#fff', fontSize:12, textDecoration:'none', fontWeight:700 }}>App Store</a>
                    <a href={playStoreUrl} target="_blank" rel="noreferrer" style={{ width: 140, height: 40, background: '#2dd4bf', borderRadius: 6, display:'flex', alignItems:'center', justifyContent:'center', color:'#0f172a', fontSize:12, textDecoration:'none', fontWeight:700 }}>Google Play</a>
                  </div>
                </div>
                <a href={playStoreUrl} target="_blank" rel="noreferrer" title="Open CoinGecko App">
                  <img src={imgSrc} onError={() => setQrFallback(true)} alt="CoinGecko App QR" style={{ width: 96, height: 96, borderRadius: 8 }} />
                </a>
              </div>
            );
          })()}

          <div style={{ color:'#94a3b8', fontSize:12, marginTop: 12 }}>
            By continuing, you acknowledge that you've read and agree fully to our <a href="https://www.coingecko.com/en/terms" target="_blank" rel="noreferrer">Terms of Service</a> and <a href="https://www.coingecko.com/en/privacy" target="_blank" rel="noreferrer">Privacy Policy</a>.
          </div>
        </div>
      </div>
    </div>
  );
}

export default LoginModal;


