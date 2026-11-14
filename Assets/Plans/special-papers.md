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

---

# Paper Menus — Current Requested Extension
This extension is the implementation scope for the current request. Earlier special-paper work is context only: do not reimplement it. The earlier instruction to preserve pause UI/StartMenu is superseded only for the menu changes specified below. Preserve all gameplay document artwork and dynamic special-paper presentation.

# Project Overview
- **Game Title:** Papers please.
- **High-Level Concept:** Give the start menu and in-game pause menu coordinated physical office-form papers. Pause offers a separate options page with master-volume controls, retaining the desk-based presentation rather than a conventional flat menu.
- **Players:** Existing single player.
- **Inspiration / Reference Games:** Existing Papers, Please-style office and the project's `PopParent` start-menu paper.
- **Tone / Art Direction:** User selected **official office forms**: warm ivory, dark ink, ruled borders, subtle wear, muted red/green stamp accents. Decorative artwork stays separate from editable text.
- **Target Platform:** Existing WebGL, Unity 6000.6.0f1. No packages, compatibility guards or scripting defines changed.
- **Screen Orientation / Resolution:** Preserve landscape presentation, existing Canvas scaling and 960×600 Web player defaults. Check paper readability at that size and 1920×1080.
- **Render Pipeline:** Existing URP / PC_RPAsset; no global rendering changes.

# Game Mechanics
## Core Gameplay Loop
No scoring, stamping, queue, timer-duration or special-effect changes. Escape suspends gameplay and displays a physical pause paper. Resume returns to the exact suspended state; Options replaces the page content without unpausing; Back returns to the pause page. Reopening pause starts on its main page.

## Controls and Input Methods
- User selected **entirely physical controls**, not a draggable slider: collider buttons, hover outlines and editable mesh text matching `PopParent`. Volume uses minus/plus buttons and a percentage, in 10-percentage-point steps clamped to 0–100%.
- Project Settings verified `activeInputHandler=2` (Both). The existing EventSystem uses InputSystemUIInputModule; existing SelectManager and PauseManager use legacy polling. Implement changed menu controls with `UnityEngine.InputSystem` event-driven InputActions, without migrating gameplay input.
- Escape preserves its existing toggle behavior: Resume from either pause page. Mouse clicks select the physical button under the displayed render-texture pointer location.
- Preserve current Start and Quit actions. Quit keeps the existing application-exit behavior; WebGL generally cannot close a browser tab, so do not claim that it will. No new return-to-title or restart feature.
- Master volume changes apply through `AudioListener.volume`, preserving all per-source balances. No mixers exist, so no mixer creation/routing overhaul is needed. Volume remains set across pause/resume and scene transitions in the current session; disk/browser persistence is outside this request.

# UI
## Confirmed direction and alternatives
The selected physical −/+ controls maximize consistency with the desk/document aesthetic (MDA) and give explicit percentage feedback supporting competence (SDT). A physical-paper slider would allow finer, faster adjustments but adds pointer-drag mapping complexity; a paper-styled Canvas would be simpler but less faithful to PopParent. User explicitly chose the all-physical design and office-form artwork.

## Paper mockups
```text
START PAPER                    PAUSE PAPER
┌────────────────────────┐     ┌────────────────────────┐
│ OFFICE OF DOCUMENTS    │     │ OFFICE OF DOCUMENTS    │
│ ────────────────────── │     │ ────────────────────── │
│     PAPERS PLEASE      │     │     SHIFT PAUSED       │
│                        │     │                        │
│       [ START ]        │     │      [ RESUME ]        │
│       [ QUIT  ]        │     │      [ OPTIONS ]       │
│                        │     │      [ QUIT ]          │
│  small stamp / form ID │     │  small stamp / form ID │
└────────────────────────┘     └────────────────────────┘

OPTIONS PAGE (same pause paper)
┌────────────────────────┐
│ OFFICE OF DOCUMENTS    │
│ ────────────────────── │
│      AUDIO OPTIONS     │
│                        │
│     MASTER VOLUME      │
│    [ − ]  80%  [ + ]   │
│                        │
│        [ BACK ]        │
│  small stamp / form ID │
└────────────────────────┘
```
- Produce coordinated decorative background PNGs for StartMenu and PauseMenu: lightly textured ivory, double-rule border, header/footer rules and restrained stamp ornamentation. Do not bake button labels, titles or the changing volume percentage into the texture. Use hand-authored/procedural 2D artwork; no external asset-generation service is required.
- Keep generous blank text areas, strong ink contrast and useful click sizes. Artwork must not obscure the existing title and action labels. The options page reuses the pause-paper background.
- Keep the existing PopParent paper geometry and dynamic mesh-text style; add pause content as a separate prefab. Do not edit shared GameOverPaper or PaperBack material in place, as these affect result documents.
- Freeze gameplay without freezing menu input. The existing pause configuration disables Main Camera, HUD and PaperApproveManager, so the pause paper needs its own rendering path. Use an isolated menu-only camera/render texture shown by a non-raycast RawImage above the existing scene image on the existing Canvas. This displays real 3D paper/colliders, not flat Canvas buttons. No additional EventSystem.

