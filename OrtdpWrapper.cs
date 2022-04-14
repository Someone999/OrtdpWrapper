using System;
using System.Collections.Generic;
using System.IO;
using OrtdpWrapper.Rtppi;
using OsuRTDataProvider;
using OsuRTDataProvider.Listen;
using osuToolsV2.Beatmaps;
using osuToolsV2.Beatmaps.BreakTimes;
using osuToolsV2.Beatmaps.HitObjects;
using osuToolsV2.Beatmaps.TimingPoints;
using osuToolsV2.Database;
using osuToolsV2.Database.Beatmap;
using osuToolsV2.Game.Legacy;
using osuToolsV2.Game.Mods;
using osuToolsV2.Rulesets;
using osuToolsV2.Rulesets.Legacy;
using osuToolsV2.ScoreInfo;
using osuToolsV2.Tools;
using RealTimePPDisplayer;

namespace OrtdpWrapper
{
    public class OrtdpWrapper
    {
        private OsuRTDataProviderPlugin _ortdpPlugin;
        private OsuListenerManager _listenerManager;
        private RtppInfo _rtppInfo;
        private Ruleset _globalRuleset = new EmptyRuleset();
        private Beatmap _globalBeatmap;
        private IScoreInfo _globalScoreInfo = new OsuScoreInfo();
        private RealTimePPDisplayerPlugin _rtppd;
        private OsuBeatmapDb _osuBeatmapDb;
        private TimeSpan _curTime, _duration;
        private List<IHitObject> _globalHitObejcts;
        private List<BreakTime> _globalBreakTimes;
        private List<TimingPoint> _globalTimingPoints;
        public string CurrentPlayer { get; private set; }

        public OrtdpWrapper(OsuRTDataProviderPlugin ortdpPlugin = null, RealTimePPDisplayerPlugin rtppd = null, RtppInfo rtppi = null)
        {
            _ortdpPlugin = ortdpPlugin ?? new OsuRTDataProviderPlugin();
            _listenerManager = _ortdpPlugin.ListenerManager;
            _rtppd = rtppd ?? new RealTimePPDisplayerPlugin();
            _rtppInfo = rtppi ?? new RtppInfo();
            _osuBeatmapDb = new OsuBeatmapDb();
            InitListener(_listenerManager);
        }

        private void InitListener(OsuListenerManager listenerManager)
        {
            listenerManager.OnBeatmapChanged += ListenerManagerOnOnBeatmapChanged;
            listenerManager.OnComboChanged += combo => _globalScoreInfo.Combo = combo;
            listenerManager.OnCountGekiChanged += hit => _globalScoreInfo.CountGeki = hit;
            listenerManager.OnCount300Changed += hit => _globalScoreInfo.Count300 = hit;
            listenerManager.OnCountKatuChanged += hit => _globalScoreInfo.CountKatu = hit;
            listenerManager.OnCount100Changed += hit => _globalScoreInfo.Count100 = hit;
            listenerManager.OnCount50Changed += hit => _globalScoreInfo.Count50 = hit;
            listenerManager.OnCountMissChanged += hit => _globalScoreInfo.CountMiss = hit;
            listenerManager.OnModsChanged += mods => _globalScoreInfo.Mods = 
                ModList.FromLegacyMods((LegacyGameMod)mods.Mod, _globalRuleset);
            listenerManager.OnPlayerChanged += player => CurrentPlayer = player;
            listenerManager.OnPlayModeChanged += (last, mode) => ChangePlayMode(((LegacyRuleset)mode).ToString());
            
            /*   Not implement
             *   OnPlayTimeChanged
             *   OnAccuracyChanged
             *   OnScoreChanged
             *   OnStatusChanged
             *   OnErrorStatisticsChanged
             *   OnHealthPoint
             *   OnHitEventsChanged
             */
            
            listenerManager.Start();

        }

        private void ChangePlayMode(string rulesetName)
        {
            _globalRuleset = Ruleset.FromRulesetName(rulesetName);
            _globalScoreInfo = _globalRuleset.CreateScoreInfo();
        }
        
        private int GetDifficultyMul(double val)
        {
            if (val < 0) throw new ArgumentOutOfRangeException(nameof(val),"Difficulty must greater than or equals to 0.");
            if (val >= 0 && val <= 5) return 2;
            if (val >= 6 && val <= 12) return 3;
            if (val >= 13 && val <= 17) return 4;
            if (val >= 18 && val <= 24) return 5;
            if (val >= 25 && val <= 30) return 6;
            if (val > 30) return 6;
            return -1;
        }

        private void ReadFromOsuDb(OsuBeatmap osuBeatmap)
        {
            _globalBeatmap = osuBeatmap.ToBeatmap();
            _globalBeatmap.Stars = osuBeatmap.Stars;
            _duration = osuBeatmap.TotalTime;
            _globalHitObejcts = _globalBeatmap.HitObjects;
            _globalBreakTimes = _globalBeatmap.BreakTimes;
            _globalTimingPoints = _globalBeatmap.TimingPoints;
        }

        private void ReadFromOrtdp(OsuRTDataProvider.BeatmapInfo.Beatmap beatmap)
        {
            _globalBeatmap = new Beatmap(beatmap.FilenameFull);
            _globalBeatmap.Stars = _rtppInfo.BeatmapTuple.RealTimeStars;
            _duration = TimeSpan.FromMilliseconds(_rtppInfo.BeatmapTuple.Duration);
            _globalHitObejcts = _globalBeatmap.HitObjects;
            _globalBreakTimes = _globalBeatmap.BreakTimes;
            _globalTimingPoints = _globalBeatmap.TimingPoints;
        }
        
        private void ListenerManagerOnOnBeatmapChanged(OsuRTDataProvider.BeatmapInfo.Beatmap map)
        {
            if (map == null)
            {
                return;
            }

            byte[] beatmapBytes = File.ReadAllBytes(map.FilenameFull);
            string beatmapHash = MD5String.GetMd5String(beatmapBytes);

            OsuBeatmap osuBeatmap = _osuBeatmapDb.Beatmaps.FindByMd5(beatmapHash);
            if (osuBeatmap != null)
            {
                ReadFromOsuDb(osuBeatmap);
            }
            else
            {
                ReadFromOrtdp(map);
            }
        }

    }
}