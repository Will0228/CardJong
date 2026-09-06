using Photon.Realtime;
using UnityEngine;

namespace CardJong.Network.Matching
{
    /// <summary>
    /// Photon への接続設定。App ID とリージョンをここに持つ。
    /// </summary>
    /// <remarks>
    /// PUN の PhotonServerSettings とは別物。PUN の層（PhotonNetwork）を使わないため、
    /// Realtime だけで完結する設定を自前で持っている。
    /// <see cref="Photon.Realtime.AppSettings"/> は Unity でシリアライズできるので、
    /// Inspector には PUN の設定画面と同じ項目が並ぶ。
    /// </remarks>
    [CreateAssetMenu(fileName = "PhotonMatchSettings", menuName = "CardJong/Photon Match Settings")]
    public sealed class PhotonMatchSettings : ScriptableObject
    {
        [Tooltip("Photon のダッシュボードで作った Realtime アプリの設定。App ID Realtime にダッシュボードの値を貼る。")]
        [SerializeField] private AppSettings _appSettings = new();

        public AppSettings AppSettings => _appSettings;

        /// <summary>App ID が入っているか。接続前の確認に使う。</summary>
        public bool HasAppId => _appSettings != null && !string.IsNullOrWhiteSpace(_appSettings.AppIdRealtime);
    }
}