# Key Asset & Context
## Verified existing context
- `Assets/Prefabs/PopParent.prefab`: only StartMenu directly depends on it. Contains nested GameOverPaper/Model front and back quads, mesh TextMeshPro title/action text, Start/Quit BoxColliders and first-child Selection highlights. Action colliders use DocumentClick layer 6; Pop layer 8 is also used by its root in StartMenu overrides.
- `Assets/Scripts/StartMenuManager.cs`: subscribes to SelectManager.OnFieldClick, dispatches by collider name `Start`/`Quit`; Start fades then loads build scene index 1. Preserve names and event signatures.
- `Assets/Scripts/SelectManager.cs`: render-camera/renderImageRect pointer mapping, hover events and click events. Two instances are serialized in StartMenu for action selection and pop hover. Its hover logic assumes a first child exists. Preserve existing pop wiring and enable/disable timing.
- `Assets/Scripts/PauseManager.cs`: pausePanel, resumeButton, quitButton and SceneTransitionManager are actually assigned in SampleScene. Pause hides HUD, Main Camera and PaperApproveManager, sets timeScale=0 and pauses sources; Resume currently indiscriminately re-enables objects.
- `Assets/Scenes/SampleScene.unity`: existing PausePanel contains Container/Btns/Btn_Resune and Btn_Quit. Existing Canvas, one EventSystem and Pixelated/NonPixelated render-texture images are present.
- `Assets/Scripts/PlayerCamera.cs`: uses LateUpdate pointer motion even with timeScale=0; disabling gameplay camera/controller must be preserved. Cache and restore cursor state for the clickable pause menu.
- `Assets/Scripts/SceneTransitionManager.cs`: fade routines use scaled time. Preserve existing quit path that restores time before fading; avoid global fade-timing changes.
- AudioSources use individual volume settings; no AudioMixer assets or existing volume-setting code found.
- Dependencies were inspected through read-only AssetDatabase queries (no dedicated dependency tool exposed). PauseManager affects SampleScene; SelectManager affects StartMenu; GameOverPaper affects PopParent and both NPC result prefabs. Recheck modified assets before edits.

## Planned assets
- Modify `Assets/Prefabs/PopParent.prefab`: assign start-specific front artwork material and align its dynamic text/highlights to the official-form design. Preserve nested prefab identity and existing actions.
- Create `Assets/Prefabs/PausePaper.prefab`: reuse the existing physical-paper model pattern with MainPage (Resume, Options, Quit) and OptionsPage (VolumeDown, VolumeUp, Back, dynamic percentage). All hit targets have first-child Selection highlights; decorative geometry has no interactive collider.
- Create `Assets/Images/Documents/MenuPapers/StartMenu_Background.png` and `PauseMenu_Background.png`.
- Create dedicated materials under `Assets/Materials/MenuPapers/` for the two paper fronts and, only if necessary, menu-specific highlight styling. Never recolor shared Green.mat or PaperBack.mat globally.
- Create `Assets/Textures/PauseMenu.renderTexture` for the isolated menu-camera output; determine its dimensions from the existing presentation during implementation, keeping aspect and readable resolution.
- Modify `Assets/Scripts/PauseManager.cs`: physical selection references, page switching, event-driven pause action, master-volume operations and precise state restoration. Keep public Pause(), Resume(), static IsPaused and QuitApplication() contracts. Remove obsolete flat-menu button subscriptions only after rewiring.
- Modify `Assets/Scripts/SelectManager.cs` narrowly: InputAction-based click/pointer handling, safe bounded render-image mapping, optional-child protection, selection reset when disabled/page changes. Preserve public UnityEvent contracts and both StartMenu use cases.
- Modify `Assets/Scenes/SampleScene.unity`: connect pause prefab, isolated camera, Canvas RawImage and selector; retire the old PausePanel presentation without rebuilding the rest of the Canvas. Keep obsolete objects inactive until references are verified, then remove only unused pause-specific elements if safe.
- Modify `Assets/Scenes/StartMenu.unity` only for presentation overrides needed by the new artwork; preserve existing PopEffect, start action and transition wiring. StartMenuManager logic need not change unless a discovered hookup requires a minimal correction.

