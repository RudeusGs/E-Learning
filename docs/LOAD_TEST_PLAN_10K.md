# Load-test plan for a 10,000-user deployment

The capacity target must be expressed as concurrent activity, not registered accounts. Start with three profiles and adjust from product analytics:

| Profile               | Concurrent signed-in users | Concurrent video players | Approx. heartbeat RPS at 30s |
| --------------------- | -------------------------: | -----------------------: | ---------------------------: |
| Normal class day      |                      1,000 |                      300 |                           10 |
| Busy period           |                      3,000 |                    1,500 |                           50 |
| Deliberate worst case |                     10,000 |                   10,000 |                          333 |

Test a realistic mix rather than only heartbeat traffic: catalog reads, lesson detail reads, 30-second video heartbeats, checkpoint answers, reinforcement answers, completion requests and Admin progress reads.

Suggested acceptance targets before launch:

- no authorization or completion-rule failures under load;
- API p95 < 500 ms for heartbeat/answer and p99 < 1 s under the agreed busy profile;
- 429 rate remains near zero for compliant clients;
- DB pool has headroom and does not exhaust;
- PostgreSQL CPU and write IOPS remain below sustained saturation;
- no duplicate LessonProgress rows or lost completion timestamps during concurrent requests;
- scale-out test confirms API instances remain stateless.

The application-level global concurrency limiter defaults to 100 requests per API instance. Do not raise it blindly: measure DB capacity first, then tune the limit and instance count together.
