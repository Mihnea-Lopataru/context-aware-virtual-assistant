import argparse
import csv
import json
import sys
import time
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen
from uuid import uuid4


RESULT_COLUMNS = [
    "evaluation_run_id",
    "evaluation_id",
    "request_id",
    "timestamp",
    "user_id",
    "session_id",
    "repetition_index",
    "configuration_id",
    "test_case_id",
    "test_case_description",
    "provider",
    "model",
    "context_mode",
    "context_enabled",
    "memory_enabled",
    "user_query",
    "context_summary",
    "generated_response",
    "total_latency_ms",
    "context_building_latency_ms",
    "memory_retrieval_latency_ms",
    "llm_generation_latency_ms",
    "prompt_length_chars",
    "response_length_chars",
    "retrieved_memory_count",
    "retrieved_memories_included",
    "estimated_prompt_tokens",
    "estimated_response_tokens",
    "success",
    "error_message",
    "fallback_used"
]

MANUAL_COLUMNS = RESULT_COLUMNS + [
    "expected_behavior_notes",
    "contextual_relevance_score",
    "helpfulness_score",
    "non_spoiler_score",
    "clarity_score",
    "correctness_score",
    "comments"
]

ERROR_COLUMNS = [
    "evaluation_run_id",
    "evaluation_id",
    "timestamp",
    "test_case_id",
    "provider",
    "context_mode",
    "memory_enabled",
    "component",
    "error_type",
    "error_message",
    "fallback_used"
]

SUMMARY_COLUMNS = [
    "provider",
    "context_mode",
    "memory_enabled",
    "number_of_requests",
    "success_rate",
    "average_total_latency_ms",
    "average_llm_latency_ms",
    "average_memory_latency_ms",
    "average_prompt_length_chars",
    "average_response_length_chars"
]

DEFAULT_KNOWLEDGE = {
    "environment": "Interactive Unity pipe-puzzle scene.",
    "assistant_goal": (
        "Give short contextual hints that help the player continue without "
        "revealing the complete puzzle solution."
    )
}

DEFAULT_EVALUATION_MEMORIES = [
    {
        "memory_id": "curved_pipe_direction_change",
        "role": "assistant",
        "content": (
            "Earlier, the assistant explained that curved pipes are useful "
            "when the flow direction needs to change rather than continue "
            "straight."
        ),
        "metadata": {"topic": "curved_pipes"}
    },
    {
        "memory_id": "straight_vs_curved_confusion",
        "role": "user",
        "content": (
            "The user previously confused a straight pipe with a curved pipe "
            "and needed help comparing the shape of the pipe against the slot."
        ),
        "metadata": {"topic": "pipe_shape_confusion"}
    },
    {
        "memory_id": "proximity_not_enough",
        "role": "assistant",
        "content": (
            "The assistant previously warned that being near a slot is not "
            "enough; the pipe shape and connection direction also need to "
            "match the slot."
        ),
        "metadata": {"topic": "incorrect_placement"}
    },
    {
        "memory_id": "gradual_hint_preference",
        "role": "user",
        "content": (
            "The user prefers gradual hints that help them reason through the "
            "pipe puzzle without revealing the full solution immediately."
        ),
        "metadata": {"topic": "user_preference"}
    },
    {
        "memory_id": "missing_pipe_help",
        "role": "assistant",
        "content": (
            "The user previously needed help identifying which slot was still "
            "missing a pipe when the puzzle was almost complete."
        ),
        "metadata": {"topic": "almost_complete"}
    },
    {
        "memory_id": "shape_and_direction_rule",
        "role": "assistant",
        "content": (
            "The assistant previously explained that the correct slot can "
            "often be inferred by matching pipe shape and connection direction "
            "instead of guessing from distance alone."
        ),
        "metadata": {"topic": "slot_matching"}
    }
]


