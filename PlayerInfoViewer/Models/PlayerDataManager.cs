﻿using PlayerInfoViewer.Configuration;
using System;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using Zenject;

namespace PlayerInfoViewer.Models
{
    public class PlayerDataManager : IInitializable
    {
        private readonly IPlatformUserModel _userModel;
        public bool _playerInfoGetActive = false;
        public string _userID;
        public PlayerFullInfoJson _playerFullInfo;
        public event Action OnPlayCountChange;

        public PlayerDataManager(IPlatformUserModel userModel)
        {
            _userModel = userModel;
        }

        public async void Initialize()
        {
            var userInfo = await _userModel.GetUserInfo();
            _userID = userInfo.platformUserId;
            await GetPlayerFullInfo();
            LastPlayerInfoUpdate();
            OnPlayCountChange?.Invoke();
        }
        public void PlayerInfoCheck()
        {
            if (_playerFullInfo == null || _playerInfoGetActive)
                return;
            OnPlayCountChange?.Invoke();
        }
        public async Task GetPlayerFullInfo()
        {
            if (_userID == null || _playerInfoGetActive)
                return;
            _playerInfoGetActive = true;
            var playerFullInfoURL = $"https://scoresaber.com/api/player/{_userID}/full";
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(playerFullInfoURL);
            var resJsonString = await response.Content.ReadAsStringAsync();
            _playerFullInfo = JsonConvert.DeserializeObject<PlayerFullInfoJson>(resJsonString);
            _playerInfoGetActive = false;
        }
        public void LastPlayerInfoUpdate()
        {
            if (_playerFullInfo == null)
                return;
            DateTime lastPlayTime;
            DateTime lastGetTime;
            if (!DateTime.TryParse(PluginConfig.Instance.LastPlayTime, out lastPlayTime))
                lastPlayTime = DateTime.Now.AddYears(-1);
            if (!DateTime.TryParse(PluginConfig.Instance.LastGetTime, out lastGetTime))
                lastGetTime = DateTime.Now.AddYears(-1);
            if (DateTime.Now - lastPlayTime >= new TimeSpan(PluginConfig.Instance.IntervalTime, 0, 0) &&
                lastGetTime < DateTime.Today.AddHours(PluginConfig.Instance.DateChangeTime))
            {
                PluginConfig.Instance.LastGetTime = DateTime.Now.ToString();
                PluginConfig.Instance.LastPP = _playerFullInfo.pp;
                PluginConfig.Instance.LastRank = _playerFullInfo.rank;
                PluginConfig.Instance.LastCountryRank = _playerFullInfo.countryRank;
                PluginConfig.Instance.LastTotalScore = _playerFullInfo.scoreStats.totalScore;
                PluginConfig.Instance.LastTotalRankedScore = _playerFullInfo.scoreStats.totalRankedScore;
                PluginConfig.Instance.LastAverageRankedAccuracy = _playerFullInfo.scoreStats.averageRankedAccuracy;
                PluginConfig.Instance.LastTotalPlayCount = _playerFullInfo.scoreStats.totalPlayCount;
                PluginConfig.Instance.LastRankedPlayCount = _playerFullInfo.scoreStats.rankedPlayCount;
                PluginConfig.Instance.LastReplaysWatched = _playerFullInfo.scoreStats.replaysWatched;
            }
            PluginConfig.Instance.LastPlayTime = DateTime.Now.ToString();
        }
    }
}
