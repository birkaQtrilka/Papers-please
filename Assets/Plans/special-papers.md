# Project Overview
- **Game Title:** Papers please (current project/product title).
- **High-Level Concept:** Extend the existing desk-based document approval game with optional special papers that trade risks for scoring advantages. Reusable effect assets define behavior while per-round runtime instances track durations and stacking.
- **Players:** Existing single-player game; no networking changes.
- **Inspiration / Reference Games:** Existing Papers, Please-style document inspection loop; no new reference-driven redesign.
- **Tone / Art Direction:** Preserve the existing stylized/pixelated office, physical paper presentation, stamping, sounds and feedback.
- **Target Platform:** WebGL, Unity 6000.6.0f1; no platform/package changes.
- **Screen Orientation / Resolution:** Existing landscape HUD uses a 1920×1080 CanvasScaler reference; Web player default is 960×600. Preserve both settings.
- **Render Pipeline:** URP, current PC_RPAsset.

# Game Mechanics
## Core Gameplay Loop
Keep spawning NPCs with document data, inspecting papers, selecting Approve/Disapprove, stamping and awarding normal-paper points. Special papers use the same delivery and stamping flow but replace the direct scoring step with an accept/skip decision.

### Confirmed requirements and clarification
1. Approve accepts a special paper; Disapprove skips it. Neither decision awards or deducts points, regardless of shouldApprove.
2. User clarified the original end-of-round challenge into a **next-N-paper score multiplier**. Do not implement an N-correct challenge, final-score multiplication or triple failure penalty.
3. Both normal-paper gains and losses are multiplied. Default multiplier example: **N=3, M=2**.
4. Time-trade example: multiply **current remaining time by 0.5**, then award **+100 additional points for each later correct normal paper**, lasting until the round ends. Wrong normal papers retain their normal penalty except for any active multipliers.
5. Independent effects stack. Multiply all active score multipliers; add all active correct-guess bonuses. Repeated time cuts each halve the then-current remainder.
6. Scoring order is deterministic: calculate normal base points (+100 correct / −50 wrong), add correct-only bonuses, then apply the product of active multipliers. Thus one time bonus and one ×2 effect produce +400 on a correct guess and −100 on a wrong guess. This makes accepted combinations meaningful and independent of acceptance order.
7. Every resolved **normal** paper consumes one use of every active next-N multiplier, correct or wrong. Special papers (accepted or skipped), trashed/unresolved papers and result-screen papers do not consume uses or receive bonuses.
8. Activation and scoring occur at the existing ink-on-paper resolution point. The activating special does not benefit from, or consume, its own effect. Rejecting a special does not affect existing effects.
9. Unused multiplier uses disappear when the round ends; there is no additional penalty. Existing negative-score loss and timer-expiration rules remain.

Optional acceptance supports player agency, while short multipliers and time pressure create risk/reward decisions within the existing loop. No progression, backend, random special spawn system or unrelated gameplay changes.

## Controls and Input Methods
- Project Settings activeInputHandler=2 (Both); scene EventSystem uses InputSystemUIInputModule, while existing document raycast controls use legacy mouse input.
- Reuse existing physical Approve/Disapprove interactions. No new bindings, input polling or backend migration.
- Special papers must explain that Approve accepts the terms and Disapprove skips them.

# UI
Preserve the current HUD, score/timer widgets, pause UI and result presentation. No new Canvas panels, menus or font-system work.

Paper presentation mockup using the existing physical paper surface:

```text
+--------------------------------------+
| SPECIAL: DOUBLE POINTS                |
| Next 3 normal papers: points ×2       |
| Gains AND losses are doubled.         |
| [APPROVE: ACCEPT] [DISAPPROVE: SKIP]   |
+--------------------------------------+

+--------------------------------------+
| SPECIAL: TIME TRADE                   |
| Remaining time is halved.             |
| +100 per correct normal paper         |
| until the round ends.                 |
| [APPROVE: ACCEPT] [DISAPPROVE: SKIP]   |
+--------------------------------------+
```

Create two simple readable paper textures with these terms, matching the existing paper aspect/presentation; reuse the physical Approve/Disapprove collider layout without moving it. Terms are baked into the paper texture, not a new runtime text UI. Existing timer display updates immediately after a time cut. Existing score feedback and audio remain for normal-paper judgments; special decisions use stamping feedback without falsely indicating a scored correct/wrong guess. Active-effect HUD counters are outside this change; runtime state remains inspectable for focused verification.