# Implementation Steps
1. **Confirm edit-time wiring and isolate impact.** Recheck dependencies for modified assets, inspect StartMenu's live prefab overrides, cameras, CanvasScaler and selector enable timing, and identify all references to old pause elements. Record existing render and cursor states. **Dependencies:** approved extension; exploration findings above.
2. **Author coordinated paper backgrounds/materials.** Create the two decorative PNGs and front materials, with dynamic-text safe areas and office-form accents. Apply start material as a PopParent-specific override; leave shared result-paper sources untouched. Verify start-paper appearance before proceeding. **Dependencies:** Step 1.
3. **Build the pause paper incrementally.** Create PausePaper using the existing paper model layout, add the three main-page collider buttons and separate volume/back page, mesh text and selection highlights. MainPage and OptionsPage must be mutually exclusive. Assign dedicated pause material; no unnecessary animated effects. **Dependencies:** Steps 1–2.
4. **Make physical menu selection pause-safe.** Update SelectManager to use enabled/disposed InputActions with click callbacks, pointer-position action and existing hover events. Map pointer through the supplied RawImage rect, reject outside clicks, respect its UV rect and camera output, and clear stale highlights on page/enable changes. Preserve both existing StartMenu selectors' behavior; no gameplay input migration. **Dependencies:** Steps 1 and 3.
5. **Integrate pause state and volume.** Update PauseManager with serialized selector/page/percentage references and explicit handlers for Resume, Options, Quit, VolumeDown, VolumeUp and Back. Enable Escape action through lifecycle subscriptions; guard duplicate pause/resume. Cache prior timeScale, active states and cursor state; restore them instead of blindly reactivating objects. Keep source pause/unpause behavior, do not unpause unrelated already-paused sources, and handle runtime-spawned scene sources as needed for the existing game. Master volume clamps at endpoints and changes only AudioListener.volume; update percentage immediately. Reset static pause state safely on teardown. Quit restores time before the current transition and exit. **Dependencies:** Steps 3–4.
6. **Wire rendering and retire the flat pause panel.** Add an isolated menu camera/output with only pause-paper objects on its culling path (reuse Pop layer 8 for isolated pause objects where safe). Place camera/paper away from gameplay and avoid rendering game colliders. Enable camera, selector and overlay image only while paused. Ensure gameplay image/camera rendering remains unchanged on Resume and no second active AudioListener is introduced. Assign physical actions and disable old Canvas buttons after successful migration. **Dependencies:** Steps 3–5.
7. **Focused verification, save and cleanup.** Run the checks below; fix only task-related regressions, remove temporary tooling and save intended prefabs/assets/scenes. Stop after essential requested behavior works. **Dependencies:** Steps 2–6.

# Verification & Testing
- Compile without new errors; verify assigned references, click layers, exactly one EventSystem and no extra active AudioListener.
- StartMenu: paper artwork is visible, text stays dynamic/readable, Start still loads gameplay with its fade, Quit retains its existing platform behavior, and pop/hover selectors still behave correctly.
- Pause: Escape displays paper with timeScale=0; no camera movement, document selection or timer advancement occurs. Physical Resume/Options/Quit buttons remain responsive despite paused time and disabled Main Camera.
- Options: Options shows only the volume/back page; Back shows only main page. −/+ changes percentage and master gain in 10% steps, clamps at 0/100%, and leaves per-source volume settings unchanged. After Resume, audible gameplay uses the selected master gain. Reopening pause starts at main page with the same session volume.
- Resume and Escape from either page restore prior active-object, cursor and time states; already-inactive objects remain inactive. No stale highlights, double listeners, double clicks or hidden-page hit targets.
- Display: check 960×600 and 1920×1080, pointer alignment near buttons and image edges, no z-fighting/clipping, no UI blocker or scene object obscuring the paper. Result/special document textures and scoring flow remain untouched.
- Quit fade does not stall at timeScale=0. WebGL tab closing is not asserted or tested as supported. A full build is unnecessary unless a platform-specific issue emerges during these focused checks.