def parse_args():
    parser = argparse.ArgumentParser(
        description="Run deterministic backend assistant evaluation cases."
    )
    parser.add_argument(
        "--backend-url",
        default="http://localhost:8000",
        help="Base URL of the running backend service."
    )
    parser.add_argument(
        "--test-cases",
        default=str(Path(__file__).with_name("test_cases.json")),
        help="Path to evaluation test_cases.json."
    )
    parser.add_argument(
        "--output-dir",
        default=str(Path(__file__).with_name("exports")),
        help="Directory where CSV and JSONL outputs are written."
    )
    parser.add_argument(
        "--provider-filter",
        choices=["all", "ollama", "openai"],
        default="all",
        help="Restrict execution to one provider."
    )
    parser.add_argument(
        "--repetitions",
        type=int,
        default=1,
        help="Number of repetitions per test case and configuration."
    )
    parser.add_argument(
        "--no-memory-configs",
        action="store_true",
        help="Skip configurations where memory_enabled=true."
    )
    parser.add_argument(
        "--include-minimal-context",
        action="store_true",
        help="Also run minimal context ablation configurations."
    )
    parser.add_argument(
        "--seed-memory",
        action="store_true",
        help="Seed deterministic evaluation conversation memories before running."
    )
    parser.add_argument(
        "--reset-evaluation-memory",
        action="store_true",
        help="Delete existing memories for the evaluation identity before seeding/running."
    )
    parser.add_argument(
        "--evaluation-user-id",
        type=int,
        default=9001,
        help="Stable user_id used by all evaluation requests and seeded memories."
    )
    parser.add_argument(
        "--evaluation-session-id",
        type=int,
        default=9001,
        help="Stable session_id used by all evaluation requests and seeded memories."
    )
    parser.add_argument(
        "--timeout",
        type=int,
        default=90,
        help="HTTP timeout per evaluation request in seconds."
    )
    return parser.parse_args()


def load_test_cases(path):
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)

    if isinstance(data, list):
        return data

    return data.get("test_cases", [])


def build_configurations(provider_filter, include_memory, include_minimal):
    providers = ["ollama", "openai"]
    if provider_filter != "all":
        providers = [provider_filter]

    configs = []
    for provider in providers:
        provider_configs = [
            ("none", False),
            ("full", False)
        ]

        if include_memory:
            provider_configs.append(("full", True))

        if include_minimal:
            provider_configs.append(("minimal", False))

        for context_mode, memory_enabled in provider_configs:
            configs.append({
                "configuration_id": (
                    f"{provider}_{context_mode}_"
                    f"memory_{str(memory_enabled).lower()}"
                ),
                "provider": provider,
                "context_mode": context_mode,
                "context_enabled": context_mode != "none",
                "memory_enabled": memory_enabled
            })

    return configs


def post_json(url, payload, timeout):
    body = json.dumps(payload).encode("utf-8")
    request = Request(
        url,
        data=body,
        headers={"Content-Type": "application/json"},
        method="POST"
    )

    with urlopen(request, timeout=timeout) as response:
        return json.loads(response.read().decode("utf-8"))


def reset_evaluation_memory(backend_url, user_id, session_id, timeout):
    endpoint = backend_url.rstrip("/") + "/evaluation/memory/reset"
    return post_json(
        endpoint,
        {
            "user_id": user_id,
            "session_id": session_id
        },
        timeout
    )


def seed_evaluation_memory(
    backend_url,
    user_id,
    session_id,
    reset_before_seed,
    timeout
):
    endpoint = backend_url.rstrip("/") + "/evaluation/memory/seed"
    return post_json(
        endpoint,
        {
            "user_id": user_id,
            "session_id": session_id,
            "reset_before_seed": reset_before_seed,
            "memories": DEFAULT_EVALUATION_MEMORIES
        },
        timeout
    )


def build_payload(
    test_case,
    config,
    evaluation_run_id,
    repetition_index,
    evaluation_user_id,
    evaluation_session_id
):
    evaluation_id = (
        f"{evaluation_run_id}-"
        f"{test_case['test_case_id']}-"
        f"{config['configuration_id']}-"
        f"rep{repetition_index}"
    )
    payload = dict(test_case)
    payload.update(config)
    payload["user_id"] = evaluation_user_id
    payload["session_id"] = evaluation_session_id
    payload["evaluation_id"] = evaluation_id
    payload["evaluation_run_id"] = evaluation_run_id
    payload["knowledge"] = test_case.get("knowledge") or DEFAULT_KNOWLEDGE
    return payload