# Key Asset & Context
## Existing assets and configuration
- `Assets/Scripts/DocDataSO.cs`: contains `DocTexture` and `shouldApprove`; all 13 existing document assets reference this script. Add an optional reusable-effect asset list with a safe empty/null default. Existing documents remain normal automatically.
- `Assets/Scripts/StampData.cs`: carries Approved, DocData, PaperObject and PaperModel. Preserve this signature unless internal resolution helpers need a new separate type.
- `Assets/Scripts/StampManager.cs`: currently owns public `Score`, +100/−50 scoring and animations. Score=200 is serialized in SampleScene. Preserve Score and existing public event/method signatures and initial scene value. Pass full StampData through the stamping animation rather than discarding document metadata.
- `Assets/Scripts/QueueManager.cs`: consumes its document list from the end. Connected to PaperApproveManager.PaperEvaluated → OnPaperEvaluated → QueueManager.PaperEvaluated → StampManager.StampPaper. Adds CheckScore to InkStamped and CallOffNPCs to TimerCompleted at runtime. Current StartGameWin runs before the last asynchronous stamp resolves; correct this as part of integration.
- `Assets/Scripts/UI/CountdownTimer.cs`: private timeRemaining, starts automatically with serialized duration=600; add read-only remaining/running properties and safe remaining-time adjustment/stop methods without restarting the countdown.
- `Assets/Scripts/NPC.cs`: SetDocData assigns the paper texture. Existing presentation can remain unchanged.
- `Assets/Scenes/SampleScene.unity`: wire the runtime effect controller and insert two example special documents without overwriting normal document assets or removing normal papers.
- `Assets/Prefabs/Paper.prefab`, `Assets/Prefabs/NPC.prefab`: reuse unchanged. Result NPC prefabs remain separate and unscored.
- `Assets/Scenes/StampTest.unity` and `Assets/Scripts/Tests/StampTest.cs`: StampManager also appears here; preserve normal-only operation when no special controller is assigned.
- `Assets/Scripts/Editor/DocDataSOGenerator.cs`: default empty effects preserve its existing generated documents; no generator changes required.

## New assets
- `Assets/Scripts/SpecialPapers/SpecialPaperEffectSO.cs`: abstract reusable effect definition, display metadata and runtime-instance factory.
- `Assets/Scripts/SpecialPapers/ISpecialPaperEffect.cs`: runtime lifecycle contract for activation, score contribution, normal-paper completion, expiry and round cleanup. Use plain C# runtime objects with no mutable counters stored in ScriptableObjects.
- `Assets/Scripts/SpecialPapers/SpecialPaperEffectController.cs`: scene-scoped effect manager with accepted effect instances, deterministic additive/multiplicative aggregation and once-only cleanup. Provide a small context exposing safe timer access; do not allow effects to depend on stamping animation internals.
- `Assets/Scripts/SpecialPapers/NextPapersMultiplierEffectSO.cs`: configurable positive paper count and multiplier; separate runtime remaining-use counter.
- `Assets/Scripts/SpecialPapers/TimeTradeBonusEffectSO.cs`: configurable remaining-time fraction (default 0.5) and nonnegative correct-paper bonus; bonus runtime survives until round end.
- `Assets/Resources/SpecialPapers/Effects/Next3DoublePoints.asset` and `TimeHalfBonus100.asset`.
- `Assets/Resources/SpecialPapers/Documents/Next3DoublePoints_DocData.asset` and `TimeHalfBonus100_DocData.asset`.
- `Assets/Images/Documents/SpecialPapers/Next3DoublePoints.png` and `TimeHalfBonus100.png`: readable example textures.
- A focused Editor-only verification helper under `Assets/Scripts/Editor/Tests/` if needed; do not restructure assemblies or introduce a test package just for this feature.

## Dependency/impact findings
AssetDatabase dependency/dependent queries confirm DocDataSO impacts existing document assets; StampManager impacts SampleScene and StampTest; QueueManager and CountdownTimer impact SampleScene; Paper.prefab is used by NPC.prefab and both gameplay/test scenes. Asset dependency queries do not reveal C# references, so source searches also covered Score, InkStamped, StampFinished and OnPaperEvaluated. Recheck affected dependencies before implementation edits. No dependency-specific tool is available in this session; equivalent read-only AssetDatabase queries were used.

# Implementation Steps
1. **Add reusable definitions and runtime contracts.**
   - Create the new SpecialPapers scripts and extend DocDataSO with optional effect references and an IsSpecial classification helper.
   - Definition assets remain configuration only; each acceptance creates fresh state even when the same asset appears repeatedly.
   - Validate N≥1, finite positive M, fraction in (0,1], nonnegative bonus and missing references. Empty lists remain normal; malformed special configurations must warn and avoid accidentally awarding normal points.
   - **Dependencies:** Existing DocDataSO and StampData contracts inspected; no new packages.

2. **Implement safe timer operations.**
   - Add RemainingTime, IsRunning, ScaleRemainingTime(factor) and StopTimer to CountdownTimer.
   - Clamp to zero, refresh text/color/whole-second bookkeeping immediately, preserve initial duration and current paused state, and never restart an expired/stopped timer.
   - Centralize completion so zero completion is dispatched at most once per timer run. Reuse existing pulse behavior rather than duplicating countdown logic.
   - **Dependencies:** Step 1 context contract.

