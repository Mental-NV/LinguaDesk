#!/usr/bin/env python3
"""Render Muse JSONL output as a plain, incrementally flushed transcript."""

import json
import sys


def render(lines, output):
    saw_delta = False
    ends_with_newline = True

    for line in lines:
        try:
            event = json.loads(line)
        except (json.JSONDecodeError, TypeError):
            output.write(line)
            output.flush()
            ends_with_newline = line.endswith('\n')
            continue

        if not isinstance(event, dict):
            output.write(line)
            output.flush()
            ends_with_newline = line.endswith('\n')
            continue

        payload = event.get('payload') or {}
        if not isinstance(payload, dict):
            continue
        payload_type = event.get('payload_type')

        if payload_type == 'run.output.delta':
            text = payload.get('text')
            if isinstance(text, str) and text:
                output.write(text)
                output.flush()
                saw_delta = True
                ends_with_newline = text.endswith('\n')
        elif payload_type == 'run.terminal.completed' and not saw_delta:
            text = payload.get('text')
            if isinstance(text, str) and text:
                output.write(text)
                output.flush()
                ends_with_newline = text.endswith('\n')
        elif payload_type == 'run.terminal.failed':
            reason = payload.get('reason')
            if isinstance(reason, str) and reason:
                if not ends_with_newline:
                    output.write('\n')
                output.write(f'muse: run failed: {reason}\n')
                output.flush()
                ends_with_newline = True

    if not ends_with_newline:
        output.write('\n')
        output.flush()


if __name__ == '__main__':
    render(sys.stdin, sys.stdout)