def response_to_row(response, test_case, config, repetition_index):
    metrics = response.get("metrics") or {}
    row = {
        "evaluation_run_id": response.get("evaluation_run_id"),
        "evaluation_id": response.get("evaluation_id"),
        "request_id": metrics.get("request_id"),
        "timestamp": metrics.get("timestamp"),
        "user_id": metrics.get("user_id"),
        "session_id": metrics.get("session_id"),
        "repetition_index": repetition_index,
        "configuration_id": config["configuration_id"],
        "test_case_id": response.get("test_case_id"),
        "test_case_description": response.get("test_case_description"),
        "provider": metrics.get("provider") or config["provider"],
        "model": metrics.get("model"),
        "context_mode": metrics.get("context_mode") or config["context_mode"],
        "context_enabled": metrics.get("context_enabled"),
        "memory_enabled": metrics.get("memory_enabled"),
        "user_query": metrics.get("user_query") or test_case.get("user_query"),
        "context_summary": response.get("context_summary"),
        "generated_response": response.get("generated_response"),
        "total_latency_ms": metrics.get("total_latency_ms"),
        "context_building_latency_ms": metrics.get("context_building_latency_ms"),
        "memory_retrieval_latency_ms": metrics.get("memory_retrieval_latency_ms"),
        "llm_generation_latency_ms": metrics.get("llm_generation_latency_ms"),
        "prompt_length_chars": metrics.get("prompt_length_chars"),
        "response_length_chars": metrics.get("response_length_chars"),
        "retrieved_memory_count": metrics.get("retrieved_memory_count"),
        "retrieved_memories_included": metrics.get("retrieved_memories_included"),
        "estimated_prompt_tokens": metrics.get("estimated_prompt_tokens"),
        "estimated_response_tokens": metrics.get("estimated_response_tokens"),
        "success": response.get("success"),
        "error_message": response.get("error_message"),
        "fallback_used": response.get("fallback_used")
    }
    return row


def make_failure_row(
    evaluation_run_id,
    evaluation_id,
    test_case,
    config,
    error_type,
    error_message
):
    timestamp = datetime.now(timezone.utc).isoformat()
    return {
        "evaluation_run_id": evaluation_run_id,
        "evaluation_id": evaluation_id,
        "timestamp": timestamp,
        "test_case_id": test_case.get("test_case_id"),
        "provider": config["provider"],
        "context_mode": config["context_mode"],
        "memory_enabled": config["memory_enabled"],
        "component": "runner",
        "error_type": error_type,
        "error_message": error_message,
        "fallback_used": False
    }