3. **Implement stacking and both effect types.**
   - Controller creates independent runtime effects on special acceptance, safely iterates without mutating collections during callbacks, combines bonuses then multipliers, and consumes uses only after a normal paper receives its points.
   - Multiplier expires immediately after its Nth normal-paper resolution; time cut happens once per acceptance, bonus persists until round end.
   - Use a wider intermediate score type, a documented rounding rule (nearest integer, midpoint away from zero) and safe integer range handling for configurable noninteger/stacked multipliers. Reject nonfinite parameters.
   - Cleanup prevents expired effects or late callbacks from modifying a finished round. Provide queryable active state for verification without a new HUD.
   - **Dependencies:** Steps 1–2.

4. **Integrate stamping without changing its public entry points.**
   - Carry StampData into MoveStamperAnimation. At the current score-resolution moment, normal papers use existing correctness and base scoring plus controller modifiers; special papers accept or skip with zero direct delta.
   - Keep existing InkPressed, InkStamped, StampFinished, PaperTrashed and physical stamping behavior. ScoreChanged should notify genuine score changes; update ScoreUI consistently.
   - Preserve normal correctness audio/green-red feedback, and avoid scored correct/wrong feedback for neutral specials.
   - Normal-only fallback works when no controller exists (StampTest). Special configuration without its required controller warns and skips, not normal-scores.
   - Guard a paper from being submitted/resolved twice; disable its decision colliders on commitment rather than after the movement delay. Suppress scoring/effect activation after round closure.
   - **Dependencies:** Steps 1–3.

5. **Synchronize queue and round completion.**
   - QueueManager defers victory until no documents/queued NPCs remain AND the final submitted paper completes its stamp. Never show a stale final score.
   - Maintain explicit round-ended/pending-paper state; reject invalid submissions when no paper is ready, during terminal states or for result papers. Preserve queue progression events.
   - Make loss/timeout handling idempotent, and clear effects/stop the timer exactly once on all terminal paths. If score becomes negative on the final normal paper, loss takes precedence over victory.
   - Timer expiration ends play immediately; any submitted but not-yet-resolved paper finishes presentation without awarding points or accepting an effect. Guard asynchronous arrivals/spawns from reopening gameplay after completion.
   - Unsubscribe added runtime listeners when destroyed. Limit queue changes to lifecycle correctness necessary for these effects.
   - **Dependencies:** Steps 2–4.

6. **Author and connect examples.**
   - Create effect and document assets with N=3, M=2 and time fraction=0.5, bonus=100, plus printed-term textures.
   - Add the controller to SampleScene and assign StampManager/timer references. Insert examples so both are delivered early enough to try combinations; account for the list being consumed in reverse order.
   - Preserve all existing normal document assets, public event wiring, initial score, duration, prefabs, StartMenu and rendering/input settings.
   - **Dependencies:** Steps 1–5.

7. **Focused verification and cleanup.**
   - Perform checks below using the available Editor tooling. Add only a small Editor-only harness if direct deterministic runtime checks require one, remove temporary assets/tooling afterward, and save intended scene/asset changes.
   - **Dependencies:** Steps 1–6.

# Verification & Testing
Keep checks proportionate; stop after requested behaviors and essential regressions pass.

## Deterministic scoring/effect checks
- No active effects: normal correct +100, wrong −50; existing serialized score starts at 200.
- Accept multiplier: immediate score delta 0; next correct/wrong/correct papers yield +200/−100/+200; fourth correct yields +100.
- Reject either special: score/time/effects unchanged; accepted/skipped specials do not consume existing multiplier uses.
- Time trade: known 80 seconds becomes 40; correct earns +200, wrong −50; repeated acceptance makes remaining time 20 and correct earns +300 without multipliers.
- Combined single time bonus and ×2: correct +400, wrong −100. Two ×2 instances give ×4 and consume independently; activating a second after the first has partly expired preserves separate counters.
- Reusing the same definition asset creates independent instances; counters never change the ScriptableObject and reset on scene reload.

## Essential integration checks
- Play through one normal paper, accept/reject each special, and observe immediate timer change and preserved stamping/queue movement.
- Last paper's score resolves before the win result is populated; final negative score produces loss rather than victory.
- Timeout during stamping ignores late score/effect changes, closes effects once and creates only one terminal NPC; near-zero time cuts never restart an expired timer.
- Repeated clicks cannot double-submit or double-apply; no inputs score result-screen papers.
- Pause remains pause: accepting a time cut cannot restart or unpause the timer. Restart restores fresh effect state.
- Existing StampTest normal-paper flow still works without the new controller.
- Compile without new errors; confirm references/event wiring and readable printed terms at the existing rendering resolution. No new player-only Editor dependencies or package/define changes. A full WebGL build is only necessary if focused checks expose a platform-specific concern.
