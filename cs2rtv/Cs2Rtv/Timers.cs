using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;

namespace Cs2Rtv;

public partial class Cs2Rtv 
{
    private void KillTimer() 
    {
        try
        {
            canRtvTimer?.Kill();
            mapTimer?.Kill();
            rtvTimer?.Kill();
            changeMapRepeat?.Kill();
            repeatTimer?.Kill();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error killing timers");
        }
    }

    private void StartMapTimer() 
    {
        Server.NextFrame(() => 
        {
            try
            {
                mapTimer = AddTimer(TimerConfig.MapTimerInterval, StartMapTimerHandler, TimerFlags.STOP_ON_MAPCHANGE);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to start map timer");
                Utils.RetryTimer(() => StartMapTimer(), TimerConfig.ErrorRetryInterval);
            }
        });
    }

    private void StartMapTimerHandler() 
    {
        try
        {
            timeLeft--;
            if (timeLeft % TimerConfig.MapWarningThreshold == 0 && timeLeft > 0) 
            {
                Utils.BroadcastMessage($"距离投票下一张地图还有{timeLeft}分钟");
            }

            if (timeLeft <= 0) 
            {
                isRtving = true;
                RepeatBroadcast(TimerConfig.DefaultRepeatCount, TimerConfig.RepeatInterval, "当前地图时长还剩5分钟");
            } 
            else 
            {
                mapTimer = AddTimer(TimerConfig.MapTimerInterval, StartMapTimerHandler, TimerFlags.STOP_ON_MAPCHANGE);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in map timer handler");
            Utils.RetryTimer(() => StartMapTimerHandler(), TimerConfig.ErrorRetryInterval);
        }
    }

    private void EndMapTimer() 
    {
        Server.NextFrame(() => 
        {
            try
            {
                mapTimer = AddTimer(TimerConfig.MapTimerInterval, EndMapTimerHandler, TimerFlags.STOP_ON_MAPCHANGE);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to start end map timer");
                Utils.RetryTimer(() => EndMapTimer(), TimerConfig.ErrorRetryInterval);
            }
        });
    }

    private void EndMapTimerHandler() 
    {
        try
        {
            timeLeft--;
            if (timeLeft <= 0)
                ChangeMapRepeat(nextMap!);
            else 
            {
                if (timeLeft is < 2 and > 0)
                    Utils.BroadcastMessage("距离换图还有60秒");
                mapTimer = AddTimer(TimerConfig.MapTimerInterval, EndMapTimerHandler, TimerFlags.STOP_ON_MAPCHANGE);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in end map timer handler");
            Utils.RetryTimer(() => EndMapTimerHandler(), TimerConfig.ErrorRetryInterval);
        }
    }

    private void CanRtvTimer() 
    {
        canRtv = false;
        Server.NextFrame(() => 
        {
            try
            {
                canRtvTimer = AddTimer(TimerConfig.RtvCooldown * 60f, () => canRtv = true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to start RTV cooldown timer");
                Utils.RetryTimer(() => CanRtvTimer(), TimerConfig.ErrorRetryInterval);
            }
        });
    }

    private void ChangeMapRepeat(Map map) 
    {
        try
        {
            var music = mapendmusiclist[random.Next(0, mapendmusiclist.Count - 1)];
            RepeatBroadcast(TimerConfig.DefaultRepeatCount, TimerConfig.RepeatInterval, $"即将更换地图为{map.name}......");
            ChangeMapRepeatHandler(map, TimerConfig.MaxRetryAttempts);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start map change repeat");
            Utils.RetryTimer(() => ChangeMapRepeat(map), TimerConfig.ErrorRetryInterval);
        }
    }

    private void ChangeMapRepeatHandler(Map map, int tryRound) 
    {
        try
        {
            changeMapRepeat = AddTimer(TimerConfig.ChangeMapRetryInterval, () => 
            {
                tryRound--;
                if (tryRound < 0) 
                {
                    map = mapList[random.Next(0, mapList.Count - 1)];
                }

                Server.ExecuteCommand($"host_workshop_map {map.id}");
                ChangeMapRepeatHandler(map, tryRound);
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in map change repeat handler");
            Utils.RetryTimer(() => ChangeMapRepeatHandler(map, tryRound), TimerConfig.ErrorRetryInterval);
        }
    }

    private void RepeatBroadcast(int repeatRound, float eachRepeatTime, string chatMessage) 
    {
        try
        {
            repeatTimer = AddTimer(TimerConfig.RepeatInterval, () => 
            {
                try
                {
                    if (repeatRound <= 0) 
                    {
                        Server.NextFrame(StartRtv);
                    } 
                    else 
                    {
                        foreach (var player in Utils.GetPlayers()) 
                        {
                            player.EmitSound("Alert.WarmupTimeoutBeep", new Dictionary<string, float> { { "volume", 1.0f }, { "pitch", 1.0f } });
                            Utils.BroadcastMessage(chatMessage, player);
                        }

                        repeatRound--;
                        RepeatBroadcast(repeatRound, eachRepeatTime, chatMessage);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error in repeat broadcast handler");
                    Utils.RetryTimer(() => RepeatBroadcast(repeatRound, eachRepeatTime, chatMessage), TimerConfig.ErrorRetryInterval);
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start repeat broadcast");
            Utils.RetryTimer(() => RepeatBroadcast(repeatRound, eachRepeatTime, chatMessage), TimerConfig.ErrorRetryInterval);
        }
    }
}
