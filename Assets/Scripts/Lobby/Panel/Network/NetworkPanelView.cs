

using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using YuJanggi.BootStrap;

namespace YuJanggi.Lobby.Panel
{
    public class NetworkPanelView
    {
        private readonly TMP_Text       _statusText;
        private readonly TMP_Text       _statusDetailText;

        private CancellationTokenSource _timerCts;
        public NetworkPanelView(
            TMP_Text status,
            TMP_Text detail)
        {
            _statusText         = status;
            _statusDetailText   = detail;
        }

        public void HandleText(
            NetworkState state)
        {
            switch (state) {
                case NetworkState.Connecting:
                    _statusText.SetText("Offline");
                    _statusDetailText.SetText("Connection In Progress");
                    break;

                case NetworkState.Handshaking:
                    _statusText.SetText("Offline");
                    _statusDetailText.SetText("Handshake In Progress");
                    break;

                case NetworkState.Online:
                    _statusText.SetText("Online");
                    _statusDetailText.SetText("Connection Online");
                    break;

                case NetworkState.Offline:
                    _statusText.SetText("Offline");
                    _statusDetailText.SetText("Connection Failed");
                    break;

                case NetworkState.Matching:
                    _statusText.SetText("Online");
                    _statusDetailText.SetText("Match Searching");
                    break;

                case NetworkState.Matched:
                    _statusText.SetText("Online");
                    _statusDetailText.SetText("Match Found");
                    break;

                default:
                    _statusText.SetText("Unknown");
                    _statusDetailText.SetText("Unknown");
                    break;
            }
        }



        public void StartMatchingTimer(
            CancellationToken token)
        {
            StopMatchingTimer();

            _timerCts =
                CancellationTokenSource.CreateLinkedTokenSource(token);

            RunMatchingTimerAsync(
                _timerCts.Token).Forget();
        }

        public void StopMatchingTimer()
        {
            _timerCts?.Cancel();
            _timerCts?.Dispose();
            _timerCts = null;
        }

        private async UniTask RunMatchingTimerAsync(
            CancellationToken token)
        {
            int elapsedSeconds = 0;

            while (true)
            {
                int minutes = elapsedSeconds / 60;
                int seconds = elapsedSeconds % 60;

                _statusDetailText.SetText(
                    "Match Searching {0:00}:{1:00}",
                    minutes,
                    seconds);

                await UniTask.Delay(
                    TimeSpan.FromSeconds(1),
                    cancellationToken: token);

                elapsedSeconds++;
            }
        }
    }
}

