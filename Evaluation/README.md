# Evaluation Runner

This folder contains a deterministic evaluation harness for the backend
assistant. It sends predefined simulated pipe-puzzle states to the backend and
exports response, latency, context, memory, and fallback metrics.

The runner is designed for dissertation comparisons. It does not require manual
Unity playtesting, but it does require the backend stack to be running.

## What It Measures

The default evaluation compares:

- Ollama and OpenAI provider configurations
- no-context and full-context prompts
- full-context prompts with and without semantic memory
- latency and response size
- memory retrieval behavior
- LLM fallback/error behavior

Optional minimal-context configurations can also be included.

## Backend Requirements

Start the backend from the repository root:

```bash
cd Backend
./run.sh
```

Rebuild first if backend dependencies or Docker files changed:

```bash
cd Backend
./run.sh --build
```

The backend must expose these evaluation endpoints:

```text
POST /evaluation/run
POST /evaluation/memory/seed
POST /evaluation/memory/reset
```

These endpoints are separate from the normal Unity `/hints` endpoint, so
evaluation runs do not change the in-game assistant flow.

## Basic Run

Run commands from the repository root.

Windows `cmd`:

```bat
py Evaluation\run_evaluation.py --backend-url http://localhost:8000 --output-dir Evaluation\exports --repetitions 3 --seed-memory --reset-evaluation-memory
```

PowerShell:

```powershell
python .\Evaluation\run_evaluation.py --backend-url http://localhost:8000 --output-dir .\Evaluation\exports --repetitions 3 --seed-memory --reset-evaluation-memory
```

Linux, macOS, WSL, or Git Bash:

```bash
python Evaluation/run_evaluation.py --backend-url http://localhost:8000 --output-dir Evaluation/exports --repetitions 3 --seed-memory --reset-evaluation-memory
```

Use `python` or `py` according to how Python is installed on the machine.

## Quick Smoke Test

Before running the full comparison, run one provider with one repetition:

Windows `cmd`:

```bat
py Evaluation\run_evaluation.py --backend-url http://localhost:8000 --output-dir Evaluation\exports --provider-filter openai --repetitions 1 --seed-memory --reset-evaluation-memory
```

Bash:

```bash
python Evaluation/run_evaluation.py --backend-url http://localhost:8000 --output-dir Evaluation/exports --provider-filter openai --repetitions 1 --seed-memory --reset-evaluation-memory
```

Use `--provider-filter ollama` instead if you want to smoke-test the local
provider.

## Useful Options

```text
--backend-url http://localhost:8000
--test-cases Evaluation/test_cases.json
--output-dir Evaluation/exports
--provider-filter ollama|openai|all
--repetitions 3
--no-memory-configs
--include-minimal-context
--seed-memory
--reset-evaluation-memory
--evaluation-user-id 9001
--evaluation-session-id 9001
--timeout 90
```

The default provider filter is `all`, so a full run evaluates both Ollama and
OpenAI. This can take time and may consume OpenAI API credits.

## Controlled Memory Seeding

Memory-enabled configurations are only meaningful if relevant memories exist in
Qdrant. The runner therefore supports controlled, deterministic memory seeding.

Use:

```text
--seed-memory
```

to insert predefined pipe-puzzle memories for the evaluation identity.

Use:

```text
--reset-evaluation-memory
```

to clear old memories for that evaluation user/session before seeding. This
keeps repeated runs reproducible.

The default evaluation identity is:

```text
user_id=9001
session_id=9001
```

Override it when needed:

Windows `cmd`:

```bat
py Evaluation\run_evaluation.py --seed-memory --reset-evaluation-memory --evaluation-user-id 9100 --evaluation-session-id 9100
```

Bash:

```bash
python Evaluation/run_evaluation.py --seed-memory --reset-evaluation-memory --evaluation-user-id 9100 --evaluation-session-id 9100
```

Seeded memories include examples such as:

- curved pipes help when flow direction must change
- the user previously confused straight and curved pipes
- the user previously focused on proximity instead of shape compatibility
- the user prefers gradual, non-spoiler hints
- the user previously needed help finding a missing slot
- slot matching can be inferred from pipe shape and connection direction

The seeded memory point IDs are deterministic, so reseeding the same identity
updates the same records instead of creating uncontrolled duplicates.

## Keeping Memory Ablation Valid

For `memory_enabled=true` rows, at least some test cases should show:

```text
retrieved_memory_count > 0
retrieved_memories_included=true
memory_retrieval_latency_ms > 0
```

For `memory_enabled=false` rows, the runner and backend should keep:

```text
retrieved_memory_count=0
retrieved_memories_included=false
```

This ensures the dissertation comparison is between the same test cases with
and without semantic memory.

## Default Configurations

By default, the runner evaluates:

```text
ollama, context_mode=none, memory_enabled=false
ollama, context_mode=full, memory_enabled=false
ollama, context_mode=full, memory_enabled=true
openai, context_mode=none, memory_enabled=false
openai, context_mode=full, memory_enabled=false
openai, context_mode=full, memory_enabled=true
```

Add minimal-context ablations with:

```text
--include-minimal-context
```

This adds:

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

The export files are overwritten on each run. Rename or copy previous exports
if you need to preserve multiple result sets.

CSV files are written with a UTF-8 byte-order mark so Excel on Windows can open
model responses with punctuation such as curly quotes and dashes correctly.

## Important CSV Columns

`evaluation_results.csv` contains one row per generated response. Useful
columns include:

```text
evaluation_run_id
evaluation_id
test_case_id
provider
model
context_mode
context_enabled
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
retrieved_memories_included
estimated_prompt_tokens
estimated_response_tokens
success
error_message
fallback_used
```

`manual_annotation_dataset.csv` repeats the response rows and adds empty manual
rating columns:

```text
expected_behavior_notes
contextual_relevance_score
helpfulness_score
non_spoiler_score
clarity_score
correctness_score
comments
```

`error_metrics.csv` records runner/backend/provider failures and fallback
events.

`evaluation_summary.csv` groups rows by provider, context mode, and memory
setting and reports success rate plus average latency/length metrics.

## Troubleshooting

- If every request fails with a connection error, confirm the backend is running
  at `http://localhost:8000`.
- If OpenAI rows fail, confirm the backend OpenAI key file is configured.
- If Ollama rows fail, confirm Ollama is running and the configured model exists
  on the host.
- If memory-enabled rows retrieve no memories, run with
  `--seed-memory --reset-evaluation-memory` and confirm Qdrant plus OpenAI
  embeddings are working.
- If a full run is too slow, use `--provider-filter openai` or
  `--provider-filter ollama` and lower `--repetitions`.
