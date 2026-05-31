# Evaluation Runner

This folder contains a deterministic evaluation harness for the context-aware
assistant backend. It does not depend on manually testing the Unity scene.
Instead, it sends predefined simulated pipe-puzzle states to the backend and
records generated responses plus latency and ablation metrics.

## Backend

Start the backend as usual before running evaluations.

From `Backend`:

```bash
./run.sh
```

or, when a rebuild is needed:

```bash
./run.sh --build
```

The backend exposes:

```text
POST /evaluation/run
```

The endpoint reuses the normal hint generation stack: `HintService`,
`ContextBuilder`, `PromptBuilder`, the configured LLM providers, and the
existing Qdrant memory retrieval path. The normal `/hints` endpoint used by
Unity is unchanged.

## Running

From the repository root:

```bash
python Evaluation/run_evaluation.py --backend-url http://localhost:8000 --output-dir Evaluation/exports --repetitions 3
```

Useful options:

```text
--provider-filter ollama|openai|all
--no-memory-configs
--include-minimal-context
--seed-memory
--reset-evaluation-memory
--evaluation-user-id 9001
--evaluation-session-id 9001
--test-cases Evaluation/test_cases.json
--timeout 90
```

Each run receives a unique `evaluation_run_id`. The runner continues if one
provider, configuration, or test case fails.

## Controlled Memory Seeding

Memory-enabled evaluation runs use Qdrant conversation memory. If no controlled
memory exists for the evaluation user/session, `retrieved_memory_count` can be
0 even when `memory_enabled=true`. That makes the memory ablation invalid
because the memory and no-memory configurations are effectively the same.

To make the comparison reproducible, the runner can seed deterministic pipe
puzzle memories before the test cases run. These memories cover curved pipes,
straight-vs-curved confusion, incorrect placement caused by relying only on
proximity, gradual hint preference, missing final pipe identification, and
matching by shape plus connection direction.

Recommended memory-ablation command:

```bash
python Evaluation/run_evaluation.py --backend-url http://localhost:8000 --output-dir Evaluation/exports --repetitions 3 --seed-memory --reset-evaluation-memory
```

The runner uses a stable evaluation identity by default:

```text
user_id=9001
session_id=9001
```

Override these if needed:

```bash
python Evaluation/run_evaluation.py --seed-memory --reset-evaluation-memory --evaluation-user-id 9100 --evaluation-session-id 9100
```

`--reset-evaluation-memory` deletes old Qdrant chat memories for that exact
evaluation user/session before seeding. The seeded memory point IDs are
deterministic, so repeated seeding for the same identity updates the same seed
records instead of creating duplicates.

To verify memory was actually used, inspect `evaluation_results.csv` rows where:

```text
memory_enabled=true
retrieved_memory_count > 0
retrieved_memories_included=true
memory_retrieval_latency_ms > 0
```

For valid ablation, rows with `memory_enabled=false` should keep:

```text
retrieved_memory_count=0
retrieved_memories_included=false
```

## Default Configurations

The default configurations are:

```text
ollama, context_mode=none, memory_enabled=false
ollama, context_mode=full, memory_enabled=false
ollama, context_mode=full, memory_enabled=true
openai, context_mode=none, memory_enabled=false
openai, context_mode=full, memory_enabled=false
openai, context_mode=full, memory_enabled=true
```

Add `--include-minimal-context` to also run:

```text
ollama, context_mode=minimal, memory_enabled=false
openai, context_mode=minimal, memory_enabled=false
```

## Output Files

Outputs are written to `Evaluation/exports` by default:

```text
evaluation_results.csv
evaluation_results.jsonl
manual_annotation_dataset.csv
error_metrics.csv
evaluation_summary.csv
```

`evaluation_results.csv` contains one raw row per generated response. It is not
averaged, so repetitions remain available for statistical analysis.

Important columns include:

```text
evaluation_run_id
evaluation_id
user_id
session_id
test_case_id
test_case_description
provider
model
context_mode
memory_enabled
user_query
context_summary
generated_response
total_latency_ms
context_building_latency_ms
memory_retrieval_latency_ms
llm_generation_latency_ms
prompt_length_chars
response_length_chars
retrieved_memory_count
success
error_message
fallback_used
```

`manual_annotation_dataset.csv` repeats the response rows and adds empty manual
rating columns:

```text
contextual_relevance_score
helpfulness_score
non_spoiler_score
clarity_score
correctness_score
comments
```

`error_metrics.csv` contains failed runner/backend/provider requests and LLM
fallback events. `evaluation_summary.csv` groups raw rows by provider, context
mode, and memory setting and reports success rate and average latency/length
metrics.

## Dissertation Use

Use `evaluation_results.csv` for raw quantitative comparisons such as latency,
prompt size, response size, fallback frequency, and success rate. Use
`manual_annotation_dataset.csv` for later human scoring of contextual
relevance, helpfulness, clarity, non-spoiler behavior, and correctness.

## Assumptions

Ollama must be reachable through the backend's `OLLAMA_BASE_URL`. OpenAI runs
require the backend's OpenAI API key secret to be configured. Memory-enabled
configurations use the existing Qdrant and embedding setup; if embedding
configuration is unavailable, memory retrieval returns no memories and the
request still continues.
