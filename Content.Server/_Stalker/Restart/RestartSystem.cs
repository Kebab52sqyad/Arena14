using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Events;
using Content.Server.Spawners.Components;
using Robust.Server;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System.Linq;
using System.Numerics;
using Content.Server.GameTicking;
using Content.Server.Voting.Managers;
using Content.Shared.GameTicking;
using Content.Shared.Voting;

namespace Content.Server._Stalker.Restart;

public partial class RestartSystem : EntitySystem
{
    [Dependency] private readonly IBaseServer _server = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    private ISawmill _sawmill = default!;
    private readonly HashSet<string> _usedHomeCommand = new();


    private readonly TimeSpan _updateDelay = TimeSpan.FromSeconds(60f);
    private readonly TimeSpan _teleportDelay = TimeSpan.FromMinutes(5f);
    private TimeSpan _updateTime;
    private bool _restartVoteLaunched = false;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStartArena); // arena-changes
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnLobbyArena);

        base.Initialize();
        _sawmill = _logManager.GetSawmill("Restart");
        InitializeCommands();
    }

    //arena-changes-start

    private void OnLobbyArena(PlayerJoinedLobbyEvent ev)
    {
        if (_restartVoteLaunched)
            return;

        var mgr = IoCManager.Resolve<IVoteManager>();
        mgr.CreateStandardVote(null, StandardVoteType.Map);
        _restartVoteLaunched = true;
    }
    private void OnRoundStartArena(RoundStartingEvent ev)
    {
        StartRestart(TimeSpan.FromMinutes(25));
        //var mgr = IoCManager.Resolve<IVoteManager>();

    }
    // arena-changes-end

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_updateTime > _timing.CurTime)
            return;

        _updateTime = _timing.CurTime + _updateDelay;

        var data = GetData();
        if (data.Comp.Time == TimeSpan.Zero)
            return;

        if (data.Comp.Time <= _timing.CurTime)
        {
            _server.Shutdown(null);
            return;
        }

        if (data.Comp.IntervalLast >= _timing.CurTime)
            return;

        var delta = data.Comp.Time - _timing.CurTime;
        _chat.DispatchServerAnnouncement($"Перезапуск сервера через: {Math.Round(delta.TotalMinutes, 1)} минут");
        if (delta < _teleportDelay)
        {
            _chat.DispatchServerAnnouncement($"Вы можете использовать команду home для быстрого возврата в Чистилище");
        }

        data.Comp.IntervalLast = _timing.CurTime + data.Comp.IntervalDelay;
    }

    public void StartRestart(TimeSpan delay)
    {
        var data = GetData();
        _chat.DispatchServerAnnouncement($"Запущен авто-рестарт сервера через: {Math.Round(delay.TotalMinutes, 1)} минут");

        data.Comp.Time = _timing.CurTime + delay;
        data.Comp.IntervalLast = _timing.CurTime + data.Comp.IntervalDelay;
        _usedHomeCommand.Clear();
        _updateTime = TimeSpan.Zero;
    }

    public void TpToPurgatory(IConsoleShell shell)
    {
        // arena changes
    }

    private Entity<RestartComponent> GetData()
    {
        var query = EntityQueryEnumerator<RestartComponent>();
        while (query.MoveNext(out var uid, out var restart))
        {
            return (uid, restart);
        }

        var entity = Spawn(null, MapCoordinates.Nullspace);
        var component = EnsureComp<RestartComponent>(entity);

        return (entity, component);
    }
}
