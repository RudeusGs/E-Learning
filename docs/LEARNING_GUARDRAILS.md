# Learning Guardrails

Updated: 2026-08-29

## Goal

A lesson is not completed by clicking a button. Completion is server-authoritative and requires the learner to satisfy the configured learning gates.

## Completion policy

For a Published lesson that the current Student is allowed to access:

1. A LessonProgress row must exist. Direct completion before Start is rejected.
2. If the lesson has a video, Admin must configure a trusted `VideoDurationSeconds`.
3. Video progress is accepted only through periodic heartbeat requests. The server caps forward progress by elapsed wall-clock time and by the next unanswered video checkpoint.
4. A `VIDEO_CHECKPOINT` question pauses the learning flow at `VideoTimestampSeconds`; the learner must answer that question correctly before progress may advance beyond that point.
5. `REINFORCEMENT` questions are locked until the required video has been watched to the end.
6. A reinforcement question is counted from the latest submitted answer for that question; learners may retry, and the current score updates accordingly. A video checkpoint remains passed once it has been answered correctly.
7. Lesson completion requires reinforcement score > 80%. If the lesson has no reinforcement questions, the quiz gate is considered satisfied.
8. `Complete` is still idempotent after the lesson is already Completed.

The 80% threshold is centralized in `LessonCompletionPolicy.RequiredQuizScorePercent`.

## Video behavior

The Vue player uses the YouTube IFrame API with native controls and keyboard seek disabled. The application adds these UX controls:

- forward seek is corrected back to the trusted maximum;
- rewind is allowed;
- playback rate is forced to 1x;
- changing browser tab or window focus pauses playback;
- a server heartbeat is sent approximately every 30 seconds while playing;
- a checkpoint pauses video and blocks progress until answered correctly.

### Security limitation of YouTube

A public YouTube video cannot be made cryptographically unskippable by a web application. A determined learner can modify browser code or call APIs directly. Therefore completion eligibility is never trusted from browser state alone: the server checks its own heartbeat progress, checkpoint attempts and reinforcement score.

For high-stakes assessment where media anti-tampering is mandatory, move lesson video to controlled HLS/DASH delivery with signed short-lived segment URLs and server-side viewing telemetry. YouTube is acceptable for ordinary course compliance, not a DRM boundary.

## Scale notes for ~10,000 users

Do not send `timeupdate` to the API every second. At a 30-second heartbeat interval, 10,000 simultaneously playing users would produce about 333 heartbeat requests/second; realistic concurrency should be measured, not assumed. Queries are scoped to one Student/Lesson and completion aggregates use indexed StudentAnswer/LessonProgress paths.

Required production work before launch:

- run load tests at expected concurrent video-user counts, not only total registered users;
- monitor p95/p99 heartbeat and answer latency, DB CPU, connection pool saturation and write IOPS;
- keep API instances stateless so horizontal scaling is possible;
- use PgBouncer or appropriately sized Npgsql pooling when measured concurrency requires it;
- retain cursor pagination on admin reports and student catalog;
- rate-limit abusive answer/heartbeat traffic per authenticated user if production telemetry shows abuse.
