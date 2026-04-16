using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCore;

public sealed class CoachmarkService : ICoachmarkService
{
    private readonly cpmRunningContext _db;
    private readonly ICoachmarkDefinitionProvider _definitions;

    public CoachmarkService(cpmRunningContext db, ICoachmarkDefinitionProvider definitions)
    {
        _db          = db;
        _definitions = definitions;
    }

    // ── GetActiveForPageAsync ─────────────────────────────────────────────────

    public async Task<CoachmarkFlow?> GetActiveForPageAsync(
        string pageKey, int userId, CancellationToken ct = default)
    {
        var definitions = _definitions.GetForPage(pageKey);
        if (definitions.Count == 0) return null;

        var now = DateTime.UtcNow;

        // Splits singles en sequences
        var singles = definitions.Where(d => d.SequenceKey is null).ToList();
        var sequences = definitions
            .Where(d => d.SequenceKey is not null)
            .GroupBy(d => d.SequenceKey!)
            .ToDictionary(g => g.Key, g => g.OrderBy(d => d.StepIndex).ToList());

        // Haal state op voor alle relevante keys in één query
        var stateKeys = singles.Select(d => d.FeatureKey)
            .Concat(sequences.Keys)
            .ToList();

        var states = await _db.UserCoachmarkState
            .AsNoTracking()
            .Where(s => s.UserId == userId && stateKeys.Contains(s.FeatureKey))
            .ToDictionaryAsync(s => s.FeatureKey, ct);

        // Sequences hebben prioriteit boven singles
        foreach (var (seqKey, steps) in sequences)
        {
            var firstDef = steps[0];
            if (!IsWithinWindow(firstDef, now)) continue;

            var state = states.GetValueOrDefault(seqKey);
            if (state?.Dismissed == true || state?.Completed == true) continue;

            int currentStep = state?.CurrentStep ?? 0;
            if (currentStep >= steps.Count) continue;

            return new CoachmarkFlow
            {
                StateKey    = seqKey,
                IsSequence  = true,
                CurrentStep = currentStep,
                Steps       = steps.Select(ToStepData).ToList()
            };
        }

        // Singles: eerste geldig en niet-gezien
        foreach (var def in singles.OrderBy(d => d.ReleaseDate))
        {
            if (!IsWithinWindow(def, now)) continue;

            var state = states.GetValueOrDefault(def.FeatureKey);
            if (state?.Dismissed == true || state?.Completed == true) continue;

            return new CoachmarkFlow
            {
                StateKey    = def.FeatureKey,
                IsSequence  = false,
                CurrentStep = 0,
                Steps       = [ToStepData(def)]
            };
        }

        return null;
    }

    // ── MarkShownAsync ────────────────────────────────────────────────────────

    public async Task MarkShownAsync(string stateKey, int userId, CancellationToken ct = default)
    {
        var state = await GetOrCreateStateAsync(stateKey, userId, ct);
        var now = DateTime.UtcNow;

        state.LastShownAt  = now;
        state.ShowCount   += 1;
        state.FirstShownAt ??= now;

        await _db.SaveChangesAsync(ct);
    }

    // ── DismissAsync ──────────────────────────────────────────────────────────

    public async Task DismissAsync(string stateKey, int userId, CancellationToken ct = default)
    {
        var state = await GetOrCreateStateAsync(stateKey, userId, ct);
        var now = DateTime.UtcNow;

        state.Dismissed   = true;
        state.DismissedAt = now;
        state.LastShownAt = now;

        await _db.SaveChangesAsync(ct);
    }

    // ── CompleteAsync ─────────────────────────────────────────────────────────

    public async Task CompleteAsync(string stateKey, int userId, CancellationToken ct = default)
    {
        var state = await GetOrCreateStateAsync(stateKey, userId, ct);
        var now = DateTime.UtcNow;

        state.Completed   = true;
        state.CompletedAt = now;
        state.LastShownAt = now;

        await _db.SaveChangesAsync(ct);
    }

    // ── AdvanceStepAsync ──────────────────────────────────────────────────────

    public async Task AdvanceStepAsync(string stateKey, int userId, int step, CancellationToken ct = default)
    {
        var state = await GetOrCreateStateAsync(stateKey, userId, ct);

        // Nooit teruggaan via de server; stap vooruit is voldoende
        if (step > state.CurrentStep)
        {
            state.CurrentStep = step;
            state.LastShownAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<UserCoachmarkState> GetOrCreateStateAsync(
        string stateKey, int userId, CancellationToken ct)
    {
        var state = await _db.UserCoachmarkState
            .FirstOrDefaultAsync(s => s.UserId == userId && s.FeatureKey == stateKey, ct);

        if (state is null)
        {
            state = new UserCoachmarkState
            {
                UserId     = userId,
                FeatureKey = stateKey
            };
            _db.UserCoachmarkState.Add(state);
        }

        return state;
    }

    private static bool IsWithinWindow(CoachmarkDefinition def, DateTime now)
    {
        if (now < def.ReleaseDate) return false;
        if (def.MaxShowDays > 0 && now > def.ReleaseDate.AddDays(def.MaxShowDays)) return false;
        return true;
    }

    private static CoachmarkStepData ToStepData(CoachmarkDefinition def) => new()
    {
        TargetSelector = def.TargetSelector,
        Title          = def.Title,
        Message        = def.Message,
        Placement      = def.Placement.ToString().ToLowerInvariant()
    };
}