def write_csv(path, rows, columns):
    with open(path, "w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=columns, extrasaction="ignore")
        writer.writeheader()
        writer.writerows(rows)


def write_jsonl(path, records):
    with open(path, "w", encoding="utf-8") as f:
        for record in records:
            f.write(json.dumps(record, ensure_ascii=False) + "\n")


def build_manual_rows(result_rows, test_cases_by_id):
    rows = []
    for row in result_rows:
        manual_row = dict(row)
        test_case = test_cases_by_id.get(row["test_case_id"], {})
        manual_row["expected_behavior_notes"] = test_case.get(
            "expected_behavior_notes",
            ""
        )
        for column in MANUAL_COLUMNS:
            manual_row.setdefault(column, "")
        return_columns = {
            "contextual_relevance_score",
            "helpfulness_score",
            "non_spoiler_score",
            "clarity_score",
            "correctness_score",
            "comments"
        }
        for column in return_columns:
            manual_row[column] = ""
        rows.append(manual_row)
    return rows


def build_summary_rows(result_rows):
    groups = defaultdict(list)
    for row in result_rows:
        groups[(row["provider"], row["context_mode"], row["memory_enabled"])].append(row)

    summary_rows = []
    for (provider, context_mode, memory_enabled), rows in sorted(groups.items()):
        count = len(rows)
        successes = sum(1 for row in rows if str(row.get("success")).lower() == "true")
        summary_rows.append({
            "provider": provider,
            "context_mode": context_mode,
            "memory_enabled": memory_enabled,
            "number_of_requests": count,
            "success_rate": round(successes / count, 4) if count else 0,
            "average_total_latency_ms": average(rows, "total_latency_ms"),
            "average_llm_latency_ms": average(rows, "llm_generation_latency_ms"),
            "average_memory_latency_ms": average(rows, "memory_retrieval_latency_ms"),
            "average_prompt_length_chars": average(rows, "prompt_length_chars"),
            "average_response_length_chars": average(rows, "response_length_chars")
        })
    return summary_rows


def average(rows, key):
    values = []
    for row in rows:
        try:
            values.append(float(row.get(key) or 0))
        except (TypeError, ValueError):
            pass
    return round(sum(values) / len(values), 3) if values else 0


def main():
    args = parse_args()
    if args.repetitions < 1:
        print("--repetitions must be at least 1", file=sys.stderr)
        return 2

    test_cases = load_test_cases(args.test_cases)
    if not test_cases:
        print("No test cases found.", file=sys.stderr)
        return 2

    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    evaluation_run_id = str(uuid4())
    endpoint = args.backend_url.rstrip("/") + "/evaluation/run"
    configs = build_configurations(
        provider_filter=args.provider_filter,
        include_memory=not args.no_memory_configs,
        include_minimal=args.include_minimal_context
    )
    has_memory_configs = any(config["memory_enabled"] for config in configs)

    if args.reset_evaluation_memory and not args.seed_memory:
        try:
            response = reset_evaluation_memory(
                args.backend_url,
                args.evaluation_user_id,
                args.evaluation_session_id,
                args.timeout
            )
            print(
                "Reset evaluation memory for "
                f"user_id={response.get('user_id')} "
                f"session_id={response.get('session_id')}"
            )
        except Exception as e:
            print(f"Failed to reset evaluation memory: {e}", file=sys.stderr)
            return 1

    if args.seed_memory:
        try:
            response = seed_evaluation_memory(
                args.backend_url,
                args.evaluation_user_id,
                args.evaluation_session_id,
                args.reset_evaluation_memory,
                args.timeout
            )
            print(
                "Seeded evaluation memory: "
                f"{response.get('seeded_memory_count')} memories for "
                f"user_id={response.get('user_id')} "
                f"session_id={response.get('session_id')}"
            )
            if response.get("seeded_memory_count") != len(DEFAULT_EVALUATION_MEMORIES):
                print(
                    "Warning: not all evaluation memories were stored. "
                    "Check backend embedding/Qdrant configuration.",
                    file=sys.stderr
                )
        except Exception as e:
            print(f"Failed to seed evaluation memory: {e}", file=sys.stderr)
            return 1
    elif has_memory_configs:
        print(
            "Warning: memory-enabled configurations are included, but "
            "--seed-memory was not provided. Retrieved memory counts may be 0.",
            file=sys.stderr
        )

    result_rows = []
    jsonl_records = []
    error_rows = []

    total_requests = len(test_cases) * len(configs) * args.repetitions
    completed = 0

    for test_case in test_cases:
        for config in configs:
            for repetition_index in range(1, args.repetitions + 1):
                payload = build_payload(
                    test_case,
                    config,
                    evaluation_run_id,
                    repetition_index,
                    args.evaluation_user_id,
                    args.evaluation_session_id
                )
                evaluation_id = payload["evaluation_id"]

                try:
                    started = time.perf_counter()
                    response = post_json(endpoint, payload, args.timeout)
                    elapsed_ms = round((time.perf_counter() - started) * 1000, 3)
                    response["_runner_latency_ms"] = elapsed_ms
                    row = response_to_row(
                        response,
                        test_case,
                        config,
                        repetition_index
                    )
                    result_rows.append(row)
                    jsonl_records.append(response)

                    if row.get("error_message"):
                        error_rows.append({
                            "evaluation_run_id": evaluation_run_id,
                            "evaluation_id": row["evaluation_id"],
                            "timestamp": row["timestamp"],
                            "test_case_id": row["test_case_id"],
                            "provider": row["provider"],
                            "context_mode": row["context_mode"],
                            "memory_enabled": row["memory_enabled"],
                            "component": "llm",
                            "error_type": "generation_error",
                            "error_message": row["error_message"],
                            "fallback_used": row["fallback_used"]
                        })

                except HTTPError as e:
                    error_body = e.read().decode("utf-8", errors="replace")
                    error_rows.append(make_failure_row(
                        evaluation_run_id,
                        evaluation_id,
                        test_case,
                        config,
                        "http_error",
                        f"HTTP {e.code}: {error_body}"
                    ))
                except URLError as e:
                    error_rows.append(make_failure_row(
                        evaluation_run_id,
                        evaluation_id,
                        test_case,
                        config,
                        "connection_error",
                        str(e)
                    ))
                except Exception as e:
                    error_rows.append(make_failure_row(
                        evaluation_run_id,
                        evaluation_id,
                        test_case,
                        config,
                        type(e).__name__,
                        str(e)
                    ))
                finally:
                    completed += 1
                    print(
                        f"[{completed}/{total_requests}] "
                        f"{test_case['test_case_id']} "
                        f"{config['configuration_id']} rep {repetition_index}"
                    )

    test_cases_by_id = {
        test_case["test_case_id"]: test_case
        for test_case in test_cases
    }
    manual_rows = build_manual_rows(result_rows, test_cases_by_id)
    summary_rows = build_summary_rows(result_rows)

    write_csv(output_dir / "evaluation_results.csv", result_rows, RESULT_COLUMNS)
    write_jsonl(output_dir / "evaluation_results.jsonl", jsonl_records)
    write_csv(
        output_dir / "manual_annotation_dataset.csv",
        manual_rows,
        MANUAL_COLUMNS
    )
    write_csv(output_dir / "error_metrics.csv", error_rows, ERROR_COLUMNS)
    write_csv(output_dir / "evaluation_summary.csv", summary_rows, SUMMARY_COLUMNS)

    print(f"Evaluation run id: {evaluation_run_id}")
    print(f"Results written to: {output_dir}")
    print(f"Successful response rows: {len(result_rows)}")
    print(f"Error rows: {len(error_rows)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
